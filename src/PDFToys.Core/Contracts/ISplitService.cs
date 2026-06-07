using PDFToys.Core.Models;

namespace PDFToys.Core.Contracts;

public interface ISplitPdfService
{
    OperationResult Split(PdfFile input, SplitOptions options);
}