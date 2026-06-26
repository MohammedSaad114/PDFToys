namespace PDFToys.Core.Models;

public enum PdfExportFormat
{
    Jpg,
    Jpeg,
    Png,
    Markdown
}

public sealed record ExportOptions(string OutputDirectory, PdfExportFormat Format);
