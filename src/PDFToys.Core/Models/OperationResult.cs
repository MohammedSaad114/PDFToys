namespace PDFToys.Core.Models;

public sealed record OperationResult(bool IsSuccess, string OutputPath, string ErrorMessage);