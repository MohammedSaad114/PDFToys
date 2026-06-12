using PDFToys.Core.Contracts;
using PDFToys.Core.Models;

namespace PDFToys.Core.Services.ExportStrategies;

public abstract class ExportStrategyBase : IExportStrategy
{
    public abstract bool CanHandle(PdfExportFormat format);
    public abstract OperationResult Execute(IReadOnlyList<PdfFile> inputs, ExportOptions options, string outputFolder);

    protected static string SanitizeSourceName(string inputPath)
    {
        var sourceName = Path.GetFileNameWithoutExtension(inputPath);
        if (string.IsNullOrWhiteSpace(sourceName))
        {
            return "document";
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new char[sourceName.Length];
        for (var i = 0; i < sourceName.Length; i++)
        {
            var ch = sourceName[i];
            sanitized[i] = invalidChars.Contains(ch) ? '_' : ch;
        }

        return new string(sanitized);
    }
}