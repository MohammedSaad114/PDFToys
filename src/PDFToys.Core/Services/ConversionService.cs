using PDFToys.Core.Contracts;
using PDFToys.Core.Models;

namespace PDFToys.Core.Services;

public sealed class ConversionService : ServiceBase, IConversionService
{
    private readonly IEnumerable<IConversionStrategy> _strategies;

    public ConversionService(IEnumerable<IConversionStrategy> strategies)
    {
        _strategies = strategies;
    }

    public OperationResult Convert(string inputFilePath, ConversionOptions options)
    {
        return ExecuteSafe(() =>
        {
            var optionsError = ValidateOptionsNotNull(options);
            if (optionsError != null) return optionsError;

            var validationError = ValidateForeignInput(inputFilePath, options.OutputDirectory);
            if (validationError != null) return validationError;

            var outputFilePath = PrepareOutputEnvironment(inputFilePath, options.OutputDirectory, "Converted");

            var extension = Path.GetExtension(inputFilePath).ToLowerInvariant();
            var strategy = _strategies.FirstOrDefault(s => s.CanHandle(extension));

            if (strategy == null)
            {
                return new OperationResult(false, string.Empty, $"Unsupported file type: {extension}");
            }

            // 4. Execute the worker!
            return strategy.Execute(inputFilePath, options, outputFilePath);
        });
    }
}