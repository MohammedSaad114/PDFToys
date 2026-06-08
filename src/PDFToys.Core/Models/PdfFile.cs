namespace PDFToys.Core.Models;

public sealed record PdfFile(string FilePath)
{
    public PdfFile() : this(string.Empty) { }
}