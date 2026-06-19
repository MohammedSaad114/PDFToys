using PdfSharp.Pdf.IO;
using PDFToys.Core.Models;
using PDFToys.Core.Services;
using PDFToys.Core.Services.ImageProcessing;

namespace PDFToys.Core.Tests;

public sealed class CompressionServiceTests : PdfTestBase
{
    private readonly CompressionService _service;

    public CompressionServiceTests()
    {
        _service = new CompressionService(new SkiaImageCompressor());
    }

    [Theory]
    [InlineData(CompressionLevel.Normal)]
    [InlineData(CompressionLevel.Maximum)]
    public void Compress_AllCompressionLevels_ExecuteSuccessfully(CompressionLevel level)
    {
        var inputPath = CreatePdf($"level-test-{level}.pdf", 2);
        var inputSize = new FileInfo(inputPath).Length;
        var options = new CompressionOptions(TempDirectory, level);

        var result = _service.Compress(new PdfFile(inputPath), options);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.True(File.Exists(result.OutputPath));
        Assert.True(new FileInfo(result.OutputPath).Length <= inputSize);
    }

    [Fact]
    public void Compress_WithEmbeddedImage_ProducesValidPdf()
    {
        var inputPath = CreatePdfWithEmbeddedImage("image.pdf", 200, 200);
        var options = new CompressionOptions(TempDirectory, CompressionLevel.Normal);

        var result = _service.Compress(new PdfFile(inputPath), options);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        using var document = PdfReader.Open(result.OutputPath, PdfDocumentOpenMode.Import);
        Assert.Equal(1, document.PageCount);
    }

    [Fact]
    public void Compress_WithEmbeddedImage_OutputIsNotLargerThanInput()
    {
        var inputPath = CreatePdfWithEmbeddedImage("large-image.pdf", 400, 400);
        var inputSize = new FileInfo(inputPath).Length;
        var options = new CompressionOptions(TempDirectory, CompressionLevel.Maximum);

        var result = _service.Compress(new PdfFile(inputPath), options);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        var outputSize = new FileInfo(result.OutputPath).Length;
        Assert.True(outputSize <= inputSize, $"Output ({outputSize} bytes) should not exceed input ({inputSize} bytes).");
    }
}
