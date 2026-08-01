namespace PDFToys.App.Models;

public sealed record PagePreviewResult(bool IsSuccess, byte[]? ImagePngBytes, string ErrorMessage);
