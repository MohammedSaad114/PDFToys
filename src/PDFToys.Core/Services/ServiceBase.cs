using PDFToys.Core.Models;

namespace PDFToys.Core.Services;

public abstract class ServiceBase
{
    /// <summary>
    /// Wraps PDF operations in a standard try/catch block to guarantee an OperationResult is always returned.
    /// </summary>
    protected static OperationResult ExecuteSafe(Func<OperationResult> operation)
    {
        try
        {
            return operation();
        }
        catch (Exception ex)
        {
            return new OperationResult(false, string.Empty, ex.Message);
        }
    }

    /// <summary>
    /// Performs standard validation for the input file and output directory.
    /// </summary>
    protected static OperationResult? ValidateStandardInputs(PdfFile input, string outputDirectory)
    {
        if (input is null || !File.Exists(input.FilePath))
        {
            return new OperationResult(false, string.Empty, $"Input file not found: {input?.FilePath}");
        }

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            return new OperationResult(false, string.Empty, "Output directory is required.");
        }

        return null; // Null indicates validation passed
    }

    /// <summary>
    /// Creates the output directory and generates a standardized output file path.
    /// </summary>
    protected static string PrepareOutputEnvironment(string inputPath, string outputDirectory, string suffix)
    {
        Directory.CreateDirectory(outputDirectory);
        var originalFileName = Path.GetFileNameWithoutExtension(inputPath);
        return Path.Combine(outputDirectory, $"{originalFileName}_{suffix}.pdf");
    }
}