using PDFToys.Core.Contracts;
using PDFToys.Core.Models;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PDFToys.Core.Services;

public sealed class SplitService : ISplitPdfService
{
    /// <summary>
    /// Splits the specified input PDF based on the provided configuration options.
    /// </summary>
    /// <param name="input">The input PDF file to split.</param>
    /// <param name="options">The configuration defining how to split the document (e.g., chunk size, custom ranges).</param>
    /// <returns>An OperationResult indicating success or failure, containing the output directory path if successful.</returns>
    public OperationResult Split(PdfFile input, SplitOptions options)
    {
        try
        {
            if (!File.Exists(input.FilePath))
            {
                return new OperationResult(false, string.Empty, $"Input file not found: {input.FilePath}");
            }

            Directory.CreateDirectory(options.OutputDirectory);

            using var inputDocument = PdfReader.Open(input.FilePath, PdfDocumentOpenMode.Import);
            var originalFileName = Path.GetFileNameWithoutExtension(input.FilePath);

            if (options.CustomRanges is { Count: > 0 })
            {
                var rangeValidation = ValidateRanges(options.CustomRanges, inputDocument.PageCount);
                if (!rangeValidation.IsSuccess)
                {
                    return rangeValidation;
                }

                SaveCustomRanges(inputDocument, options, originalFileName, options.CustomRanges);
                return new OperationResult(true, Path.GetFullPath(options.OutputDirectory), string.Empty);
            }

            if (options.Preset is not SplitPreset.None)
            {
                var presetRanges = BuildPresetRanges(inputDocument.PageCount, options.Preset);
                SaveCustomRanges(inputDocument, options, originalFileName, presetRanges);
                return new OperationResult(true, Path.GetFullPath(options.OutputDirectory), string.Empty);
            }

            if (options.SplitEveryPages <= 0)
            {
                return new OperationResult(false, string.Empty, "SplitEveryPages must be greater than 0.");
            }

            var partNumber = 1;
            var pagesInCurrentPart = 0;
            PdfDocument? outputDocument = null;

            for (var pageIndex = 0; pageIndex < inputDocument.PageCount; pageIndex++)
            {
                if (outputDocument is null || pagesInCurrentPart == options.SplitEveryPages)
                {
                    outputDocument?.Dispose();
                    outputDocument = new PdfDocument();
                    pagesInCurrentPart = 0;
                }

                outputDocument.AddPage(inputDocument.Pages[pageIndex]);
                pagesInCurrentPart++;

                var isChunkComplete = pagesInCurrentPart == options.SplitEveryPages;
                var isLastPage = pageIndex == inputDocument.PageCount - 1;
                if (!isChunkComplete && !isLastPage)
                {
                    continue;
                }
                var outputFileName = $"{originalFileName}_Part{partNumber}.pdf";
                var outputPath = Path.Combine(options.OutputDirectory, outputFileName);
                outputDocument.Save(outputPath);
                partNumber++;
            }

            outputDocument?.Dispose();
            return new OperationResult(true, Path.GetFullPath(options.OutputDirectory), string.Empty);
        }
        catch (Exception ex)
        {
            return new OperationResult(false, string.Empty, ex.Message);
        }
    }

    private static OperationResult ValidateRanges(IReadOnlyList<SplitRange> ranges, int pageCount)
    {
        var ordered = ranges.OrderBy(range => range.StartPage).ToArray();
        for (var i = 0; i < ordered.Length; i++)
        {
            var range = ordered[i];
            if (range.StartPage < 1 || range.EndPage < 1)
            {
                return new OperationResult(false, string.Empty, "Range pages must be greater than or equal to 1.");
            }

            if (range.EndPage < range.StartPage)
            {
                return new OperationResult(false, string.Empty, "EndPage must be greater than or equal to StartPage.");
            }

            if (range.EndPage > pageCount)
            {
                return new OperationResult(false, string.Empty, $"Range exceeds page count ({pageCount}).");
            }

            if (i == 0)
            {
                continue;
            }

            var previous = ordered[i - 1];
            if (range.StartPage <= previous.EndPage)
            {
                return new OperationResult(false, string.Empty, "Custom ranges must not overlap.");
            }
        }

        return new OperationResult(true, string.Empty, string.Empty);
    }

    private static IReadOnlyList<SplitRange> BuildPresetRanges(int pageCount, SplitPreset preset)
    {
        return preset switch
        {
            SplitPreset.Half => BuildEvenRanges(pageCount, 2),
            SplitPreset.Quarter => BuildEvenRanges(pageCount, 4),
            _ => []
        };
    }

    private static IReadOnlyList<SplitRange> BuildEvenRanges(int pageCount, int parts)
    {
        var ranges = new List<SplitRange>(parts);
        var baseSize = pageCount / parts;
        var remainder = pageCount % parts;
        var currentStart = 1;

        for (var part = 0; part < parts; part++)
        {
            var thisPartSize = baseSize + (part < remainder ? 1 : 0);
            if (thisPartSize <= 0)
            {
                continue;
            }

            var end = currentStart + thisPartSize - 1;
            ranges.Add(new SplitRange(currentStart, end));
            currentStart = end + 1;
        }

        return ranges;
    }

    private static void SaveCustomRanges(
        PdfDocument inputDocument,
        SplitOptions options,
        string originalFileName,
        IReadOnlyList<SplitRange> ranges)
    {
        var partNumber = 1;
        foreach (var range in ranges)
        {
            using var outputDocument = new PdfDocument();
            for (var page = range.StartPage; page <= range.EndPage; page++)
            {
                outputDocument.AddPage(inputDocument.Pages[page - 1]);
            }

            var outputFileName = $"{originalFileName}_Part{partNumber}_p{range.StartPage}-{range.EndPage}.pdf";
            var outputPath = Path.Combine(options.OutputDirectory, outputFileName);
            outputDocument.Save(outputPath);
            partNumber++;
        }
    }
}