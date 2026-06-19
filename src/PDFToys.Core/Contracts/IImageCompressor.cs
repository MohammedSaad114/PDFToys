using PDFToys.Core.Models;

namespace PDFToys.Core.Contracts;

public interface IImageCompressor
{
    /// <summary>
    /// Re-encodes raw image bytes as JPEG. Returns null when decode or encode fails.
    /// </summary>
    CompressedImageResult? Compress(byte[] imageBytes, int quality);
}
