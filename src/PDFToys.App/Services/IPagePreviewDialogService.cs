using System.Threading.Tasks;
using PDFToys.App.Models;

namespace PDFToys.App.Services;

public interface IPagePreviewDialogService
{
    Task ShowAsync(PagePreviewRequest request);
}
