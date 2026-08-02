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
public sealed record ConvertExportFileItem(string FullPath, string FileName);
public sealed class ConvertExportViewModel : ViewModelBase
{
    private readonly IConversionService _conversionService;
    private readonly IMergeService _mergeService;
    private readonly IExportService _exportService;
    private readonly IFileDialogService _fileDialogService;
    private ConvertExportOperationItem _selectedOperationItem;
    private string _statusMessage = "Select files and an operation to begin.";
    private bool _isBusy;

    public ConvertExportViewModel(
        IConversionService conversionService,
        IMergeService mergePdfService,
        IExportService pdfExportService,
        IFileDialogService fileDialogService,
        Action goBackAction,
        IEnumerable<string>? initialFiles = null)
    {
        _conversionService = conversionService;
        _mergeService = mergePdfService;
        _exportService = pdfExportService;
        _fileDialogService = fileDialogService;
        SelectedFiles = [];
        OperationItems =
        [
            new ConvertExportOperationItem(ConvertExportOperation.ConvertToPdf, "Convert to PDF"),
            new ConvertExportOperationItem(ConvertExportOperation.CombineToPdf, "Combine into PDF"),
            new ConvertExportOperationItem(ConvertExportOperation.ConvertEachToPdf, "Convert Each to PDF"),
            new ConvertExportOperationItem(ConvertExportOperation.PdfToJpg, "Convert PDF to JPG"),
            new ConvertExportOperationItem(ConvertExportOperation.PdfToJpeg, "Convert PDF to JPEG"),
            new ConvertExportOperationItem(ConvertExportOperation.PdfToPng, "Convert PDF to PNG"),
            new ConvertExportOperationItem(ConvertExportOperation.PdfToMarkdown, "Convert PDF to Markdown")
        ];
        _selectedOperationItem = OperationItems[0];

        if (initialFiles is not null)
        {
            foreach (var file in initialFiles.Where(path => !string.IsNullOrWhiteSpace(path)))
            {
                AddFile(file);
            }
        }

        GoBackCommand = new DelegateCommand(goBackAction);
        AddFilesCommand = new AsyncDelegateCommand(AddFilesAsync);
        RemoveFileCommand = new ParameterDelegateCommand(RemoveFile);
        ExecuteCommand = new AsyncDelegateCommand(ExecuteAsync, () => !IsBusy);
    }

    public ObservableCollection<ConvertExportFileItem> SelectedFiles { get; }

    public IReadOnlyList<ConvertExportOperationItem> OperationItems { get; }

