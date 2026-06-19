using PDFToys.Core.Contracts;
using PDFToys.Core.Models;
using PDFToys.Core.OS.Windows;
using PDFToys.Core.Services;
using PDFToys.Core.Services.ConversionStrategies;
using PdfSharp.Pdf.IO;
using SkiaSharp;

namespace PDFToys.Core.Tests;

public sealed class ConversionServiceTests : PdfTestBase
{
    private readonly IConversionService _service;

    public ConversionServiceTests()
    {
        if (OperatingSystem.IsWindows())
        {
            PdfSharp.Fonts.GlobalFontSettings.UseWindowsFontsUnderWindows = true;
        }

        var strategies = new IConversionStrategy[]
        {
            new ImageToPdfStrategy(),
            new TextToPdfStrategy(),
            new SvgToPdfStrategy(),
            new OfficeComPdfConverter()
        };

        _service = new ConversionService(strategies);
    }

    [Fact]
    public void Convert_UnsupportedExtension_ReturnsFailure()
    {
        var inputPath = Path.Combine(TempDirectory, "file.xyz");
        File.WriteAllText(inputPath, "content");

        var result = _service.Convert(inputPath, new ConversionOptions(TempDirectory));

        Assert.False(result.IsSuccess);
        Assert.Contains("Unsupported file type", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("sample.txt")]
    [InlineData("sample.md")]
    public void Convert_TextFile_CreatesPdf(string fileName)
    {
        var inputPath = Path.Combine(TempDirectory, fileName);
        File.WriteAllText(inputPath, "# Title\n\nHello from PDFToys.");

        var result = _service.Convert(inputPath, new ConversionOptions(TempDirectory));

        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(result.OutputPath));
        AssertValidPdf(result.OutputPath);
    }

    [Fact]
    public void Convert_Png_CreatesPdf()
    {
        var inputPath = Path.Combine(TempDirectory, "sample.png");
        CreatePng(inputPath, 120, 80);

        var result = _service.Convert(inputPath, new ConversionOptions(TempDirectory));

        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(result.OutputPath));
        AssertValidPdf(result.OutputPath);
    }

    [Fact]
    public void Convert_Jpg_CreatesPdf()
    {
        var inputPath = Path.Combine(TempDirectory, "sample.jpg");
        CreateJpeg(inputPath, 100, 60);

        var result = _service.Convert(inputPath, new ConversionOptions(TempDirectory));

        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(result.OutputPath));
        AssertValidPdf(result.OutputPath);
    }

    [Theory]
    [InlineData("slides.pptx")]
    [InlineData("slides.ppt")]
    public void Convert_Presentation_WhenPowerPointInstalled_CreatesPdf(string fileName)
    {
        if (!IsPowerPointInstalled())
        {
            return;
        }

        var fixturePath = ResolveFixturePath(fileName);
        if (fixturePath is null)
        {
            return;
        }

        var inputPath = Path.Combine(TempDirectory, fileName);
        File.Copy(fixturePath, inputPath, overwrite: true);

        var result = _service.Convert(inputPath, new ConversionOptions(TempDirectory));

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.True(File.Exists(result.OutputPath));
        AssertValidPdf(result.OutputPath);
    }

    [Fact]
    public void Convert_Pptx_WhenPowerPointMissing_FailsGracefully()
    {
        if (IsPowerPointInstalled())
        {
            return;
        }

        var inputPath = Path.Combine(TempDirectory, "sample.pptx");
        File.WriteAllText(inputPath, "placeholder");

        var result = _service.Convert(inputPath, new ConversionOptions(TempDirectory));

        Assert.False(result.IsSuccess);
        Assert.Contains("PowerPoint", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Convert_Svg_CreatesPdf()
    {
        var inputPath = Path.Combine(TempDirectory, "sample.svg");
        File.WriteAllText(
            inputPath,
            """
            <svg xmlns="http://www.w3.org/2000/svg" width="100" height="80">
              <rect x="10" y="10" width="80" height="60" fill="blue"/>
            </svg>
            """);

        var result = _service.Convert(inputPath, new ConversionOptions(TempDirectory));

        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(result.OutputPath));
        AssertValidPdf(result.OutputPath);
    }

    private static bool IsPowerPointInstalled() =>
        Type.GetTypeFromProgID("PowerPoint.Application", throwOnError: false) is not null;

    private static void AssertValidPdf(string outputPath)
    {
        using var document = PdfReader.Open(outputPath, PdfDocumentOpenMode.Import);
        Assert.True(document.PageCount > 0);
    }

    private static void CreatePng(string path, int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Coral);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(path);
        data!.SaveTo(stream);
    }

    private static void CreateJpeg(string path, int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.LightGreen);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        using var stream = File.Create(path);
        data!.SaveTo(stream);
    }

    private static string? ResolveFixturePath(string fileName)
    {
        var baseDirectory = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDirectory, "Fixtures", "Presentations", fileName),
            Path.Combine(baseDirectory, "..", "..", "..", "Fixtures", "Presentations", fileName),
        };

        foreach (var candidate in candidates)
        {
            var fullPath = Path.GetFullPath(candidate);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }
}