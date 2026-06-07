using PDFToys.Core.Models;

namespace PDFToys.Core.Contracts;

public interface ICompressionService
{
    OperationResult Compress(PdfFile input, CompressOptions options);
}