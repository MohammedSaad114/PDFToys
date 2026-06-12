using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PDFToys.Core.Contracts;
using PDFToys.Core.Models;
using System.Text;

namespace PDFToys.Core.Services;

public sealed class MergeService : ServiceBase, IMergeService
{
    static MergeService()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// Merges multiple PDF files into a single output document.
    /// </summary>
    /// <param name="inputs">The PDF files to merge.</param>
    /// <param name="options">Configuration for the output destination.</param>
    /// <returns>An OperationResult indicating success or failure.</returns>
    public OperationResult Merge(IReadOnlyList<PdfFile> inputs, MergeOptions options)
    {
        return ExecuteSafe(() =>
        {
            var optionsError = ValidateOptionsNotNull(options);
            if (optionsError != null)
            {
                return optionsError;
            }

            var inputValidation = ValidateInputFiles(inputs);
            if (inputValidation != null)
            {
                return inputValidation;
            }

            var outputDirValidation = ValidateOutputDirectory(options.OutputDirectory);
            if (outputDirValidation != null)
            {
                return outputDirValidation;
            }

            if (string.IsNullOrWhiteSpace(options.OutputFileName))
            {
                return new OperationResult(false, string.Empty, "Output file name is required.");
            }

            Directory.CreateDirectory(options.OutputDirectory);

            using var outputDocument = new PdfDocument();

            foreach (var file in inputs!)
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
        });
    }
}
