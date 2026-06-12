using PDFToys.Core.Models;

namespace PDFToys.Core.Contracts;

public interface IMergeService
{
    OperationResult Merge(IReadOnlyList<PdfFile> inputs, MergeOptions options);
}
