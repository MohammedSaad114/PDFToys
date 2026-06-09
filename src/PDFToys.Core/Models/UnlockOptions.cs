namespace PDFToys.Core.Models;

public sealed record UnlockOptions(
    string Password,
    string OutputDirectory
);
