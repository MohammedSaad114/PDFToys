using PDFToys.Core.Models;

namespace PDFToys.Core.Contracts;

public interface IProtectService
{
    OperationResult Protect(PdfFile input, PasswordOptions options);
}