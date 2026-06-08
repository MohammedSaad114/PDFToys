namespace PDFToys.Core.Models;

public sealed record MergeOptions(string OutputDirectory, string OutputFileName)
{
    public MergeOptions() : this(string.Empty, string.Empty) { }
}