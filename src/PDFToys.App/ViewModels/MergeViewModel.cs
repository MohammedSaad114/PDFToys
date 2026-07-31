using PDFToys.Core.Contracts;
using PDFToys.Core.Models;
using PDFToys.App.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PDFToys.App.ViewModels;

public sealed class MergeItem : ViewModelBase
{
    private string _fullPath;
    private int _order;

    public MergeItem(string fullPath, int order = 0)
    {
        _fullPath = fullPath;
        _order = order;
    }

    public string FullPath
    {
        get => _fullPath;
        set
        {
            if (_fullPath == value)
            {
                return;
            }

            _fullPath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FileName));
        }
    }

    public string FileName => System.IO.Path.GetFileName(FullPath);

    public int Order
    {
        get => _order;
        set
        {
            if (_order == value)
            {
                return;
            }

            _order = value;
            OnPropertyChanged();
        }
    }
}

public sealed class MergeViewModel : ViewModelBase
{
    private readonly IMergeService _mergeService;
    private readonly IFileDialogService _dialogService;
    private string _statusMessage = "Ready";

    public MergeViewModel(IMergeService mergeService, IFileDialogService dialogService, Action goBackAction)
    {
        _mergeService = mergeService;
        _dialogService = dialogService;
        MergeItems = [];
        GoBackCommand = new DelegateCommand(goBackAction);
        AddFilesCommand = new AsyncDelegateCommand(AddFilesAsync);
        ExecuteMergeCommand = new AsyncDelegateCommand(ExecuteMergeAsync);
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

    public ObservableCollection<MergeItem> MergeItems { get; }

    public ICommand GoBackCommand { get; }

    public ICommand AddFilesCommand { get; }

    public ICommand ExecuteMergeCommand { get; }

    public void ReorderItem(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= MergeItems.Count || newIndex < 0 || newIndex >= MergeItems.Count)
        {
            return;
        }

        MergeItems.Move(oldIndex, newIndex);
        for (var i = 0; i < MergeItems.Count; i++)
        {
            MergeItems[i].Order = i + 1;
        }
    }

    private async Task AddFilesAsync()
    {
        var newFiles = await _dialogService.PickPdfFilesAsync();
        foreach (var path in newFiles.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            if (!MergeItems.Any(existing => string.Equals(existing.FullPath, path, StringComparison.OrdinalIgnoreCase)))
            {
                MergeItems.Add(new MergeItem(path, MergeItems.Count + 1));
            }
        }

        StatusMessage = MergeItems.Count == 0
            ? "No PDF files selected."
            : $"{MergeItems.Count} PDF files selected.";
    }

    private async Task ExecuteMergeAsync()
    {
        if (MergeItems.Count < 2)
        {
            StatusMessage = "Please select at least 2 PDFs.";
            return;
        }

        var selectedPaths = MergeItems.Select(x => x.FullPath).ToArray();
        var directories = selectedPaths
            .Select(Path.GetDirectoryName)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (directories.Count == 0)
        {
            StatusMessage = "Could not determine output directory.";
            return;
        }

        string outputFilePath;
        if (directories.Count == 1)
        {
            outputFilePath = BuildUniqueOutputPath(directories[0]!);
        }
        else
        {
            outputFilePath = await _dialogService.ShowSaveFileDialogAsync("Save Merged PDF", "Merged_Output.pdf", ".pdf") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(outputFilePath))
            {
                StatusMessage = "Merge canceled.";
                return;
            }
        }

        var inputs = selectedPaths.Select(path => new PdfFile(path)).ToArray();
        var outputDirectory = Path.GetDirectoryName(outputFilePath);
        var outputFileName = Path.GetFileName(outputFilePath);
        if (string.IsNullOrWhiteSpace(outputDirectory) || string.IsNullOrWhiteSpace(outputFileName))
        {
            StatusMessage = "Invalid output location.";
            return;
        }

        var options = new MergeOptions(outputDirectory, outputFileName);
        var result = _mergeService.Merge(inputs, options);

        StatusMessage = result.IsSuccess
            ? $"Merge complete: {outputFileName}"
            : result.ErrorMessage;
    }

    private static string BuildUniqueOutputPath(string directory)
    {
        var baseName = "Merged_Output";
        var extension = ".pdf";
        var candidate = Path.Combine(directory, $"{baseName}{extension}");
        if (!File.Exists(candidate))
        {
            return candidate;
        }

        var suffix = 1;
        while (true)
        {
            candidate = Path.Combine(directory, $"{baseName}_{suffix}{extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }

            suffix++;
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
