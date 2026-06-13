using PdfSharp;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PDFToys.Core.Contracts;
using PDFToys.Core.Models;

namespace PDFToys.Core.Services;

public sealed class RearrangeService : ServiceBase, IRearrangeService
{
    /// <summary>
    /// Creates a new PDF from the requested page order and optional per-page rotations.
    /// </summary>
    /// <param name="input">The source PDF file.</param>
    /// <param name="pages">Ordered list of source pages using 0-based indices.</param>
    /// <param name="options">The destination options for the rearranged PDF.</param>
    /// <returns>An OperationResult containing the path to the rearranged file.</returns>
    public OperationResult Rearrange(PdfFile input, IReadOnlyList<PageArrangementItem> pages, RearrangeOptions options)
    {
        return ExecuteSafe(() =>
        {
            var optionsError = ValidateOptionsNotNull(options);
            if (optionsError != null)
            {
                return optionsError;
            }

            var validationError = ValidateStandardInputs(input, options!.OutputDirectory);
            if (validationError != null)
            {
                return validationError;
            }

            if (pages is null || pages.Count == 0)
            {
                return new OperationResult(false, string.Empty, "At least one page must be included.");
            }

            var outputPath = PrepareOutputEnvironment(input.FilePath, options.OutputDirectory, "Rearranged");

            using var inputDocument = PdfReader.Open(input.FilePath, PdfDocumentOpenMode.Import);
            using var outputDocument = new PdfDocument();

            foreach (var pageItem in pages)
            {
                if (pageItem.SourcePageIndex < 0 || pageItem.SourcePageIndex >= inputDocument.PageCount)
                {
                    return new OperationResult(
                        false,
                        string.Empty,
                        $"Page index out of range: {pageItem.SourcePageIndex} (0-based).");
                }

                var page = outputDocument.AddPage(inputDocument.Pages[pageItem.SourcePageIndex]);
                page.Rotation = ToPageRotation(pageItem.RotationDegrees);
            }

            outputDocument.Save(outputPath);

            return new OperationResult(true, Path.GetFullPath(outputPath), string.Empty);
        });
    }

    private static PageRotation ToPageRotation(int rotationDegrees)
    {
        var normalized = ((rotationDegrees % 360) + 360) % 360;
        return normalized switch
        {
            90 => PageRotation.Rotate90DegreesRight,
            180 => PageRotation.RotateUpsideDown,
            270 => PageRotation.Rotate90DegreesLeft,
            _ => PageRotation.None
        };
    }
}
