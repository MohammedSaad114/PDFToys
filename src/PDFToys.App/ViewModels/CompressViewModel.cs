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

public sealed record CompressFileItem(string FullPath, string FileName);

public sealed record CompressionLevelItem(CompressionLevel Level, string Label);

public sealed class CompressViewModel : ViewModelBase
{
    private readonly ICompressionService _compressionService;
    private readonly IFileDialogService _fileDialogService;
    private string _statusMessage = "Ready";
    private CompressionLevelItem _selectedCompressionLevel;
    private bool _isBusy;

    public CompressViewModel(
        ICompressionService compressionService,
        IFileDialogService fileDialogService,
        Action goBackAction,
        IEnumerable<string>? initialFiles = null)
    {
        _compressionService = compressionService;
        _fileDialogService = fileDialogService;
        SelectedFiles = [];
        CompressionLevels =
        [
            new CompressionLevelItem(CompressionLevel.Normal, "Standard"),
            new CompressionLevelItem(CompressionLevel.Maximum, "Maximum")
        ];
        _selectedCompressionLevel = CompressionLevels[0];

        if (initialFiles is not null)
        {
            AddUniqueFiles(initialFiles);
        }

        AddFilesCommand = new AsyncDelegateCommand(AddFilesAsync, () => !IsBusy);
        RemoveFileCommand = new ParameterDelegateCommand(RemoveFile, () => !IsBusy);
        ExecuteCompressCommand = new AsyncDelegateCommand(ExecuteCompressAsync, () => !IsBusy);
        GoBackCommand = new DelegateCommand(goBackAction, () => !IsBusy);
    }

    public ObservableCollection<CompressFileItem> SelectedFiles { get; }

    public IReadOnlyList<CompressionLevelItem> CompressionLevels { get; }

    public CompressionLevelItem SelectedCompressionLevel
    {
        get => _selectedCompressionLevel;
        set
        {
            if (_selectedCompressionLevel == value)
            {
                return;
            }

            _selectedCompressionLevel = value;
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
            (ExecuteCompressCommand as AsyncDelegateCommand)?.RaiseCanExecuteChanged();
            (GoBackCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        }
    }

    public ICommand AddFilesCommand { get; }

    public ICommand RemoveFileCommand { get; }

    public ICommand GoBackCommand { get; }

    public ICommand ExecuteCompressCommand { get; }

    public async Task ExecuteCompressAsync()
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

        if (!CompressionLevels.Contains(SelectedCompressionLevel))
        {
            StatusMessage = "Please select a valid compression level.";
            return;
        }

        IsBusy = true;
        try
        {
            var selectedFiles = SelectedFiles.ToArray();
            var failures = new List<CompressionFailure>();
            var outputPaths = new List<string>();
            var succeeded = 0;

            for (var i = 0; i < selectedFiles.Length; i++)
            {
                var selectedFile = selectedFiles[i];
                try
                {
                    if (!File.Exists(selectedFile.FullPath))
                    {
                        failures.Add(new CompressionFailure(selectedFile.FullPath, "File not found."));
                    }
                    else
                    {
                        var outputDirectory = Path.GetDirectoryName(selectedFile.FullPath)
                            ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                        var options = new CompressionOptions(
                            outputDirectory,
                            SelectedCompressionLevel.Level);
                        var result = await Task.Run(
                            () => _compressionService.Compress(
                                new PdfFile(selectedFile.FullPath),
                                options));

                        if (!result.IsSuccess)
                        {
                            var message = string.IsNullOrWhiteSpace(result.ErrorMessage)
                                ? "Compression failed."
                                : result.ErrorMessage;
                            failures.Add(new CompressionFailure(selectedFile.FullPath, message));
                        }
                        else if (string.IsNullOrWhiteSpace(result.OutputPath) || !File.Exists(result.OutputPath))
                        {
                            failures.Add(new CompressionFailure(
                                selectedFile.FullPath,
                                "Compression completed without producing an output file."));
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
                    failures.Add(new CompressionFailure(selectedFile.FullPath, ex.Message));
                }

                StatusMessage = $"Compressed {i + 1}/{selectedFiles.Length} files...";
            }

            StatusMessage = FormatCompletion(
                succeeded,
                selectedFiles.Length,
                failures,
                outputPaths);
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
                SelectedFiles.Add(new CompressFileItem(path, Path.GetFileName(path)));
            }
        }
    }

    private void RemoveFile(object? parameter)
    {
        if (parameter is not CompressFileItem fileItem)
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
        IReadOnlyList<CompressionFailure> failures,
        IReadOnlyList<string> outputPaths)
    {
        if (failures.Count == 0)
        {
            return total == 1 && outputPaths.Count == 1
                ? $"Compression complete: {outputPaths[0]}"
                : $"Compressed {succeeded}/{total} files successfully.";
        }

        var failureDetails = string.Join(
            "; ",
            failures.Select(failure => $"{failure.FilePath}: {failure.Message}"));
        return $"Compressed {succeeded}/{total} files successfully, "
            + $"{failures.Count} failed. Failures: {failureDetails}";
    }

    private sealed record CompressionFailure(string FilePath, string Message);

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
