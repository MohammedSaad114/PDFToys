namespace PDFToys.App.Models;

public sealed class UserSettings
{
    public string? LastWorkspace { get; set; }

    public string DefaultPdfOutputMode { get; set; } = nameof(PdfOutputMode.CreateNewCopy);

    public string DefaultCompressionLevel { get; set; } = "Standard";
}
