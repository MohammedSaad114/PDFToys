using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
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

        var result = _service.Rearrange(
            new PdfFile(inputPath),
            [new PageArrangementItem(0), new PageArrangementItem(1), new PageArrangementItem(2)],
            null!);

        Assert.False(result.IsSuccess);
        Assert.Contains("Options are required", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rearrange_WithNullPages_ReturnsFailure()
    {
        var inputPath = Path.Combine(TempDirectory, "pages.pdf");
        CreatePdf(inputPath, 3);
        var options = new RearrangeOptions(TempDirectory);

        var result = _service.Rearrange(new PdfFile(inputPath), null!, options);

        Assert.False(result.IsSuccess);
        Assert.Contains("At least one page must be included", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rearrange_WithEmptyPages_ReturnsFailure()
    {
        var inputPath = Path.Combine(TempDirectory, "pages.pdf");
        CreatePdf(inputPath, 3);
        var options = new RearrangeOptions(TempDirectory);

        var result = _service.Rearrange(new PdfFile(inputPath), [], options);

        Assert.False(result.IsSuccess);
        Assert.Contains("At least one page must be included", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rearrange_ValidPageOrder_CreatesRearrangedCopy()
    {
        var inputPath = Path.Combine(TempDirectory, "pages.pdf");
        CreatePdf(inputPath, 3);
        var options = new RearrangeOptions(TempDirectory);

        var result = _service.Rearrange(
            new PdfFile(inputPath),
            [new PageArrangementItem(2), new PageArrangementItem(0), new PageArrangementItem(1)],
            options);

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

        var result = _service.Rearrange(
            new PdfFile(inputPath),
            [new PageArrangementItem(0), new PageArrangementItem(2)],
            options);

        Assert.False(result.IsSuccess);
        Assert.Contains("out of range", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rearrange_MissingInput_ReturnsFailure()
    {
        var missingPath = Path.Combine(TempDirectory, "ghost.pdf");
        var options = new RearrangeOptions(TempDirectory);

        var result = _service.Rearrange(new PdfFile(missingPath), [new PageArrangementItem(0)], options);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Rearrange_WithRotation_AppliesRotation()
    {
        var inputPath = Path.Combine(TempDirectory, "rotate.pdf");
        CreatePdf(inputPath, 2);
        var options = new RearrangeOptions(TempDirectory);

        var result = _service.Rearrange(
            new PdfFile(inputPath),
            [new PageArrangementItem(0, 90), new PageArrangementItem(1, 0)],
            options);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.True(File.Exists(result.OutputPath));

        using var outputDocument = PdfReader.Open(result.OutputPath, PdfDocumentOpenMode.Import);
        Assert.Equal(2, outputDocument.PageCount);
    }

    [Fact]
    public void Rearrange_DuplicatePageIndices_Allowed()
    {
        var inputPath = Path.Combine(TempDirectory, "duplicate.pdf");
        CreatePdf(inputPath, 2);
        var options = new RearrangeOptions(TempDirectory);

        var result = _service.Rearrange(
            new PdfFile(inputPath),
            [new PageArrangementItem(0), new PageArrangementItem(0)],
            options);

        Assert.True(result.IsSuccess, result.ErrorMessage);

        using var outputDocument = PdfReader.Open(result.OutputPath, PdfDocumentOpenMode.Import);
        Assert.Equal(2, outputDocument.PageCount);
    }

    [Fact]
    public void TryGetPageCount_ValidPdf_ReturnsPageCount()
    {
        var inputPath = Path.Combine(TempDirectory, "count.pdf");
        CreatePdf(inputPath, 3);

        var pageCount = _service.TryGetPageCount(new PdfFile(inputPath));

        Assert.Equal(3, pageCount);
    }

    [Fact]
    public void TryGetPageCount_MissingFile_ReturnsNull()
    {
        var missingPath = Path.Combine(TempDirectory, "missing.pdf");

        Assert.Null(_service.TryGetPageCount(new PdfFile(missingPath)));
    }

    [Fact]
    public void TryGetPageCount_NonPdfExtension_ReturnsNull()
    {
        var textPath = Path.Combine(TempDirectory, "notes.txt");
        File.WriteAllText(textPath, "not a pdf");

        Assert.Null(_service.TryGetPageCount(new PdfFile(textPath)));
    }

    [Fact]
    public void TryGetPageCount_NullOrEmptyPath_ReturnsNull()
    {
        Assert.Null(_service.TryGetPageCount(new PdfFile(string.Empty)));
    }
}
