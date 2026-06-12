using PDFToys.Core.Models;

namespace PDFToys.Core.Contracts;

public interface IExportStrategy
{
    bool CanHandle(PdfExportFormat format);

    OperationResult Execute(IReadOnlyList<PdfFile> inputs, ExportOptions options, string outputFolder);
}