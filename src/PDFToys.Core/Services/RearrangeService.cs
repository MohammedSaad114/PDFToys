using PDFToys.Core.Contracts;
using PDFToys.Core.Models;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PDFToys.Core.Services;

// TODO: Add support for page transformations (e.g., rotating specific pages).
public sealed class RearrangeService : ServiceBase, IRearrangeService
{
    /// <summary>
    /// Creates a new PDF based on the requested page order.
    /// </summary>
    /// <param name="input">The source PDF file.</param>
    /// <param name="newPageOrder">An array of 0-based page indices representing the new order. (e.g., 0 = Page 1).</param>
    /// <param name="outputDirectory">The destination folder for the rearranged PDF.</param>
    /// <returns>An OperationResult containing the path to the rearranged file.</returns>
    public OperationResult Rearrange(PdfFile input, int[] newPageOrder, string outputDirectory)
    {
        return ExecuteSafe(() =>
        {
            // Validation 
            var validationError = ValidateStandardInputs(input, outputDirectory);
            if (validationError != null)
            {
                return validationError;
            }

            if (newPageOrder is null || newPageOrder.Length == 0)
            {
                return new OperationResult(false, string.Empty, "newPageOrder must contain at least one page index.");
            }

            var outputPath = PrepareOutputEnvironment(input.FilePath, outputDirectory, "Rearranged");

            using var inputDocument = PdfReader.Open(input.FilePath, PdfDocumentOpenMode.Import);
            using var outputDocument = new PdfDocument();

            foreach (var requestedIndex in newPageOrder)
            {
                if (requestedIndex < 0 || requestedIndex >= inputDocument.PageCount)
                {
                    return new OperationResult(false, string.Empty, $"Page index out of range: {requestedIndex} (0-based).");
                }

                outputDocument.AddPage(inputDocument.Pages[requestedIndex]);
            }

            outputDocument.Save(outputPath);

            return new OperationResult(true, Path.GetFullPath(outputPath), string.Empty);
        });
    }
}