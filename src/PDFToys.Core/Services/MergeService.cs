using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PDFToys.Core.Contracts;
using PDFToys.Core.Models;
using System.Text;

namespace PDFToys.Core.Services;

public sealed class PdfSharpMergeService : IMergeService
{
    // Required for PDFsharp to parse legacy Windows-1252 fonts in modern .NET (8+)
    static PdfSharpMergeService()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// Merges multiple PDF files into a single output document.
    /// </summary>
    /// <param name="inputs">An array of PDF files to merge.</param>
    /// <param name="options">Configuration for the output destination.</param>
    /// <returns>An OperationResult indicating success or failure.</returns>
    public OperationResult Merge(PdfFile[] inputs, MergeOptions options)
    {
        try
        {
            if (inputs is null || inputs.Length == 0)
            {
                return new OperationResult(false, string.Empty, "At least one input file is required.");
            }

            using var outputDocument = new PdfDocument();

            foreach (var file in inputs)
            {
                using var inputDocument = PdfReader.Open(file.FilePath, PdfDocumentOpenMode.Import);

                foreach (var page in inputDocument.Pages)
                {
                    outputDocument.AddPage(page);
                }
            }

            var outputPath = Path.GetFullPath(Path.Combine(options.OutputDirectory, options.OutputFileName));
            outputDocument.Save(outputPath);

            return new OperationResult(true, outputPath, string.Empty);
        }
        catch (Exception ex)
        {
            return new OperationResult(false, string.Empty, ex.Message);
        }
    }
}
