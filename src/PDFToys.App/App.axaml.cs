using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PDFToys.App.Models;
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
        services.AddPdfToysCore();
        services.AddSingleton<IFileDialogService, AvaloniaFileDialogService>();
        services.AddSingleton<IPdfPagePreviewService, PdfPagePreviewService>();
        services.AddSingleton<IPagePreviewDialogService, AvaloniaPagePreviewDialogService>();
        services.AddSingleton<IUserSettingsStore, JsonUserSettingsStore>();
        services.AddSingleton<StartupOperationRouter>();
        services.AddSingleton<HeadlessConversionExecutor>();
        services.AddTransient<MergeViewModel>();
        services.AddTransient<SplitViewModel>();
        services.AddTransient<CompressViewModel>();
        services.AddTransient<ProtectViewModel>();
        services.AddTransient<OrganizePagesViewModel>();
        services.AddTransient<UnlockViewModel>();
        services.AddTransient<ConvertExportViewModel>();
        services.AddTransient<StartupOperationViewModel>();
        services.AddTransient<MainViewModel>();

        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var args = desktop.Args ?? [];
            var request = new ArgumentParser().Parse(args);
            var startupRouter = Services.GetRequiredService<StartupOperationRouter>();
            var route = startupRouter.BuildRoute(request);

            if (route.Kind == StartupRouteKind.HeadlessConversion)
            {
                var executor = Services.GetRequiredService<HeadlessConversionExecutor>();
                var result = executor.Execute(route);
                Environment.ExitCode = result.ExitCode;
                desktop.Shutdown(result.ExitCode);
                base.OnFrameworkInitializationCompleted();
                return;
            }

            var mainViewModel = new MainViewModel(Services, request);

            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };

        }

        base.OnFrameworkInitializationCompleted();
    }
}
