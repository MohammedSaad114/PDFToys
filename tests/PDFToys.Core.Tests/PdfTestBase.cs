using PdfSharp.Pdf;

namespace PDFToys.Core.Tests;

public abstract class PdfTestBase : IDisposable
{
    protected readonly string TempDirectory;

    protected PdfTestBase()
    {
        TempDirectory = Path.Combine(Path.GetTempPath(), $"pdftoys-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(TempDirectory);
    }

    protected string CreatePdf(string fileName, int pageCount)
    {
        var fullPath = Path.Combine(TempDirectory, fileName);
        using var document = new PdfDocument();

        for (var i = 0; i < pageCount; i++)
        {
            document.AddPage();
        }

        document.Save(fullPath);
        return fullPath;
    }

    public void Dispose()
    {
        if (!Directory.Exists(TempDirectory))
        {
            return;
        }

        const int maxRetries = 5;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                Directory.Delete(TempDirectory, recursive: true);
                return;
            }
            catch (IOException) when (attempt < maxRetries)
            {
                Thread.Sleep(50 * attempt);
            }
            catch (UnauthorizedAccessException) when (attempt < maxRetries)
            {
                Thread.Sleep(50 * attempt);
            }
        }
    }
}