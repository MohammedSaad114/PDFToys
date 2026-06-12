using PDFToys.Core.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace PDFToys.Core.Services.ExportStrategies;

public sealed class MarkdownExportStrategy : ExportStrategyBase
{
    public override bool CanHandle(PdfExportFormat format) =>
        format is PdfExportFormat.Markdown;

    public override OperationResult Execute(IReadOnlyList<PdfFile> inputs, ExportOptions options, string outputFolder)
    {
        var useSourcePrefix = inputs.Count > 1;
        var exportedPages = 0;

        // Pure business logic! No try/catches or validation needed here.
        foreach (var input in inputs)
        {
            // Inherited from ExportStrategyBase!
            var sourcePrefix = SanitizeSourceName(input.FilePath);

            using var document = PdfDocument.Open(input.FilePath);
            var pageIndex = 1;

            foreach (var page in document.GetPages())
            {
                var markdown = ContentOrderTextExtractor.GetText(page, addDoubleNewline: true);
                var fileName = useSourcePrefix
                    ? $"{sourcePrefix}_page_{pageIndex:000}.md"
                    : $"page_{pageIndex:000}.md";

                File.WriteAllText(Path.Combine(outputFolder, fileName), markdown);

                pageIndex++;
                exportedPages++;
            }
        }

        return exportedPages == 0
            ? new OperationResult(false, string.Empty, "No pages were extracted from the PDF.")
            : new OperationResult(true, outputFolder, string.Empty);
    }
}