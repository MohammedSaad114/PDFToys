using PDFToys.Core.Contracts;
using PDFToys.Core.Models;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PDFToys.Core.Services;

// TODO: Implementing a secondary service in the future
// to handle deep image downsampling, as PdfSharp cannot do this.
public sealed class CompressionService : ICompressionService
{
    /// <summary>
    /// Applies Flate encoding to the document's content streams to reduce file size. 
    /// </summary>
    /// <param name="input">The source PDF file to compress.</param>
    /// <param name="options">The compression options, including output directory and Flate encoding quality.</param>
    /// <returns>An OperationResult containing the path to the structurally compressed file.</returns>
    public OperationResult Compress(PdfFile input, CompressionOptions options)
    {

        try
        {
            if (!File.Exists(input.FilePath))
            {
                return new OperationResult(false, string.Empty, $"Input file not found: {input.FilePath}");
            }

            if (string.IsNullOrWhiteSpace(options.OutputDirectory))
            {
                return new OperationResult(false, string.Empty, "Output directory is required.");
            }

            Directory.CreateDirectory(options.OutputDirectory);

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

            var originalFileName = Path.GetFileNameWithoutExtension(input.FilePath);
            var outputPath = Path.Combine(options.OutputDirectory, $"{originalFileName}_Compressed.pdf");
            outputDocument.Save(outputPath);

            return new OperationResult(true, Path.GetFullPath(outputPath), string.Empty);
        }
        catch (Exception ex)
        {
            return new OperationResult(false, string.Empty, ex.Message);
        }
    }
}