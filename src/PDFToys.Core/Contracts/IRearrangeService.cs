using PDFToys.Core.Models;

namespace PDFToys.Core.Contracts;

public interface IRearrangeService
{
    OperationResult Rearrange(PdfFile input, IReadOnlyList<PageArrangementItem> pages, RearrangeOptions options);
}
