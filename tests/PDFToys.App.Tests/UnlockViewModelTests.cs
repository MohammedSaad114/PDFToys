using PDFToys.App.Services;
using PDFToys.App.ViewModels;
using PDFToys.Core.Contracts;
using PDFToys.Core.Models;

namespace PDFToys.App.Tests;

public sealed class UnlockViewModelTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        $"pdftoys-unlock-view-model-{Guid.NewGuid():N}");

    public UnlockViewModelTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task ExecuteUnlockAsync_WithoutFiles_ShowsValidationMessage()
    {
        var service = new ProtectServiceStub(CreateSuccessfulResult);
        var viewModel = CreateViewModel(service);
        viewModel.Password = "secret";

        await viewModel.ExecuteUnlockAsync();

        Assert.Equal("Please select PDF files first.", viewModel.StatusMessage);
        Assert.Empty(service.UnlockCalls);
    }

    [Fact]
    public async Task ExecuteUnlockAsync_WithoutPassword_ShowsValidationMessage()
    {
        var inputPath = CreateInput("password-required.pdf");
        var service = new ProtectServiceStub(CreateSuccessfulResult);
        var viewModel = CreateViewModel(service, [inputPath]);

        await viewModel.ExecuteUnlockAsync();

        Assert.Equal("Password is required.", viewModel.StatusMessage);
        Assert.Empty(service.UnlockCalls);
    }

    [Fact]
    public void Constructor_IgnoresDuplicateInitialFiles()
    {
        var inputPath = CreateInput("duplicate.pdf");
        var service = new ProtectServiceStub(CreateSuccessfulResult);

        var viewModel = CreateViewModel(service, [inputPath, inputPath, ""]);

        var selectedFile = Assert.Single(viewModel.SelectedFiles);
        Assert.Equal(inputPath, selectedFile.FullPath);
        Assert.IsType<UnlockFileItem>(selectedFile);
    }

    [Fact]
    public async Task ExecuteUnlockAsync_WithValidInput_ProducesCopyAndClearsPassword()
    {
        var inputPath = CreateInput("protected.pdf");
        var service = new ProtectServiceStub(CreateSuccessfulResult);
        var viewModel = CreateViewModel(service, [inputPath]);
        viewModel.Password = "secret";

        await viewModel.ExecuteUnlockAsync();

        var call = Assert.Single(service.UnlockCalls);
        Assert.Equal(inputPath, call.Input.FilePath);
        Assert.Equal("secret", call.Options.Password);
        Assert.Equal(Path.GetDirectoryName(inputPath), call.Options.OutputDirectory);
        Assert.StartsWith("Unlock complete:", viewModel.StatusMessage);
        Assert.Equal(string.Empty, viewModel.Password);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task ExecuteUnlockAsync_WithPartialFailure_ReportsFailureAndKeepsPassword()
    {
        var successfulInput = CreateInput("successful.pdf");
        var failedInput = CreateInput("failed.pdf");
        var service = new ProtectServiceStub((input, options) =>
            input.FilePath == failedInput
                ? new OperationResult(false, string.Empty, "Incorrect password.")
                : CreateSuccessfulResult(input, options));
        var viewModel = CreateViewModel(service, [successfulInput, failedInput]);
        viewModel.Password = "secret";

        await viewModel.ExecuteUnlockAsync();

        Assert.Equal(2, service.UnlockCalls.Count);
        Assert.Contains("Unlocked 1/2 files successfully", viewModel.StatusMessage);
        Assert.Contains(failedInput, viewModel.StatusMessage);
        Assert.Contains("Incorrect password.", viewModel.StatusMessage);
        Assert.Equal("secret", viewModel.Password);
    }

    [Fact]
    public async Task ExecuteUnlockAsync_WhenOutputIsMissing_ReportsFailure()
    {
        var inputPath = CreateInput("missing-output.pdf");
        var missingOutputPath = Path.Combine(_tempDirectory, "does-not-exist.pdf");
        var service = new ProtectServiceStub((_, _) =>
            new OperationResult(true, missingOutputPath, string.Empty));
        var viewModel = CreateViewModel(service, [inputPath]);
        viewModel.Password = "secret";

        await viewModel.ExecuteUnlockAsync();

        Assert.Contains("without producing an output file", viewModel.StatusMessage);
        Assert.Equal("secret", viewModel.Password);
    }

    [Fact]
    public async Task ExecuteUnlockAsync_WhileRunning_DisablesInteractiveCommands()
    {
        var inputPath = CreateInput("busy.pdf");
        using var started = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var service = new ProtectServiceStub((input, options) =>
        {
            started.Set();
            release.Wait(TimeSpan.FromSeconds(5));
            return CreateSuccessfulResult(input, options);
        });
        var viewModel = CreateViewModel(service, [inputPath]);
        viewModel.Password = "secret";

        var execution = viewModel.ExecuteUnlockAsync();
        Assert.True(started.Wait(TimeSpan.FromSeconds(5)));

        try
        {
            Assert.True(viewModel.IsBusy);
            Assert.False(viewModel.AddFilesCommand.CanExecute(null));
            Assert.False(viewModel.RemoveFileCommand.CanExecute(viewModel.SelectedFiles[0]));
            Assert.False(viewModel.ExecuteUnlockCommand.CanExecute(null));
            Assert.False(viewModel.GoBackCommand.CanExecute(null));
        }
        finally
        {
            release.Set();
        }

        await execution;
        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.ExecuteUnlockCommand.CanExecute(null));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private UnlockViewModel CreateViewModel(
        ProtectServiceStub service,
        IEnumerable<string>? initialFiles = null)
    {
        return new UnlockViewModel(
            service,
            new FileDialogStub(),
            () => { },
            initialFiles);
    }

    private string CreateInput(string fileName)
    {
        var path = Path.Combine(_tempDirectory, fileName);
        File.WriteAllBytes(path, [0x25, 0x50, 0x44, 0x46]);
        return path;
    }

    private static OperationResult CreateSuccessfulResult(
        PdfFile input,
        UnlockOptions options)
    {
        var outputPath = Path.Combine(
            options.OutputDirectory,
            $"{Path.GetFileNameWithoutExtension(input.FilePath)}_Unlocked.pdf");
        File.Copy(input.FilePath, outputPath, overwrite: true);
        return new OperationResult(true, outputPath, string.Empty);
    }

    private sealed class ProtectServiceStub(
        Func<PdfFile, UnlockOptions, OperationResult> unlockHandler) : IProtectService
    {
        public List<(PdfFile Input, UnlockOptions Options)> UnlockCalls { get; } = [];

        public OperationResult Protect(PdfFile input, ProtectOptions options)
        {
            throw new NotSupportedException();
        }

        public OperationResult Unlock(PdfFile input, UnlockOptions options)
        {
            UnlockCalls.Add((input, options));
            return unlockHandler(input, options);
        }
    }

    private sealed class FileDialogStub : IFileDialogService
    {
        public Task<IReadOnlyList<string>> PickPdfFilesAsync()
        {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        public Task<IReadOnlyList<string>> PickConvertibleFilesAsync(bool allowMultiple = true)
        {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        public Task<string?> ShowSaveFileDialogAsync(
            string title,
            string defaultFileName,
            string extension)
        {
            return Task.FromResult<string?>(null);
        }
    }
}
