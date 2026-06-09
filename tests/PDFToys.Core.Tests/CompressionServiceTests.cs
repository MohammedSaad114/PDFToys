using PDFToys.Core.Models;
using PDFToys.Core.Services;

namespace PDFToys.Core.Tests;

public sealed class CompressServiceTests : PdfTestBase
{
    private readonly CompressionService _service;

    public CompressServiceTests()
    {
        _service = new CompressionService();
    }

    [Fact]
    public void Compress_MissingInput_ReturnsFailure()
    {
        var missingPath = Path.Combine(TempDirectory, "ghost.pdf");
        var options = new CompressionOptions(TempDirectory, CompressionLevel.Normal);

        var result = _service.Compress(new PdfFile(missingPath), options);

        Assert.False(result.IsSuccess);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Compress_MissingOutputDirectory_ReturnsFailure(string invalidDirectory)
    {
        var inputPath = Path.Combine(TempDirectory, "input.pdf");
        CreatePdf(inputPath, 1);
        var options = new CompressionOptions(invalidDirectory, CompressionLevel.Normal);

        var result = _service.Compress(new PdfFile(inputPath), options);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Compress_InvalidPdf_ReturnsFailure()
    {
        var inputPath = Path.Combine(TempDirectory, "corrupt.pdf");
        File.WriteAllText(inputPath, "This is just a text file, not a real PDF.");
        var options = new CompressionOptions(TempDirectory, CompressionLevel.Normal);

        var result = _service.Compress(new PdfFile(inputPath), options);

        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.ErrorMessage); // Ensures the try/catch handled it
    }

    [Fact]
    public void Compress_ValidInput_CreatesCompressedCopy()
    {
        var inputPath = Path.Combine(TempDirectory, "document.pdf");
        CreatePdf(inputPath, 5);
        var options = new CompressionOptions(TempDirectory, CompressionLevel.Normal);

        var result = _service.Compress(new PdfFile(inputPath), options);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.True(File.Exists(result.OutputPath));
        Assert.EndsWith("_Compressed.pdf", result.OutputPath);
        Assert.NotEqual(inputPath, result.OutputPath);
    }

    [Theory]
    [InlineData(CompressionLevel.Normal)]
    [InlineData(CompressionLevel.Maximum)]
    public void Compress_AllCompressionLevels_ExecuteSuccessfully(CompressionLevel level)
    {
        var inputPath = Path.Combine(TempDirectory, $"level-test-{level}.pdf");
        CreatePdf(inputPath, 2);
        var options = new CompressionOptions(TempDirectory, level);

        var result = _service.Compress(new PdfFile(inputPath), options);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.True(File.Exists(result.OutputPath));
    }
}