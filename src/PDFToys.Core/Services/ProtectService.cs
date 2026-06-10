using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PDFToys.Core.Contracts;
using PDFToys.Core.Models;

namespace PDFToys.Core.Services;

public sealed class ProtectService : ServiceBase, IProtectService
{
    /// <summary>
    /// Encrypts the specified input PDF with a user password and saves it to the output directory.
    /// </summary>
    /// <param name="input">The source PDF file to protect.</param>
    /// <param name="options">The protection options containing the password and output destination.</param>
    /// <returns>An OperationResult indicating success or failure, containing the path to the protected file if successful.</returns>
    public OperationResult Protect(PdfFile input, ProtectOptions options)
    {
        return ExecuteSafe(() =>
        {
            // Validation
            var validationError = ValidateStandardInputs(input, options.OutputDirectory);
            if (validationError != null)
            {
                return validationError;
            }

            if (string.IsNullOrWhiteSpace(options.Password))
            {
                return new OperationResult(false, string.Empty, "Password is required.");
            }

            var outputPath = PrepareOutputEnvironment(input.FilePath, options.OutputDirectory, "Protected");

            using var document = PdfReader.Open(input.FilePath, PdfDocumentOpenMode.Modify);
            document.SecuritySettings.UserPassword = options.Password;
            document.Save(outputPath);

            return new OperationResult(true, Path.GetFullPath(outputPath), string.Empty);
        });
    }

    public OperationResult Unlock(PdfFile input, UnlockOptions options)
    {
        return ExecuteSafe(() =>
        {
            // Validation
            var validationError = ValidateStandardInputs(input, options.OutputDirectory);
            if (validationError != null)
            {
                return validationError;
            }

            if (string.IsNullOrWhiteSpace(options.Password))
            {
                return new OperationResult(false, string.Empty, "Password is required.");
            }

            var outputPath = PrepareOutputEnvironment(input.FilePath, options.OutputDirectory, "Unlocked");
            
            using var inputDocument = PdfReader.Open(input.FilePath, options.Password, PdfDocumentOpenMode.Import);
            using var outputDocument = new PdfDocument();

            foreach (var page in inputDocument.Pages)
            {
                outputDocument.AddPage(page);
            }

            outputDocument.Save(outputPath);

            return new OperationResult(true, Path.GetFullPath(outputPath), string.Empty);
        });
    }
}