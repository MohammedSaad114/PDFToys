using System.Collections.Generic;
using System.Threading.Tasks;

namespace PDFToys.App.Services;

public interface IFileDialogService
{
    Task<IReadOnlyList<string>> PickPdfFilesAsync();

    Task<IReadOnlyList<string>> PickConvertibleFilesAsync(bool allowMultiple = true);

    Task<string?> ShowSaveFileDialogAsync(string title, string defaultFileName, string extension);
}
