using PDFToys.Core.Contracts;
using PDFToys.Core.Models;

namespace PDFToys.Core.Services.ConversionStrategies;

public abstract class BaseConversionStrategy : IConversionStrategy
{
    protected const double A4WidthPoints = 595;
    protected const double A4HeightPoints = 842;

    public abstract bool CanHandle(string inputExtension);
    public abstract OperationResult Execute(string inputFilePath, ConversionOptions options, string outputFilePath);


}