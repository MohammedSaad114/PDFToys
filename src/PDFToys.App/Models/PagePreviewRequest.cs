namespace PDFToys.App.Models;

public sealed record PagePreviewRequest(
    string Title,
    string DetailsText,
    byte[]? ImagePngBytes,
    string ErrorMessage);