    public ConvertExportOperationItem SelectedOperationItem
    {
        get => _selectedOperationItem;
        set
        {
            if (_selectedOperationItem == value)
            {
                return;
            }

            _selectedOperationItem = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedOperationLabel));
            OnPropertyChanged(nameof(AddFilesButtonLabel));
        }
    }

    public ConvertExportOperation SelectedOperation => SelectedOperationItem.Operation;

    public string SelectedOperationLabel => SelectedOperationItem.Label;

    public string AddFilesButtonLabel => RequiresPdfInputs(SelectedOperation)
        ? "+ Add PDF Files"
        : "+ Add Files to Convert";

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
            (ExecuteCommand as AsyncDelegateCommand)?.RaiseCanExecuteChanged();
        }
    }

    public ICommand GoBackCommand { get; }

    public ICommand AddFilesCommand { get; }

    public ICommand RemoveFileCommand { get; }

    public ICommand ExecuteCommand { get; }

    private async Task AddFilesAsync()
    {
        var pickedFiles = RequiresPdfInputs(SelectedOperation)
            ? await _fileDialogService.PickPdfFilesAsync()
            : await _fileDialogService.PickConvertibleFilesAsync(allowMultiple: true);

        foreach (var path in pickedFiles.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            AddFile(path);
        }

        StatusMessage = SelectedFiles.Count == 0
            ? "No files selected."
            : $"{SelectedFiles.Count} file(s) selected.";
    }

    private void AddFile(string path)
    {
        if (SelectedFiles.Any(existing => string.Equals(existing.FullPath, path, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        SelectedFiles.Add(new ConvertExportFileItem(path, Path.GetFileName(path)));
    }

    private void RemoveFile(object? parameter)
    {
        if (parameter is not ConvertExportFileItem fileItem)
        {
            return;
        }

        SelectedFiles.Remove(fileItem);
        StatusMessage = SelectedFiles.Count == 0
            ? "No files selected."
            : $"{SelectedFiles.Count} file(s) selected.";
    }

    private async Task ExecuteAsync()
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
                StatusMessage = "Please select files first.";
                return;
            }

            var inputs = SelectedFiles.Select(file => file.FullPath).ToList();
            var validationError = ValidateInputs(SelectedOperation, inputs);
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                StatusMessage = validationError;
                return;
            }

            StatusMessage = "Running operation...";
            await Task.Yield();

            try
            {
                var outputs = new List<string>();
                var errors = new List<string>();
                var success = SelectedOperation switch
                {
                    ConvertExportOperation.ConvertToPdf => ExecuteConvertToPdf(inputs[0], outputs, errors),
                    ConvertExportOperation.ConvertEachToPdf => ExecuteConvertEachToPdf(inputs, outputs, errors),
                    ConvertExportOperation.CombineToPdf => ExecuteCombineToPdf(inputs, outputs, errors),
                    ConvertExportOperation.PdfToJpg => ExecuteExport(inputs, PdfExportFormat.Jpg, outputs, errors),
                    ConvertExportOperation.PdfToJpeg => ExecuteExport(inputs, PdfExportFormat.Jpeg, outputs, errors),
                    ConvertExportOperation.PdfToPng => ExecuteExport(inputs, PdfExportFormat.Png, outputs, errors),
                    ConvertExportOperation.PdfToMarkdown => ExecuteExport(inputs, PdfExportFormat.Markdown, outputs, errors),
                    _ => false
                };

                if (!success)
                {
                    StatusMessage = BatchStatusFormatter.FormatErrors(errors);
                    return;
                }

                StatusMessage = outputs.Count == 1
                    ? $"Completed: {outputs[0]}"
                    : $"Completed {outputs.Count} output(s). Last: {outputs[^1]}";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string? ValidateInputs(ConvertExportOperation operation, IReadOnlyList<string> inputs)
    {
        if (operation == ConvertExportOperation.ConvertToPdf && inputs.Count != 1)
        {
            return "Convert to PDF requires exactly one file.";
        }

        if (operation == ConvertExportOperation.CombineToPdf)
        {
            if (inputs.Count < 2)
            {
                return "Combine into PDF requires at least two files.";
            }

            var extensions = inputs
                .Select(path => Path.GetExtension(path).ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (extensions.Count != 1)
            {
                return "Combine into PDF requires files with the same extension.";
            }
        }

        if (RequiresPdfInputs(operation))
        {
            var invalid = inputs.FirstOrDefault(path => !path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));
            if (invalid is not null)
            {
                return "PDF export operations require PDF files only.";
            }
        }

        return null;
    }

    private bool ExecuteConvertToPdf(string input, List<string> outputs, List<string> errors)
    {
        var outputDirectory = ResolveOutputDirectory(input);
        var result = _conversionService.Convert(input, new ConversionOptions(outputDirectory));
        if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.OutputPath))
        {
            errors.Add(result.ErrorMessage);
            return false;
        }

        outputs.Add(result.OutputPath);
        return true;
    }

    private bool ExecuteConvertEachToPdf(IReadOnlyList<string> inputs, List<string> outputs, List<string> errors)
    {
        var failed = false;
        foreach (var input in inputs)
        {
            if (!ExecuteConvertToPdf(input, outputs, errors))
            {
                failed = true;
            }
        }

        return !failed;
    }

    private bool ExecuteCombineToPdf(IReadOnlyList<string> inputs, List<string> outputs, List<string> errors)
    {
        var convertedPdfPaths = new List<string>();
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"pdftoys-combine-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            foreach (var input in inputs)
            {
                var conversionResult = _conversionService.Convert(input, new ConversionOptions(tempDirectory));
                if (!conversionResult.IsSuccess || string.IsNullOrWhiteSpace(conversionResult.OutputPath))
                {
                    errors.Add(conversionResult.ErrorMessage);
                    return false;
                }

                convertedPdfPaths.Add(conversionResult.OutputPath);
            }

            var mergeOutputDirectory = ResolveOutputDirectory(inputs[0]);
            var mergedName = $"combined_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
            var mergeResult = _mergeService.Merge(
                convertedPdfPaths.Select(path => new PdfFile(path)).ToArray(),
                new MergeOptions(mergeOutputDirectory, mergedName));
            if (!mergeResult.IsSuccess || string.IsNullOrWhiteSpace(mergeResult.OutputPath))
            {
                errors.Add(mergeResult.ErrorMessage);
                return false;
            }

            outputs.Add(mergeResult.OutputPath);
            return true;
        }
        finally
        {
            TryDeleteDirectory(tempDirectory);
        }
    }

    private bool ExecuteExport(
        IReadOnlyList<string> inputs,
        PdfExportFormat format,
        List<string> outputs,
        List<string> errors)
    {
        if (inputs.Count == 1)
        {
            var input = inputs[0];
            var outputDirectory = ResolveOutputDirectory(input);
            var exportResult = _exportService.Export(
                [new PdfFile(input)],
                new ExportOptions(outputDirectory, format));
            if (!exportResult.IsSuccess || string.IsNullOrWhiteSpace(exportResult.OutputPath))
            {
                errors.Add(exportResult.ErrorMessage);
                return false;
            }

            outputs.Add(exportResult.OutputPath);
            return true;
        }

        var batchOutputDirectory = ResolveOutputDirectory(inputs[0]);
        var batchResult = _exportService.Export(
            inputs.Select(path => new PdfFile(path)).ToList(),
            new ExportOptions(batchOutputDirectory, format));
        if (!batchResult.IsSuccess || string.IsNullOrWhiteSpace(batchResult.OutputPath))
        {
            errors.Add(batchResult.ErrorMessage);
            return false;
        }

        outputs.Add(batchResult.OutputPath);
        return true;
    }

    private static bool RequiresPdfInputs(ConvertExportOperation operation) =>
        operation is ConvertExportOperation.PdfToJpg
            or ConvertExportOperation.PdfToJpeg
            or ConvertExportOperation.PdfToPng
            or ConvertExportOperation.PdfToMarkdown;

    private static string ResolveOutputDirectory(string inputPath)
    {
        var directory = Path.GetDirectoryName(inputPath);
        return !string.IsNullOrWhiteSpace(directory)
            ? directory
            : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    }

    private static void TryDeleteDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Best-effort cleanup for temporary combine artifacts.
        }
    }

    private sealed class DelegateCommand(Action execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => execute();
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

    private sealed class AsyncDelegateCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;

        public AsyncDelegateCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public async void Execute(object? parameter) => await _execute();

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
