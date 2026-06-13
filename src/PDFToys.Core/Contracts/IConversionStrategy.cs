using PDFToys.Core.Models;

namespace PDFToys.Core.Contracts;

public interface IConversionStrategy
{
    bool CanHandle(string inputExtension);

    OperationResult Execute(string inputFilePath, ConversionOptions options, string outputFilePath);
}