using PDFToys.App.Models;

namespace PDFToys.App.Services;

public static class ConvertFromPdfTargetResolver
{
    public static bool TryResolve(string? target, out PdfToysOperation operation)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            operation = default;
            return false;
        }

        switch (target.Trim().ToLowerInvariant())
        {
            case "jpg":
                operation = PdfToysOperation.PdfToJpg;
                return true;
            case "jpeg":
                operation = PdfToysOperation.PdfToJpeg;
                return true;
            case "png":
                operation = PdfToysOperation.PdfToPng;
                return true;
            case "markdown":
            case "md":
                operation = PdfToysOperation.PdfToMarkdown;
                return true;
            default:
                operation = default;
                return false;
        }
    }
}
