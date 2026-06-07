namespace PDFToys.Core.Contracts;

[Obsolete("Use IMergeService for merge operations.")]
public interface IProcessor
{
    void MergePdfs(string[] inputPaths, string outputPath);
}