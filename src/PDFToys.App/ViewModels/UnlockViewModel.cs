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

public sealed record UnlockFileItem(string FullPath, string FileName);

public sealed class UnlockViewModel : ViewModelBase
{
    private readonly IProtectService _protectService;
    private readonly IFileDialogService _fileDialogService;
    private string _password = string.Empty;
    private string _statusMessage = "Ready";
    private bool _isBusy;

    public UnlockViewModel(
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
            AddUniqueFiles(initialFiles);
        }

        AddFilesCommand = new AsyncDelegateCommand(AddFilesAsync, () => !IsBusy);
        RemoveFileCommand = new ParameterDelegateCommand(RemoveFile, () => !IsBusy);
        ExecuteUnlockCommand = new AsyncDelegateCommand(ExecuteUnlockAsync, () => !IsBusy);
        GoBackCommand = new DelegateCommand(goBackAction, () => !IsBusy);
    }

    public ObservableCollection<UnlockFileItem> SelectedFiles { get; }

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
        private set
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
            (AddFilesCommand as AsyncDelegateCommand)?.RaiseCanExecuteChanged();
            (RemoveFileCommand as ParameterDelegateCommand)?.RaiseCanExecuteChanged();
            (ExecuteUnlockCommand as AsyncDelegateCommand)?.RaiseCanExecuteChanged();
            (GoBackCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        }
    }

    public ICommand ExecuteUnlockCommand { get; }

    public ICommand GoBackCommand { get; }

    public ICommand AddFilesCommand { get; }

    public ICommand RemoveFileCommand { get; }

    public async Task ExecuteUnlockAsync()
    {
        if (IsBusy)
        {
            return;
        }

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

        IsBusy = true;
        try
        {
            var selectedFiles = SelectedFiles.ToArray();
            var failures = new List<UnlockFailure>();
            var outputPaths = new List<string>();
            var succeeded = 0;

            for (var i = 0; i < selectedFiles.Length; i++)
            {
                var selectedFile = selectedFiles[i];
                try
                {
                    if (!File.Exists(selectedFile.FullPath))
                    {
                        failures.Add(new UnlockFailure(selectedFile.FullPath, "File not found."));
                    }
                    else
                    {
                        var outputDirectory = Path.GetDirectoryName(selectedFile.FullPath)
                            ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                        var options = new UnlockOptions(Password, outputDirectory);
                        var result = await Task.Run(
                            () => _protectService.Unlock(
                                new PdfFile(selectedFile.FullPath),
                                options));

                        if (!result.IsSuccess)
                        {
                            var message = string.IsNullOrWhiteSpace(result.ErrorMessage)
                                ? "Unlock failed."
                                : result.ErrorMessage;
                            failures.Add(new UnlockFailure(selectedFile.FullPath, message));
                        }
                        else if (string.IsNullOrWhiteSpace(result.OutputPath) || !File.Exists(result.OutputPath))
                        {
                            failures.Add(new UnlockFailure(
                                selectedFile.FullPath,
                                "Unlock completed without producing an output file."));
                        }
                        else
                        {
                            succeeded++;
                            outputPaths.Add(result.OutputPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    failures.Add(new UnlockFailure(selectedFile.FullPath, ex.Message));
                }

                StatusMessage = $"Unlocked {i + 1}/{selectedFiles.Length} files...";
            }

            StatusMessage = FormatCompletion(
                succeeded,
                selectedFiles.Length,
                failures,
                outputPaths);

            if (failures.Count == 0)
            {
                Password = string.Empty;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AddFilesAsync()
    {
        var pickedFiles = await _fileDialogService.PickPdfFilesAsync();
        AddUniqueFiles(pickedFiles);

        StatusMessage = SelectedFiles.Count == 0
            ? "No PDF files selected."
            : $"{SelectedFiles.Count} PDF files selected.";
    }

    private void AddUniqueFiles(IEnumerable<string> files)
    {
        foreach (var path in files.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            if (!SelectedFiles.Any(existing =>
                    string.Equals(existing.FullPath, path, StringComparison.OrdinalIgnoreCase)))
            {
                SelectedFiles.Add(new UnlockFileItem(path, Path.GetFileName(path)));
            }
        }
    }

    private void RemoveFile(object? parameter)
    {
        if (parameter is not UnlockFileItem fileItem)
        {
            return;
        }

        SelectedFiles.Remove(fileItem);
        StatusMessage = SelectedFiles.Count == 0
            ? "No PDF files selected."
            : $"{SelectedFiles.Count} PDF files selected.";
    }

    private static string FormatCompletion(
        int succeeded,
        int total,
        IReadOnlyList<UnlockFailure> failures,
        IReadOnlyList<string> outputPaths)
    {
        if (failures.Count == 0)
        {
            return total == 1 && outputPaths.Count == 1
                ? $"Unlock complete: {outputPaths[0]}"
                : $"Unlocked {succeeded}/{total} files successfully.";
        }

        var failureDetails = string.Join(
            "; ",
            failures.Select(failure => $"{failure.FilePath}: {failure.Message}"));
        return $"Unlocked {succeeded}/{total} files successfully, "
            + $"{failures.Count} failed. Failures: {failureDetails}";
    }

    private sealed record UnlockFailure(string FilePath, string Message);

    private sealed class DelegateCommand
        (Action execute, Func<bool>? canExecute = null) : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => execute();

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    private sealed class ParameterDelegateCommand
        (Action<object?> execute, Func<bool>? canExecute = null) : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => execute(parameter);

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    private sealed class AsyncDelegateCommand
        (Func<Task> execute, Func<bool>? canExecute = null) : ICommand
    {
        private bool _isExecuting;

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) =>
            !_isExecuting && (canExecute?.Invoke() ?? true);

        public async void Execute(object? parameter)
        {
            await ExecuteAsync();
        }

        public async Task ExecuteAsync()
        {
            if (!CanExecute(null))
            {
                return;
            }

            _isExecuting = true;
            RaiseCanExecuteChanged();
            try
            {
                await execute();
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
