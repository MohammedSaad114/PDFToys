using PDFToys.Core.Models;

namespace PDFToys.Core.Contracts;

public interface IRearrangeService
{
    OperationResult Rearrange(PdfFile input, int[] newPageOrder, RearrangeOptions options);
}
