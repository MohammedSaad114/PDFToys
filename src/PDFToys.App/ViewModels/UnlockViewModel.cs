using PDFToys.App.Models;
using PDFToys.App.Services;
using PDFToys.Core.Contracts;
using PDFToys.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PDFToys.App.ViewModels;

public sealed class UnlockViewModel : ViewModelBase
{
    private readonly IProtectService _protectService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IUserSettingsStore? _settingsStore;
    private string _password = string.Empty;
    private string _statusMessage = "Ready";
    private bool _isBusy;

    public PdfOutputModeSelection OutputMode { get; } = new();

    public UnlockViewModel(
        IProtectService passwordService,
        IFileDialogService fileDialogService,
        Action goBackAction,
        IEnumerable<string>? initialFiles = null,
        IUserSettingsStore? settingsStore = null)
    {
        _protectService = passwordService;
        _fileDialogService = fileDialogService;
        _settingsStore = settingsStore;
        SelectedFiles = [];

        if (initialFiles is not null)
        {
            foreach (var file in initialFiles.Where(path => !string.IsNullOrWhiteSpace(path)))
            {
                if (!SelectedFiles.Any(existing => string.Equals(existing.FullPath, file, StringComparison.OrdinalIgnoreCase)))
                {
                    SelectedFiles.Add(new PdfFileItem(file, Path.GetFileName(file)));
                }
            }
        }

        AddFilesCommand = new AsyncDelegateCommand(AddFilesAsync);
        RemoveFileCommand = new ParameterDelegateCommand(RemoveFile);
        ExecuteUnlockCommand = new DelegateCommand(ExecuteUnlock, () => !IsBusy);
        GoBackCommand = new DelegateCommand(goBackAction);

        if (_settingsStore is not null)
        {
            OutputMode.ApplyModeFromName(_settingsStore.Load().DefaultPdfOutputMode);
        }
    }

    public ObservableCollection<PdfFileItem> SelectedFiles { get; }

    public string Password
    {
        get => _password;
        set
        {
            if (_password == value)
            {
                return;
            }

            _password = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage == value)
            {
                return;
            }

            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value)
            {
                return;
            }

            _isBusy = value;
            OnPropertyChanged();
            (ExecuteUnlockCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        }
    }

    public ICommand ExecuteUnlockCommand { get; }

    public ICommand GoBackCommand { get; }

    public ICommand AddFilesCommand { get; }

    public ICommand RemoveFileCommand { get; }

    private async Task AddFilesAsync()
    {
        var pickedFiles = await _fileDialogService.PickPdfFilesAsync();
        foreach (var path in pickedFiles.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            if (!SelectedFiles.Any(existing => string.Equals(existing.FullPath, path, StringComparison.OrdinalIgnoreCase)))
            {
                SelectedFiles.Add(new PdfFileItem(path, Path.GetFileName(path)));
            }
        }

        StatusMessage = SelectedFiles.Count == 0
            ? "No PDF files selected."
            : $"{SelectedFiles.Count} PDF files selected.";
    }

    private void RemoveFile(object? parameter)
    {
        if (parameter is not PdfFileItem fileItem)
        {
            return;
        }

        SelectedFiles.Remove(fileItem);
        StatusMessage = SelectedFiles.Count == 0
            ? "No PDF files selected."
            : $"{SelectedFiles.Count} PDF files selected.";
    }

    private void ExecuteUnlock()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            if (SelectedFiles.Count == 0)
            {
                StatusMessage = "Please select PDF files first.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                StatusMessage = "Password is required.";
                return;
            }

            var succeeded = 0;
            var failed = 0;
            var total = SelectedFiles.Count;
            var failedFiles = new List<string>();
            var failureMessages = new List<string>();
            foreach (var selectedFile in SelectedFiles)
            {
                try
                {
                    var selectedFilePath = selectedFile.FullPath;
                    if (!File.Exists(selectedFilePath))
                    {
                        failed++;
                        failedFiles.Add(selectedFilePath);
                        failureMessages.Add("File not found.");
                        continue;
                    }

                    var outputDirectory = Path.GetDirectoryName(selectedFilePath)
                        ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    var options = new UnlockOptions(Password, outputDirectory);
                    var result = _protectService.Unlock(new PdfFile(selectedFilePath), options);
                    if (!result.IsSuccess)
                    {
                        failed++;
                        failedFiles.Add(selectedFilePath);
                        failureMessages.Add(result.ErrorMessage);
                        continue;
                    }

                    if (OutputMode.SelectedOutputMode == PdfOutputMode.ReplaceOriginal)
                    {
                        if (!OutputFileHelper.TryReplaceOriginal(result.OutputPath, selectedFilePath))
                        {
                            failed++;
                            failedFiles.Add(selectedFilePath);
                            failureMessages.Add("Could not replace original file.");
                            continue;
                        }
                    }

                    succeeded++;
                    StatusMessage = $"Unlocked {succeeded + failed}/{total} files...";
                }
                catch (Exception ex)
                {
                    failed++;
                    failedFiles.Add(selectedFile.FullPath);
                    failureMessages.Add(ex.Message);
                }
            }

            var successVerb = OutputMode.SelectedOutputMode == PdfOutputMode.ReplaceOriginal
                ? "Replaced"
                : "Unlocked";
            StatusMessage = BatchStatusFormatter.FormatCompletion(
                successVerb,
                succeeded,
                total,
                failedFiles,
                failureMessages);

            if (failedFiles.Count == 0 && _settingsStore is not null)
            {
                var settings = _settingsStore.Load();
                settings.DefaultPdfOutputMode = OutputMode.SelectedOutputMode.ToString();
                _settingsStore.Save(settings);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private sealed class DelegateCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public DelegateCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    private sealed class ParameterDelegateCommand(Action<object?> execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => execute(parameter);
    }

    private sealed class AsyncDelegateCommand(Func<Task> execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public async void Execute(object? parameter) => await execute();
    }
}
