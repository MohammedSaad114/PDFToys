namespace PDFToys.Core.Models;

public enum CompressionLevel
{
    Normal,
    Maximum
}

public sealed record CompressionOptions(string OutputDirectory, CompressionLevel Level);
