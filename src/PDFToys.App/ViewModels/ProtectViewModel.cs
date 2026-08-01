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

public sealed class ProtectViewModel : ViewModelBase
{
    private readonly IProtectService _protectService;
    private readonly IFileDialogService _fileDialogService;
    private string _password = string.Empty;
    private string _statusMessage = "Ready";
    private bool _isBusy;

    public ProtectViewModel(
        IProtectService protectService,
        IFileDialogService fileDialogService,
        Action goBackAction,
        IEnumerable<string>? initialFiles = null)
    {
        _protectService = protectService;
        _fileDialogService = fileDialogService;
        SelectedFiles = [];
        if (initialFiles is not null)
        {
            foreach (var file in initialFiles.Where(path => !string.IsNullOrWhiteSpace(path)))
            {
                if (!SelectedFiles.Any(existing => string.Equals(existing.FullPath, file, StringComparison.OrdinalIgnoreCase)))
                {
                    SelectedFiles.Add(new ProtectFileItem(file, Path.GetFileName(file)));
                }
            }
        }

        AddFilesCommand = new AsyncDelegateCommand(AddFilesAsync);
        RemoveFileCommand = new ParameterDelegateCommand(RemoveFile);
        ExecuteProtectCommand = new DelegateCommand(ExecuteProtect, () => !IsBusy);
        GoBackCommand = new DelegateCommand(goBackAction);
    }

    public ObservableCollection<ProtectFileItem> SelectedFiles { get; }

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
            (ExecuteProtectCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        }
    }

    public ICommand ExecuteProtectCommand { get; }

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
                SelectedFiles.Add(new ProtectFileItem(path, Path.GetFileName(path)));
            }
        }

        StatusMessage = SelectedFiles.Count == 0
            ? "No PDF files selected."
            : $"{SelectedFiles.Count} PDF files selected.";
    }

    private void RemoveFile(object? parameter)
    {
        if (parameter is not ProtectFileItem fileItem)
        {
            return;
        }

        SelectedFiles.Remove(fileItem);
        StatusMessage = SelectedFiles.Count == 0
            ? "No PDF files selected."
            : $"{SelectedFiles.Count} PDF files selected.";
    }

    private void ExecuteProtect()
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
                    var options = new ProtectOptions(Password, outputDirectory);
                    var result = _protectService.Protect(new PdfFile(selectedFilePath), options);
                    if (!result.IsSuccess)
                    {
                        failed++;
                        failedFiles.Add(selectedFilePath);
                        failureMessages.Add(result.ErrorMessage);
                        continue;
                    }

                    succeeded++;
                    StatusMessage = $"Protected {succeeded + failed}/{total} files...";
                }
                catch (Exception ex)
                {
                    failed++;
                    failedFiles.Add(selectedFile.FullPath);
                    failureMessages.Add(ex.Message);
                }
            }

            StatusMessage = FormatCompletion(
                "Protected",
                succeeded,
                total,
                failedFiles,
                failureMessages);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string FormatCompletion(
        string successVerb,
        int succeeded,
        int total,
        IReadOnlyList<string> failedFiles,
        IReadOnlyList<string> failureMessages)
    {
        if (failedFiles.Count == 0)
        {
            return $"{successVerb} {succeeded}/{total} files successfully!";
        }

        var failures = new List<string>();
        for (var i = 0; i < failedFiles.Count; i++)
        {
            var message = i < failureMessages.Count
                ? failureMessages[i]
                : "Unexpected error.";
            failures.Add($"{failedFiles[i]}: {message}");
        }

        return $"{successVerb} {succeeded}/{total} files successfully, "
            + $"{failedFiles.Count} failed. Failures: {string.Join("; ", failures)}";
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

public sealed record ProtectFileItem(string FullPath, string FileName);
