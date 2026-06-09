using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PDFToys.Core.Contracts;
using PDFToys.Core.Models;

namespace PDFToys.Core.Services;

public sealed class ProtectService : IProtectService
{
    /// <summary>
    /// Encrypts the specified input PDF with a user password and saves it to the output directory.
    /// </summary>
    /// <param name="input">The source PDF file to protect.</param>
    /// <param name="options">The protection options containing the password and output destination.</param>
    /// <returns>An OperationResult indicating success or failure, containing the path to the protected file if successful.</returns>
    public OperationResult Protect(PdfFile input, ProtectOptions options)
    {
        try
        {
            if (!File.Exists(input.FilePath))
            {
                return new OperationResult(false, string.Empty, $"Input file not found: {input.FilePath}");
            }

            if (string.IsNullOrWhiteSpace(options.Password))
            {
                return new OperationResult(false, string.Empty, "Password is required.");
            }

            if (string.IsNullOrWhiteSpace(options.OutputDirectory))
            {
                return new OperationResult(false, string.Empty, "Output directory is required.");
            }

            Directory.CreateDirectory(options.OutputDirectory);

            // PdfSharp can apply security settings directly on a document opened for modify.
            using var document = PdfReader.Open(input.FilePath, PdfDocumentOpenMode.Modify);
            document.SecuritySettings.UserPassword = options.Password;

            var originalFileName = Path.GetFileNameWithoutExtension(input.FilePath);
            var outputPath = Path.Combine(options.OutputDirectory, $"{originalFileName}_Protected.pdf");
            document.Save(outputPath);

            return new OperationResult(true, Path.GetFullPath(outputPath), string.Empty);
        }
        catch (Exception ex)
        {
            return new OperationResult(false, string.Empty, ex.Message);
        }
    }

    public OperationResult Unlock(PdfFile input, UnlockOptions options)
    {
        try
        {
            if (!File.Exists(input.FilePath))
            {
                return new OperationResult(false, string.Empty, $"Input file not found: {input.FilePath}");
            }

            if (string.IsNullOrWhiteSpace(options.Password))
            {
                return new OperationResult(false, string.Empty, "Password is required.");
            }

            if (string.IsNullOrWhiteSpace(options.OutputDirectory))
            {
                return new OperationResult(false, string.Empty, "Output directory is required.");
            }

            Directory.CreateDirectory(options.OutputDirectory);

            using var inputDocument = PdfReader.Open(
                input.FilePath,
                options.Password,
                PdfDocumentOpenMode.Import);
            using var outputDocument = new PdfDocument();

            foreach (var page in inputDocument.Pages)
            {
                outputDocument.AddPage(page);
            }

            var originalFileName = Path.GetFileNameWithoutExtension(input.FilePath);
            var outputPath = Path.Combine(options.OutputDirectory, $"{originalFileName}_Unlocked.pdf");

            outputDocument.Save(outputPath);

            return new OperationResult(true, Path.GetFullPath(outputPath), string.Empty);
        }
        catch (Exception ex)
        {
            return new OperationResult(false, string.Empty, ex.Message);
        }
    }
}