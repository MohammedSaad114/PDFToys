using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using PDFToys.App.Models;
using PDFToys.App.Services;
using PDFToys.Core.Contracts;
using PDFToys.Core.Models;

namespace PDFToys.App.ViewModels;

public sealed class OrganizePagesViewModel : ViewModelBase
{
    private readonly IRearrangeService _rearrangeService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IPdfPagePreviewService _pagePreviewService;
    private readonly IPagePreviewDialogService _pagePreviewDialogService;
    private readonly Action _goBackAction;
    private string _selectedFilePath = string.Empty;
    private string _outputFilePath = string.Empty;
    private string _statusMessage = "Select a PDF to organize pages.";
    private OrganizePageItemViewModel? _selectedPage;

    public OrganizePagesViewModel(
        IRearrangeService rearrangeService,
        IFileDialogService fileDialogService,
        IPdfPagePreviewService pagePreviewService,
        IPagePreviewDialogService pagePreviewDialogService,
        Action goBackAction,
        string? initialFilePath = null)
    {
        _rearrangeService = rearrangeService;
        _fileDialogService = fileDialogService;
        _pagePreviewService = pagePreviewService;
        _pagePreviewDialogService = pagePreviewDialogService;
        _goBackAction = goBackAction;

        Pages = [];

        GoBackCommand = new DelegateCommand(_ => _goBackAction());
        SelectPdfCommand = new DelegateCommand(_ => _ = SelectPdfAsync());
        ChooseOutputCommand = new DelegateCommand(_ => _ = ChooseOutputAsync());
        MoveUpCommand = new DelegateCommand(_ => MoveSelected(-1), _ => CanMoveSelected(-1));
        MoveDownCommand = new DelegateCommand(_ => MoveSelected(1), _ => CanMoveSelected(1));
        RotateLeftCommand = new DelegateCommand(_ => RotateSelected(-90), _ => SelectedPage is not null);
        RotateRightCommand = new DelegateCommand(_ => RotateSelected(90), _ => SelectedPage is not null);
        DeleteCommand = new DelegateCommand(_ => SetSelectedIncluded(false), _ => SelectedPage?.IsIncluded == true);
        RestoreCommand = new DelegateCommand(_ => SetSelectedIncluded(true), _ => SelectedPage?.IsIncluded == false);
        ResetCommand = new DelegateCommand(_ => ResetPages(), _ => !string.IsNullOrWhiteSpace(SelectedFilePath));
        ViewPageCommand = new DelegateCommand(_ => _ = ViewPageAsync(), _ => CanViewPage());
        SaveCommand = new DelegateCommand(_ => Save(), _ => CanSave());

        if (!string.IsNullOrWhiteSpace(initialFilePath))
        {
            LoadPdf(initialFilePath);
        }
    }

    public ObservableCollection<OrganizePageItemViewModel> Pages { get; }

