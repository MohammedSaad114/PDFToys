using System;

namespace PDFToys.App.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private ViewModelBase _currentPage = null!;
    private readonly string? _startupWorkspace;

    public MainViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        CurrentPage = CreateHomeViewModel(_startupWorkspace);
    }

    public ViewModelBase CurrentPage
    {
        get => _currentPage;
        set
        {
            if (ReferenceEquals(_currentPage, value))
            {
                return;
            }

            _currentPage = value;
            OnPropertyChanged();
        }
    }

    public void NavigateTo(ViewModelBase page)
    {
        CurrentPage = page;
    }

    private HomeViewModel CreateHomeViewModel(string? workspaceDirectory)
    {
        return new HomeViewModel(
            NavigateTo,
            workspaceDirectory);
    }

}
