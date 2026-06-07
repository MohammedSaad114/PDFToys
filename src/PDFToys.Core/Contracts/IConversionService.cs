using PDFToys.Core.Models;

namespace PDFToys.Core.Contracts;

public interface IConversionService
{
    OperationResult Convert(string inputFilePath, ConversionOptions options);
}