using PDFToys.Core.Models;

namespace PDFToys.Core.Contracts;

public interface IExportService
{
    OperationResult Export(IReadOnlyList<PdfFile> inputs, ExportOptions options);
}