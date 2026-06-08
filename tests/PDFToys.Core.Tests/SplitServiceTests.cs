using PDFToys.Core.Models;
using PDFToys.Core.Services;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PDFToys.Core.Tests
{
    public sealed class SplitServiceTests : PdfTestBase
    {
        private readonly SplitService _service;
        public SplitServiceTests()
        {
            _service = new SplitService();
        }

        [Fact]
        public void Split_MissingInput_ReturnsFailure()
        {
            var missingPath = Path.Combine(TempDirectory, "does-not-exist.pdf");
            var options = new SplitOptions(TempDirectory, 1);

            var result = _service.Split(new PdfFile(missingPath), options);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Split_InvalidSplitEveryPages_ReturnsFailure()
        {
            var inputPath = Path.Combine(TempDirectory, "input.pdf");
            CreatePdf(inputPath, 3);
            var options = new SplitOptions(TempDirectory, 0);

            var result = _service.Split(new PdfFile(inputPath), options);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Split_CustomRange_PageBelowOne_ReturnsFailure()
        {
            var inputPath = Path.Combine(TempDirectory, "input.pdf");
            CreatePdf(inputPath, 5);
            var options = new SplitOptions(TempDirectory, 0, [new SplitRange(0, 2)]);

            var result = _service.Split(new PdfFile(inputPath), options);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Split_CustomRange_EndBeforeStart_ReturnsFailure()
        {
            var inputPath = Path.Combine(TempDirectory, "input.pdf");
            CreatePdf(inputPath, 5);
            var options = new SplitOptions(TempDirectory, 0, [new SplitRange(5, 3)]);

            var result = _service.Split(new PdfFile(inputPath), options);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Split_CustomRange_ExceedsPageCount_ReturnsFailure()
        {
            var inputPath = Path.Combine(TempDirectory, "input.pdf");
            CreatePdf(inputPath, 3);
            var options = new SplitOptions(TempDirectory, 0, [new SplitRange(1, 5)]);

            var result = _service.Split(new PdfFile(inputPath), options);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Split_CustomRange_Overlapping_ReturnsFailure()
        {
            var inputPath = Path.Combine(TempDirectory, "input.pdf");
            CreatePdf(inputPath, 6);
            var options = new SplitOptions(
                TempDirectory,
                0,
                [new SplitRange(1, 3), new SplitRange(3, 5)]);

            var result = _service.Split(new PdfFile(inputPath), options);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Split_InvalidPdf_ReturnsFailure()
        {
            var inputPath = Path.Combine(TempDirectory, "invalid.pdf");
            File.WriteAllText(inputPath, "this is not a valid pdf");
            var options = new SplitOptions(TempDirectory, 1);

            var result = _service.Split(new PdfFile(inputPath), options);

            Assert.False(result.IsSuccess);
            Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
        }

        [Fact]
        public void Split_EveryNPages_CreatesExpectedParts()
        {
            var inputPath = Path.Combine(TempDirectory, "ten-pages.pdf");
            CreatePdf(inputPath, 10);
            var outputDirectory = Path.Combine(TempDirectory, "every-n-output");
            var options = new SplitOptions(outputDirectory, 3);

            var result = _service.Split(new PdfFile(inputPath), options);

            Assert.True(result.IsSuccess, result.ErrorMessage);

            var outputFiles = Directory.GetFiles(outputDirectory, "ten-pages_Part*.pdf")
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            Assert.Equal(4, outputFiles.Length);
            AssertPageCounts(outputFiles, [3, 3, 3, 1]);
        }

        [Fact]
        public void Split_EveryNPages_SinglePagePdf()
        {
            var inputPath = Path.Combine(TempDirectory, "single.pdf");
            CreatePdf(inputPath, 1);
            var outputDirectory = Path.Combine(TempDirectory, "single-output");
            var options = new SplitOptions(outputDirectory, 1);

            var result = _service.Split(new PdfFile(inputPath), options);

            Assert.True(result.IsSuccess, result.ErrorMessage);

            var outputFiles = Directory.GetFiles(outputDirectory, "single_Part*.pdf");
            Assert.Single(outputFiles);
            AssertPageCounts(outputFiles, [1]);
        }

        [Fact]
        public void Split_CustomRanges_CreatesNamedParts()
        {
            var inputPath = Path.Combine(TempDirectory, "six-pages.pdf");
            CreatePdf(inputPath, 6);
            var outputDirectory = Path.Combine(TempDirectory, "custom-output");
            var options = new SplitOptions(
                outputDirectory,
                0,
                [new SplitRange(1, 2), new SplitRange(5, 6)]);

            var result = _service.Split(new PdfFile(inputPath), options);

            Assert.True(result.IsSuccess, result.ErrorMessage);

            var partOne = Path.Combine(outputDirectory, "six-pages_Part1_p1-2.pdf");
            var partTwo = Path.Combine(outputDirectory, "six-pages_Part2_p5-6.pdf");
            Assert.True(File.Exists(partOne));
            Assert.True(File.Exists(partTwo));
            AssertPageCounts([partOne, partTwo], [2, 2]);
        }

        [Fact]
        public void Split_CustomRanges_SinglePageTokens()
        {
            var inputPath = Path.Combine(TempDirectory, "pages.pdf");
            CreatePdf(inputPath, 5);
            var outputDirectory = Path.Combine(TempDirectory, "single-token-output");
            var options = new SplitOptions(outputDirectory, 0, [new SplitRange(3, 3)]);

            var result = _service.Split(new PdfFile(inputPath), options);

            Assert.True(result.IsSuccess, result.ErrorMessage);

            var outputFile = Path.Combine(outputDirectory, "pages_Part1_p3-3.pdf");
            Assert.True(File.Exists(outputFile));
            AssertPageCounts([outputFile], [1]);
        }

        [Fact]
        public void Split_HalfPreset_SplitsIntoTwoBalancedParts()
        {
            var inputPath = Path.Combine(TempDirectory, "eight-pages.pdf");
            CreatePdf(inputPath, 8);
            var outputDirectory = Path.Combine(TempDirectory, "half-output");
            var options = new SplitOptions(outputDirectory, 0, null, SplitPreset.Half);

            var result = _service.Split(new PdfFile(inputPath), options);

            Assert.True(result.IsSuccess, result.ErrorMessage);

            var outputFiles = Directory.GetFiles(outputDirectory, "eight-pages_Part*.pdf")
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            Assert.Equal(2, outputFiles.Length);
            AssertPageCounts(outputFiles, [4, 4]);
        }

        [Fact]
        public void Split_HalfPreset_DistributesRemainder()
        {
            var inputPath = Path.Combine(TempDirectory, "five-pages.pdf");
            CreatePdf(inputPath, 5);
            var outputDirectory = Path.Combine(TempDirectory, "half-remainder-output");
            var options = new SplitOptions(outputDirectory, 0, null, SplitPreset.Half);

            var result = _service.Split(new PdfFile(inputPath), options);

            Assert.True(result.IsSuccess, result.ErrorMessage);

            var outputFiles = Directory.GetFiles(outputDirectory, "five-pages_Part*.pdf")
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            Assert.Equal(2, outputFiles.Length);
            AssertPageCounts(outputFiles, [3, 2]);
        }

        [Fact]
        public void Split_QuarterPreset_SplitsIntoFourParts()
        {
            var inputPath = Path.Combine(TempDirectory, "eight-pages-quarter.pdf");
            CreatePdf(inputPath, 8);
            var outputDirectory = Path.Combine(TempDirectory, "quarter-output");
            var options = new SplitOptions(outputDirectory, 0, null, SplitPreset.Quarter);

            var result = _service.Split(new PdfFile(inputPath), options);

            Assert.True(result.IsSuccess, result.ErrorMessage);

            var outputFiles = Directory.GetFiles(outputDirectory, "eight-pages-quarter_Part*.pdf")
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            Assert.Equal(4, outputFiles.Length);
            AssertPageCounts(outputFiles, [2, 2, 2, 2]);
        }
    }
}
