using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PDFToys.App.Services;
using PDFToys.App.ViewModels;
using System;

namespace PDFToys.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public App() { }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFileDialogService, AvaloniaFileDialogService>();
        services.AddTransient<MainViewModel>();

        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var args = desktop.Args ?? [];


            var mainViewModel = new MainViewModel(Services);

            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };

        }

        base.OnFrameworkInitializationCompleted();
    }
}
