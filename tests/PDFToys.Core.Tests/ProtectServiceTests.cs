using PDFToys.Core.Models;
using PDFToys.Core.Services;

namespace PDFToys.Core.Tests;

public sealed class ProtectServiceTests : PdfTestBase
{
    private readonly ProtectService _service;

    public ProtectServiceTests()
    {
        _service = new ProtectService();
    }

    [Fact]
    public void Unlock_WithCorrectPassword_CreatesUnprotectedCopy()
    {
        var inputPath = Path.Combine(TempDirectory, "protected.pdf");
        CreatePdf(inputPath, 10);

        var protectResult = _service.Protect(
            new PdfFile(inputPath),
            new ProtectOptions("secret123", TempDirectory));
        Assert.True(protectResult.IsSuccess, protectResult.ErrorMessage);

        var unlockResult = _service.Unlock(
            new PdfFile(protectResult.OutputPath!),
            new UnlockOptions("secret123", TempDirectory));

        Assert.True(unlockResult.IsSuccess, unlockResult.ErrorMessage);
        Assert.True(File.Exists(unlockResult.OutputPath));
    }

    [Fact]
    public void Unlock_WithWrongPassword_ReturnsFailure()
    {
        var inputPath = Path.Combine(TempDirectory, "wrong-password.pdf");
        CreatePdf(inputPath, 10);

        var protectResult = _service.Protect(
            new PdfFile(inputPath),
            new ProtectOptions("secret123", TempDirectory));
        Assert.True(protectResult.IsSuccess, protectResult.ErrorMessage);

        var unlockResult = _service.Unlock(
            new PdfFile(protectResult.OutputPath!),
            new UnlockOptions("bad-password", TempDirectory));

        Assert.False(unlockResult.IsSuccess);
        Assert.NotEmpty(unlockResult.ErrorMessage);
    }

    [Fact]
    public void Unlock_WithoutPassword_ReturnsFailure()
    {
        var inputPath = Path.Combine(TempDirectory, "missing-password.pdf");
        CreatePdf(inputPath, 10);

        var unlockResult = _service.Unlock(
            new PdfFile(inputPath),
            new UnlockOptions(string.Empty, TempDirectory));

        Assert.False(unlockResult.IsSuccess);
        Assert.Contains("Password is required", unlockResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Protect_WithValidPassword_CreatesProtectedCopy()
    {
        var inputPath = Path.Combine(TempDirectory, "unprotected.pdf");
        CreatePdf(inputPath, 5);
        var options = new ProtectOptions("mysecret", TempDirectory);

        var result = _service.Protect(new PdfFile(inputPath), options);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.True(File.Exists(result.OutputPath));
        Assert.EndsWith("_Protected.pdf", result.OutputPath);

        // Ensures the original file was not overwritten
        Assert.NotEqual(inputPath, result.OutputPath);
    }

}