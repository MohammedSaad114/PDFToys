using PDFToys.Core.Models;

namespace PDFToys.Core.Contracts;

public interface IRearrangeService
{
    int? TryGetPageCount(PdfFile input);

    OperationResult Rearrange(PdfFile input, IReadOnlyList<PageArrangementItem> pages, RearrangeOptions options);
}
