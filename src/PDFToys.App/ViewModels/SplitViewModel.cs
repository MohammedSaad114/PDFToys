using PDFToys.Core.Contracts;
using PDFToys.Core.Models;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Input;

namespace PDFToys.App.ViewModels;

public sealed class SplitViewModel : ViewModelBase
{
    private readonly ISplitService _splitService;
    private string _selectedFilePath = string.Empty;
    private string _statusMessage = "Ready";
    private string _customRangeText = string.Empty;

    public SplitViewModel(ISplitService splitService, Action goBackAction)
    {
        _splitService = splitService;
        GoBackCommand = new DelegateCommand(_ => goBackAction());
        SplitPresetCommand = new DelegateCommand(ExecutePresetSplit);
        ExecuteCustomSplitCommand = new DelegateCommand(_ => ExecuteCustomSplit());
    }

    public string SelectedFilePath
    {
        get => _selectedFilePath;
        set
        {
            if (_selectedFilePath == value)
            {
                return;
            }

            _selectedFilePath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedFileLabel));
        }
    }

    public string SelectedFileLabel => string.IsNullOrWhiteSpace(SelectedFilePath)
        ? string.Empty
        : $"SELECTED: {SelectedFilePath}";

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

    public string CustomRangeText
    {
        get => _customRangeText;
        set
        {
            if (_customRangeText == value)
            {
                return;
            }

            _customRangeText = value;
            OnPropertyChanged();
        }
    }

    public ICommand SplitPresetCommand { get; }

    public ICommand ExecuteCustomSplitCommand { get; }

    public ICommand GoBackCommand { get; }

    private void ExecutePresetSplit(object? parameter)
    {
        if (!TryGetInputFile(out var inputFile))
        {
            return;
        }

        var preset = parameter?.ToString()?.ToLowerInvariant() switch
        {
            "half" => SplitPreset.Half,
            "quarter" => SplitPreset.Quarter,
            _ => SplitPreset.None
        };

        if (preset is SplitPreset.None)
        {
            StatusMessage = "Invalid split preset.";
            return;
        }

        var options = new SplitOptions(GetOutputDirectory(inputFile.FilePath), 0, null, preset);
        var result = _splitService.Split(inputFile, options);
        StatusMessage = result.IsSuccess
            ? $"Split successful: {result.OutputPath}"
            : result.ErrorMessage;
    }

    private void ExecuteCustomSplit()
    {
        if (!TryGetInputFile(out var inputFile))
        {
            return;
        }

        if (!TryParseCustomRanges(CustomRangeText, out var ranges))
        {
            StatusMessage = "Invalid range format.";
            return;
        }

        var options = new SplitOptions(GetOutputDirectory(inputFile.FilePath), 0, ranges);
        var result = _splitService.Split(inputFile, options);
        StatusMessage = result.IsSuccess
            ? $"Split successful: {result.OutputPath}"
            : result.ErrorMessage;
    }

    private static bool TryParseCustomRanges(string rangeText, out SplitRange[] ranges)
    {
        ranges = [];
        if (string.IsNullOrWhiteSpace(rangeText))
        {
            return false;
        }

        var parsedRanges = new List<SplitRange>();
        var tokens = rangeText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var token in tokens)
        {
            if (token.Contains('-'))
            {
                var bounds = token.Split('-', StringSplitOptions.TrimEntries);
                if (bounds.Length != 2 ||
                    !int.TryParse(bounds[0], out var start) ||
                    !int.TryParse(bounds[1], out var end) ||
                    start < 1 ||
                    end < 1 ||
                    end < start)
                {
                    return false;
                }

                parsedRanges.Add(new SplitRange(start, end));
                continue;
            }

            if (!int.TryParse(token, out var page) || page < 1)
            {
                return false;
            }

            parsedRanges.Add(new SplitRange(page, page));
        }

        if (parsedRanges.Count == 0)
        {
            return false;
        }

        ranges = [.. parsedRanges];
        return true;
    }

    private bool TryGetInputFile(out PdfFile file)
    {
        if (string.IsNullOrWhiteSpace(SelectedFilePath))
        {
            StatusMessage = "Please select a PDF file first.";
            file = new PdfFile(string.Empty);
            return false;
        }

        file = new PdfFile(SelectedFilePath);
        return true;
    }

    private static string GetOutputDirectory(string inputFilePath)
    {
        return Path.GetDirectoryName(inputFilePath)
            ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    }

    private sealed class DelegateCommand(Action<object?> execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => execute(parameter);
    }
}
