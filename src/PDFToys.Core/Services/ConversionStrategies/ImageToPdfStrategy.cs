using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PDFToys.Core.Models;

namespace PDFToys.Core.Services.ConversionStrategies;

public sealed class ImageToPdfStrategy : BaseConversionStrategy
{
    public override bool CanHandle(string inputExtension) =>
        inputExtension.ToLowerInvariant() is ".png" or ".jpg" or ".jpeg";

    public override OperationResult Execute(string inputFilePath, ConversionOptions options, string outputFilePath)
    {
        using var document = new PdfDocument();

        ApplyStandardMetadata(document);

        using var image = XImage.FromFile(inputFilePath);

        var widthPt = image.PointWidth <= 0 ? A4WidthPoints : image.PointWidth;
        var heightPt = image.PointHeight <= 0 ? A4HeightPoints : image.PointHeight;

        var page = document.AddPage();
        page.Width = widthPt;
        page.Height = heightPt;

        using var gfx = XGraphics.FromPdfPage(page);
        gfx.DrawImage(image, 0, 0, widthPt, heightPt);

        document.Save(outputFilePath);

        return new OperationResult(true, Path.GetFullPath(outputFilePath), string.Empty);
    }
}