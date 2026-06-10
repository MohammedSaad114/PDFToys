using PDFToys.Core.Contracts;
using PDFToys.Core.Models;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PDFToys.Core.Services;

// TODO: Add support for page transformations (e.g., rotating specific pages).
public sealed class RearrangeService : IRearrangeService
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
        try
        {
            if (!File.Exists(input.FilePath))
            {
                return new OperationResult(false, string.Empty, $"Input file not found: {input.FilePath}");
            }

            if (newPageOrder is null || newPageOrder.Length == 0)
            {
                return new OperationResult(false, string.Empty, "newPageOrder must contain at least one page index.");
            }

            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                return new OperationResult(false, string.Empty, "Output directory is required.");
            }

            using var inputDocument = PdfReader.Open(input.FilePath, PdfDocumentOpenMode.Import);
            using var outputDocument = new PdfDocument();

            foreach (var requestedIndex in newPageOrder)
            {
                if (requestedIndex < 0 || requestedIndex >= inputDocument.PageCount)
                {
                    return new OperationResult(false, string.Empty, $"Page index out of range: {requestedIndex}");
                }

                outputDocument.AddPage(inputDocument.Pages[requestedIndex]);
            }

            Directory.CreateDirectory(outputDirectory);

            var originalFileName = Path.GetFileNameWithoutExtension(input.FilePath);
            var outputPath = Path.Combine(outputDirectory, $"{originalFileName}_Rearranged.pdf");
            outputDocument.Save(outputPath);

            return new OperationResult(true, Path.GetFullPath(outputPath), string.Empty);
        }
        catch (Exception ex)
        {
            return new OperationResult(false, string.Empty, ex.Message);
        }
    }
}