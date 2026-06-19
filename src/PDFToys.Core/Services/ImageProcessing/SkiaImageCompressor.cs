using PDFToys.Core.Contracts;
using PDFToys.Core.Models;
using SkiaSharp;

namespace PDFToys.Core.Services.ImageProcessing;

public sealed class SkiaImageCompressor : IImageCompressor
{
    public CompressedImageResult? Compress(byte[] imageBytes, int quality)
    {
        try
        {
            using var bitmap = SKBitmap.Decode(imageBytes);
            if (bitmap is null)
            {
                return null;
            }

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
            if (data is null)
            {
                return null;
            }

            return new CompressedImageResult(data.ToArray(), bitmap.Width, bitmap.Height);
        }
        catch
        {
            return null;
        }
    }
}
