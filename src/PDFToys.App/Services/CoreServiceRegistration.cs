using Microsoft.Extensions.DependencyInjection;
using PDFToys.Core.Contracts;
using PDFToys.Core.OS.Windows;
using PDFToys.Core.Services;
using PDFToys.Core.Services.ExportStrategies;
using PDFToys.Core.Services.ImageProcessing;
using PDFToys.Core.Services.ConversionStrategies;

namespace PDFToys.App.Services;

public static class CoreServiceRegistration
{
    public static IServiceCollection AddPdfToysCore(this IServiceCollection services)
    {
        services.AddSingleton<IImageCompressor, SkiaImageCompressor>();
        services.AddSingleton<IConversionStrategy, ImageToPdfStrategy>();
        services.AddSingleton<IConversionStrategy, TextToPdfStrategy>();
        services.AddSingleton<IConversionStrategy, SvgToPdfStrategy>();
        services.AddSingleton<IConversionStrategy, OfficeComPdfConverter>();
        services.AddSingleton<IExportStrategy, ImageExportStrategy>();
        services.AddSingleton<IExportStrategy, MarkdownExportStrategy>();

        services.AddSingleton<IMergeService, MergeService>();
        services.AddSingleton<ISplitService, SplitService>();
        services.AddSingleton<ICompressionService, CompressionService>();
        services.AddSingleton<IProtectService, ProtectService>();
        services.AddSingleton<IRearrangeService, RearrangeService>();
        services.AddSingleton<IConversionService, ConversionService>();
        services.AddSingleton<IExportService, ExportService>();

        return services;
    }
}
