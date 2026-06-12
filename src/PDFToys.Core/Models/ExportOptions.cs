namespace PDFToys.Core.Models;

public enum PdfExportFormat
{
    Jpg,
    Png,
    Markdown
}

public sealed record ExportOptions(string OutputDirectory, PdfExportFormat Format);
