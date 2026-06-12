using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PDFToys.Core.Models;
using PDFToys.Core.Services;

namespace PDFToys.Core.Tests;

public sealed class MergeServiceTests : PdfTestBase
{
    private readonly MergeService _sut; // System Under Test

    public MergeServiceTests()
    {
        _sut = new MergeService();
    }

    /* 
     * testing the Dispose function
     */
    [Fact]
    public void PdfTestBase_Cleanup_DeletesAllFilesAfterTest()
    {
        var testFile = Path.Combine(TempDirectory, "cleanup-test.pdf");
        File.WriteAllText(testFile, "temporary content");
        // the directory must be empty in the subsequent test.
    }

    [Fact]
    public void PdfTestBase_DirectoryIsEmpty_OnStart()
    {
        var files = Directory.GetFiles(TempDirectory);
        Assert.Empty(files);
    }

    /* 
     * Testing the marging functionality
     */
    [Fact]
    public void Merge_ValidInputs_ReturnsSuccessAndCreatesFile()
    {
        var inputOne = new PdfFile { FilePath = CreatePdf("input-one.pdf", 2) };
        var inputTwo = new PdfFile { FilePath = CreatePdf("input-two.pdf", 3) };
        var options = new MergeOptions { OutputDirectory = TempDirectory, OutputFileName = "merged.pdf" };

        var result = _sut.Merge([inputOne, inputTwo], options);

        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(result.OutputPath));

        using var merged = PdfReader.Open(result.OutputPath, PdfDocumentOpenMode.Import);
        Assert.Equal(5, merged.PageCount);
    }

    [Fact]
    public void Merge_EmptyInputArray_ReturnsFailureResult()
    {
        var options = new MergeOptions { OutputDirectory = TempDirectory, OutputFileName = "merged.pdf" };

        var result = _sut.Merge([], options);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Merge_InvalidPdf_ReturnsFailureResult()
    {
        var invalidPdf = Path.Combine(TempDirectory, "invalid.pdf");
        File.WriteAllText(invalidPdf, "not a real pdf");
        var options = new MergeOptions { OutputDirectory = TempDirectory, OutputFileName = "merged.pdf" };

        var result = _sut.Merge([new PdfFile { FilePath = invalidPdf }], options);

        Assert.False(result.IsSuccess);
    }
}