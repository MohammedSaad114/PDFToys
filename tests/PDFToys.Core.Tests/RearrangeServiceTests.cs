using PDFToys.Core.Models;
using PDFToys.Core.Services;

namespace PDFToys.Core.Tests;

public sealed class RearrangeServiceTests : PdfTestBase
{
    private readonly RearrangeService _service;

    public RearrangeServiceTests()
    {
        _service = new RearrangeService();
    }

    [Fact]
    public void Rearrange_WithNullOptions_ReturnsFailure()
    {
        var inputPath = Path.Combine(TempDirectory, "pages.pdf");
        CreatePdf(inputPath, 3);

        var result = _service.Rearrange(new PdfFile(inputPath), [0, 1, 2], null!);

        Assert.False(result.IsSuccess);
        Assert.Contains("Options are required", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rearrange_ValidPageOrder_CreatesRearrangedCopy()
    {
        var inputPath = Path.Combine(TempDirectory, "pages.pdf");
        CreatePdf(inputPath, 3);
        var options = new RearrangeOptions(TempDirectory);

        var result = _service.Rearrange(new PdfFile(inputPath), [2, 0, 1], options);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.True(File.Exists(result.OutputPath));
        Assert.EndsWith("_Rearranged.pdf", result.OutputPath);
    }

    [Fact]
    public void Rearrange_PageIndexOutOfRange_ReturnsFailure()
    {
        var inputPath = Path.Combine(TempDirectory, "pages.pdf");
        CreatePdf(inputPath, 2);
        var options = new RearrangeOptions(TempDirectory);

        var result = _service.Rearrange(new PdfFile(inputPath), [0, 2], options);

        Assert.False(result.IsSuccess);
        Assert.Contains("out of range", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rearrange_MissingInput_ReturnsFailure()
    {
        var missingPath = Path.Combine(TempDirectory, "ghost.pdf");
        var options = new RearrangeOptions(TempDirectory);

        var result = _service.Rearrange(new PdfFile(missingPath), [0], options);

        Assert.False(result.IsSuccess);
    }
}
