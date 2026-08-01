using PDFToys.App.Models;

namespace PDFToys.App.Services;

public interface IPdfPagePreviewService
{
    PagePreviewResult RenderPage(string filePath, int pageNumber);
}
