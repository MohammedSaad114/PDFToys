using PDFToys.Core.Models;
using SkiaSharp;

namespace PDFToys.Core.Services.ExportStrategies;

public sealed class ImageExportStrategy : ExportStrategyBase
{
    public override bool CanHandle(PdfExportFormat format) =>
        format is PdfExportFormat.Png or PdfExportFormat.Jpg;

    public override OperationResult Execute(IReadOnlyList<PdfFile> inputs, ExportOptions options, string outputFolder)
    {
        var (imageFormat, extension) = options.Format switch
        {
            PdfExportFormat.Png => (SKEncodedImageFormat.Png, "png"),
            PdfExportFormat.Jpg => (SKEncodedImageFormat.Jpeg, "jpg"),
            _ => throw new InvalidOperationException($"Unexpected format: {options.Format}")
        };

        var useSourcePrefix = inputs.Count > 1;
        var exportedPages = 0;

        foreach (var input in inputs)
        {
            var sourcePrefix = SanitizeSourceName(input.FilePath);
            var pdfBytes = File.ReadAllBytes(input.FilePath);
            var pageIndex = 1;

            foreach (var bitmap in PDFtoImage.Conversion.ToImages(pdfBytes))
            {
                var fileName = useSourcePrefix
                    ? $"{sourcePrefix}_page_{pageIndex:000}.{extension}"
                    : $"page_{pageIndex:000}.{extension}";

                using (bitmap)
                using (var fs = File.Create(Path.Combine(outputFolder, fileName)))
                {
                    bitmap.Encode(fs, imageFormat, 100);
                }

                pageIndex++;
                exportedPages++;
            }
        }

        return exportedPages == 0
            ? new OperationResult(false, string.Empty, "No pages were exported from the PDF.")
            : new OperationResult(true, outputFolder, string.Empty);
    }
}
