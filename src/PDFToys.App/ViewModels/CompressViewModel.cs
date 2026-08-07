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

public sealed class CompressViewModel : ViewModelBase
{
    private readonly ICompressionService _compressionService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IUserSettingsStore? _settingsStore;
    private string _statusMessage = "Ready";
    private string _selectedCompressionLevel = "Standard";
    private bool _isBusy;

    public CompressViewModel(
        ICompressionService compressionService,
        IFileDialogService fileDialogService,
        Action goBackAction,
        IEnumerable<string>? initialFiles = null,
        IUserSettingsStore? settingsStore = null)
    {
        _compressionService = compressionService;
        _fileDialogService = fileDialogService;
        _settingsStore = settingsStore;
        SelectedFiles = [];
        CompressionLevels = ["Standard", "Maximum"];

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
        ExecuteCompressCommand = new DelegateCommand(ExecuteCompress, () => !IsBusy);
        GoBackCommand = new DelegateCommand(goBackAction);

        if (_settingsStore is not null)
        {
            var settings = _settingsStore.Load();
            if (CompressionLevels.Contains(settings.DefaultCompressionLevel))
            {
                SelectedCompressionLevel = settings.DefaultCompressionLevel;
            }
        }
    }

    public ObservableCollection<PdfFileItem> SelectedFiles { get; }

    public ObservableCollection<string> CompressionLevels { get; }

    public string SelectedCompressionLevel
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
            (ExecuteCompressCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        }
    }

    public ICommand AddFilesCommand { get; }

    public ICommand RemoveFileCommand { get; }

    public ICommand GoBackCommand { get; }

    public ICommand ExecuteCompressCommand { get; }

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

    private void ExecuteCompress()
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

            if (!CompressionLevels.Contains(SelectedCompressionLevel))
            {
                StatusMessage = "Please select a valid compression level.";
                return;
            }

            var succeeded = 0;
            var failed = 0;
            var total = SelectedFiles.Count;
            var failedFiles = new List<string>();
            var failureMessages = new List<string>();
            var quality = SelectedCompressionLevel.Equals("Maximum", StringComparison.OrdinalIgnoreCase)
                ? CompressionLevel.Maximum
                : CompressionLevel.Normal;

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
                    var options = new CompressionOptions(outputDirectory, quality);
                    var result = _compressionService.Compress(new PdfFile(selectedFilePath), options);
                    if (!result.IsSuccess)
                    {
                        failed++;
                        failedFiles.Add(selectedFilePath);
                        failureMessages.Add(result.ErrorMessage);
                        continue;
                    }

                    var expectedPath = BuildCompressedOutputPath(selectedFilePath);
                    if (!string.IsNullOrWhiteSpace(result.OutputPath) &&
                        File.Exists(result.OutputPath) &&
                        !string.Equals(result.OutputPath, expectedPath, StringComparison.OrdinalIgnoreCase))
                    {
                        if (OutputFileHelper.MoveToExpectedPath(result.OutputPath, expectedPath) is null)
                        {
                            failed++;
                            failedFiles.Add(selectedFilePath);
                            failureMessages.Add("Could not move compressed output to expected path.");
                            continue;
                        }
                    }

                    succeeded++;
                    StatusMessage = $"Compressed {succeeded + failed}/{total} files...";
                }
                catch (Exception ex)
                {
                    failed++;
                    failedFiles.Add(selectedFile.FullPath);
                    failureMessages.Add(ex.Message);
                }
            }

            StatusMessage = BatchStatusFormatter.FormatCompletion(
                "Compressed",
                succeeded,
                total,
                failedFiles,
                failureMessages);

            if (failedFiles.Count == 0 && _settingsStore is not null)
            {
                var settings = _settingsStore.Load();
                settings.DefaultCompressionLevel = SelectedCompressionLevel;
                _settingsStore.Save(settings);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string BuildCompressedOutputPath(string inputPath)
    {
        var directory = Path.GetDirectoryName(inputPath) ?? string.Empty;
        var fileName = Path.GetFileNameWithoutExtension(inputPath);
        return Path.Combine(directory, $"{fileName}_compressed.pdf");
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
