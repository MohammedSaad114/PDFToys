namespace PDFToys.Core.Models;

public enum CompressionLevel
{
    Fast,
    Normal,
    Maximum
}

public sealed record CompressionOptions(string OutputDirectory, CompressionLevel Quality);