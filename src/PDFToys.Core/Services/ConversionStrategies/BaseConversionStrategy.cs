using PdfSharp.Pdf;
using PDFToys.Core.Contracts;
using PDFToys.Core.Models;

namespace PDFToys.Core.Services.ConversionStrategies;

/// <summary>
/// Base class for conversion strategies to share common pdf generation helpers.
/// </summary>
public abstract class BaseConversionStrategy : IConversionStrategy
{
    // Standardized A4 Dimensions 
    protected const double A4WidthPoints = 595;
    protected const double A4HeightPoints = 842;

    public abstract bool CanHandle(string inputExtension);
    public abstract OperationResult Execute(string inputFilePath, ConversionOptions options, string outputFilePath);


    protected static void ApplyStandardMetadata(PdfDocument document)
    {
        if (document == null) return;

        document.Info.Author = "PDFToys";
        document.Info.Creator = "PDFToys";
        document.Info.CreationDate = DateTime.Now;
    }

    protected static void CleanupTempFile(string tempFilePath)
    {
        if (!string.IsNullOrWhiteSpace(tempFilePath) && File.Exists(tempFilePath))
        {
            try { File.Delete(tempFilePath); } catch { /* Ignore cleanup errors */ }
        }
    }
}