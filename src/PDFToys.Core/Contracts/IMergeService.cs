using PDFToys.Core.Models;

namespace PDFToys.Core.Contracts;

public interface IMergeService
{
    OperationResult Merge(PdfFile[] inputs, MergeOptions options);
}