    public string SelectedFilePath
    {
        get => _selectedFilePath;
        private set
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

    public string OutputFilePath
    {
        get => _outputFilePath;
        private set
        {
            if (_outputFilePath == value)
            {
                return;
            }

            _outputFilePath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SavedAsLabel));
        }
    }

    public string SavedAsLabel => string.IsNullOrWhiteSpace(OutputFilePath)
        ? string.Empty
        : $"SAVED AS: {OutputFilePath}";

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

    public OrganizePageItemViewModel? SelectedPage
    {
        get => _selectedPage;
        set
        {
            if (ReferenceEquals(_selectedPage, value))
            {
                return;
            }

            _selectedPage = value;
            OnPropertyChanged();
            RefreshCommandStates();
        }
    }

    public ICommand GoBackCommand { get; }

    public ICommand SelectPdfCommand { get; }

    public ICommand ChooseOutputCommand { get; }

    public ICommand MoveUpCommand { get; }

    public ICommand MoveDownCommand { get; }

    public ICommand RotateLeftCommand { get; }

    public ICommand RotateRightCommand { get; }

    public ICommand DeleteCommand { get; }

    public ICommand RestoreCommand { get; }

    public ICommand ResetCommand { get; }

    public ICommand ViewPageCommand { get; }

    public ICommand SaveCommand { get; }

    private async Task SelectPdfAsync()
    {
        var files = await _fileDialogService.PickPdfFilesAsync();
        if (files.Count == 0)
        {
            return;
        }

        LoadPdf(files[0]);
    }

    private async Task ChooseOutputAsync()
    {
        var defaultName = string.IsNullOrWhiteSpace(SelectedFilePath)
            ? "organized.pdf"
            : $"{Path.GetFileNameWithoutExtension(SelectedFilePath)}_organized.pdf";

        var chosenPath = await _fileDialogService.ShowSaveFileDialogAsync(
            "Save organized PDF",
            defaultName,
            ".pdf");

        if (!string.IsNullOrWhiteSpace(chosenPath))
        {
            OutputFilePath = chosenPath;
            RefreshCommandStates();
        }
    }

    private void LoadPdf(string filePath)
    {
        var pageCount = _rearrangeService.TryGetPageCount(new PdfFile(filePath));
        if (pageCount is null or 0)
        {
            StatusMessage = "Could not read the selected PDF.";
            return;
        }

        SelectedFilePath = filePath;
        Pages.Clear();

        for (var pageNumber = 1; pageNumber <= pageCount.Value; pageNumber++)
        {
            Pages.Add(new OrganizePageItemViewModel(pageNumber, pageNumber));
        }

        var directory = Path.GetDirectoryName(filePath)
            ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var fileName = $"{Path.GetFileNameWithoutExtension(filePath)}_organized.pdf";
        OutputFilePath = Path.Combine(directory, fileName);
        SelectedPage = Pages.FirstOrDefault();
        StatusMessage = $"Loaded {pageCount.Value} pages. Reorder, rotate, delete, then save.";
        RefreshCommandStates();
    }

    private bool CanViewPage()
    {
        return !string.IsNullOrWhiteSpace(SelectedFilePath) && SelectedPage is not null;
    }

    private async Task ViewPageAsync()
    {
        if (!CanViewPage() || SelectedPage is null)
        {
            return;
        }

        var result = _pagePreviewService.RenderPage(SelectedFilePath, SelectedPage.SourcePageNumber);
        var details = $"Position: {SelectedPage.DisplayPosition}{Environment.NewLine}" +
                      $"Source page: {SelectedPage.SourcePageNumber}{Environment.NewLine}" +
                      $"Rotation: {SelectedPage.RotationLabel}{Environment.NewLine}" +
                      $"Included: {SelectedPage.IncludedLabel}";

        await _pagePreviewDialogService.ShowAsync(new PagePreviewRequest(
            $"Page {SelectedPage.SourcePageNumber}",
            details,
            result.ImagePngBytes,
            result.IsSuccess ? string.Empty : result.ErrorMessage));
    }

    private void ResetPages()
    {
        if (string.IsNullOrWhiteSpace(SelectedFilePath))
        {
            return;
        }

        LoadPdf(SelectedFilePath);
        StatusMessage = "Page list reset to original order.";
    }

    private bool CanMoveSelected(int delta)
    {
        if (SelectedPage is null)
        {
            return false;
        }

        var index = Pages.IndexOf(SelectedPage);
        var targetIndex = index + delta;
        return index >= 0 && targetIndex >= 0 && targetIndex < Pages.Count;
    }

    private void MoveSelected(int delta)
    {
        if (SelectedPage is null || !CanMoveSelected(delta))
        {
            return;
        }

        var index = Pages.IndexOf(SelectedPage);
        var targetIndex = index + delta;
        Pages.Move(index, targetIndex);
        RefreshDisplayPositions();
        RefreshCommandStates();
    }

    private void RotateSelected(int delta)
    {
        if (SelectedPage is null)
        {
            return;
        }

        SelectedPage.RotationDegrees += delta;
        RefreshCommandStates();
    }

    private void SetSelectedIncluded(bool included)
    {
        if (SelectedPage is null)
        {
            return;
        }

        SelectedPage.IsIncluded = included;
        RefreshCommandStates();
    }

    private void RefreshDisplayPositions()
    {
        for (var i = 0; i < Pages.Count; i++)
        {
            Pages[i].DisplayPosition = i + 1;
        }
    }

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(SelectedFilePath)
            && !string.IsNullOrWhiteSpace(OutputFilePath)
            && Pages.Any(page => page.IsIncluded);
    }

    private void Save()
    {
        if (!CanSave())
        {
            StatusMessage = "Select a PDF, include at least one page, and choose an output path.";
            return;
        }

        var includedPages = Pages
            .Where(page => page.IsIncluded)
            .Select(page => new PageArrangementItem(page.SourcePageNumber - 1, page.RotationDegrees))
            .ToList();

        if (includedPages.Count == 0)
        {
            StatusMessage = "At least one page must be included.";
            return;
        }

        var outputDirectory = Path.GetDirectoryName(OutputFilePath);
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            StatusMessage = "Invalid output path.";
            return;
        }

        var result = _rearrangeService.Rearrange(
            new PdfFile(SelectedFilePath),
            includedPages,
            new RearrangeOptions(outputDirectory));

        if (!result.IsSuccess)
        {
            StatusMessage = result.ErrorMessage;
            return;
        }

        var finalPath = OutputFileHelper.MoveToExpectedPath(result.OutputPath, OutputFilePath);
        StatusMessage = finalPath is not null
            ? $"Saved organized PDF: {finalPath}"
            : result.ErrorMessage;
    }

    private void RefreshCommandStates()
    {
        (MoveUpCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        (MoveDownCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        (RotateLeftCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        (RotateRightCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        (DeleteCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        (RestoreCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        (ResetCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        (ViewPageCommand as DelegateCommand)?.RaiseCanExecuteChanged();
        (SaveCommand as DelegateCommand)?.RaiseCanExecuteChanged();
    }

    private sealed class DelegateCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public DelegateCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _execute(parameter);

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
