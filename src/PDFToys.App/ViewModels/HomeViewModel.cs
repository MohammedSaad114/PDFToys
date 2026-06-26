using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

namespace PDFToys.App.ViewModels;

public sealed class HomeViewModel : ViewModelBase
{
    private string _currentWorkspace = string.Empty;
    private readonly Action<ViewModelBase> _navigateAction;

    public HomeViewModel(
        Action<ViewModelBase> navigateAction,
        string? directoryPath = null)
    {
        _navigateAction = navigateAction;
        AvailableFiles = [];
        
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            return;
        }

        CurrentWorkspace = directoryPath;
        foreach (var pdfPath in Directory.GetFiles(directoryPath, "*.pdf"))
        {
            var fileName = Path.GetFileName(pdfPath);
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                AvailableFiles.Add(fileName);
            }
        }
    }

    public string Message => "Open a PDF tool for this workspace";

    public string SecondaryMessage => string.IsNullOrWhiteSpace(CurrentWorkspace)
        ? "Select a folder workspace to begin."
        : "Choose a tool to continue.";

    public string CurrentWorkspace
    {
        get => _currentWorkspace;
        set
        {
            if (_currentWorkspace == value)
            {
                return;
            }

            _currentWorkspace = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SecondaryMessage));
        }
    }

    public ObservableCollection<string> AvailableFiles { get; }

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
}
