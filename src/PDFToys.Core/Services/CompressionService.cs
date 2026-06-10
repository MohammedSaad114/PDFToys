using PDFToys.Core.Contracts;
using PDFToys.Core.Models;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PDFToys.Core.Services;

// TODO: Implementing a secondary service in the future
// to handle deep image downsampling, as PdfSharp cannot do this.
public sealed class CompressionService : ServiceBase, ICompressionService
{
    /// <summary>
    /// Applies Flate encoding to the document's content streams to reduce file size. 
    /// </summary>
    /// <param name="input">The source PDF file to compress.</param>
    /// <param name="options">The compression options, including output directory and Flate encoding quality.</param>
    /// <returns>An OperationResult containing the path to the structurally compressed file.</returns>
    public OperationResult Compress(PdfFile input, CompressionOptions options)
    {
        return ExecuteSafe(() =>
        {
            // Validation
            var validationError = ValidateStandardInputs(input, options.OutputDirectory);
            if (validationError != null)
            {
                return validationError;
            }

            var outputPath = PrepareOutputEnvironment(input.FilePath, options.OutputDirectory, "Compressed");

            using var inputDocument = PdfReader.Open(input.FilePath, PdfDocumentOpenMode.Import);
            using var outputDocument = new PdfDocument();

            outputDocument.Options.CompressContentStreams = true;
            outputDocument.Options.NoCompression = false;
            outputDocument.Options.FlateEncodeMode = options.Quality switch
            {
                CompressionLevel.Maximum => PdfFlateEncodeMode.BestCompression,
                _ => PdfFlateEncodeMode.Default
            };

            foreach (var page in inputDocument.Pages)
            {
                outputDocument.AddPage(page);
            }

            outputDocument.Save(outputPath);

            return new OperationResult(true, Path.GetFullPath(outputPath), string.Empty);
        });
    }
}