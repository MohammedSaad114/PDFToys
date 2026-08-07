using PDFToys.App.Models;

namespace PDFToys.App.ViewModels;

public sealed class StartupOperationViewModel : ViewModelBase
{
    public StartupOperationViewModel(PdfToysOperation operation)
    {
        OperationName = operation.ToString();
    }

    public string OperationName { get; }

    public string Title => "Operation Coming Soon";

    public string Message => $"The startup route for '{OperationName}' is recognized, but the UI workflow is not implemented yet.";
}
