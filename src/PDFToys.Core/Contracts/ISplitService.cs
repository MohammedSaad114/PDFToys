using PDFToys.Core.Models;

namespace PDFToys.Core.Contracts;

public interface ISplitService
{
    OperationResult Split(PdfFile input, SplitOptions options);
}