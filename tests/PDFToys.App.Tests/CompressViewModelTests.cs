using PDFToys.App.Services;
using PDFToys.App.ViewModels;
using PDFToys.Core.Contracts;
using PDFToys.Core.Models;

namespace PDFToys.App.Tests;

public sealed class CompressViewModelTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(
        Path.GetTempPath(),
        $"pdftoys-compress-view-model-{Guid.NewGuid():N}");

    public CompressViewModelTests()
    {
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task ExecuteCompressAsync_WithoutFiles_ShowsValidationMessage()
    {
        var service = new CompressionServiceStub(CreateSuccessfulResult);
        var viewModel = CreateViewModel(service);

        await viewModel.ExecuteCompressAsync();

        Assert.Equal("Please select PDF files first.", viewModel.StatusMessage);
        Assert.Empty(service.Calls);
    }

    [Fact]
    public void Constructor_IgnoresDuplicateInitialFiles()
    {
        var inputPath = CreateInput("duplicate.pdf");
        var service = new CompressionServiceStub(CreateSuccessfulResult);

        var viewModel = CreateViewModel(service, [inputPath, inputPath, ""]);

        var selectedFile = Assert.Single(viewModel.SelectedFiles);
        Assert.Equal(inputPath, selectedFile.FullPath);
        Assert.IsType<CompressFileItem>(selectedFile);
    }

    [Theory]
    [InlineData(CompressionLevel.Normal)]
    [InlineData(CompressionLevel.Maximum)]
    public async Task ExecuteCompressAsync_UsesSelectedCompressionLevel(
        CompressionLevel level)
    {
        var inputPath = CreateInput($"{level}.pdf");
        var service = new CompressionServiceStub(CreateSuccessfulResult);
        var viewModel = CreateViewModel(service, [inputPath]);
        viewModel.SelectedCompressionLevel = Assert.Single(
            viewModel.CompressionLevels,
            item => item.Level == level);

        await viewModel.ExecuteCompressAsync();

        var call = Assert.Single(service.Calls);
        Assert.Equal(level, call.Options.Level);
        Assert.Equal(Path.GetDirectoryName(inputPath), call.Options.OutputDirectory);
        Assert.StartsWith("Compression complete:", viewModel.StatusMessage);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task ExecuteCompressAsync_WithPartialFailure_ReportsFailedFile()
    {
        var successfulInput = CreateInput("successful.pdf");
        var failedInput = CreateInput("failed.pdf");
        var service = new CompressionServiceStub((input, options) =>
            input.FilePath == failedInput
                ? new OperationResult(false, string.Empty, "Test failure.")
                : CreateSuccessfulResult(input, options));
        var viewModel = CreateViewModel(service, [successfulInput, failedInput]);

        await viewModel.ExecuteCompressAsync();

        Assert.Equal(2, service.Calls.Count);
        Assert.Contains("Compressed 1/2 files successfully", viewModel.StatusMessage);
        Assert.Contains(failedInput, viewModel.StatusMessage);
        Assert.Contains("Test failure.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task ExecuteCompressAsync_WhileRunning_DisablesInteractiveCommands()
    {
        var inputPath = CreateInput("busy.pdf");
        using var started = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var service = new CompressionServiceStub((input, options) =>
        {
            started.Set();
            release.Wait(TimeSpan.FromSeconds(5));
            return CreateSuccessfulResult(input, options);
        });
        var viewModel = CreateViewModel(service, [inputPath]);

        var execution = viewModel.ExecuteCompressAsync();
        Assert.True(started.Wait(TimeSpan.FromSeconds(5)));

        try
        {
            Assert.True(viewModel.IsBusy);
            Assert.False(viewModel.AddFilesCommand.CanExecute(null));
            Assert.False(viewModel.RemoveFileCommand.CanExecute(viewModel.SelectedFiles[0]));
            Assert.False(viewModel.ExecuteCompressCommand.CanExecute(null));
            Assert.False(viewModel.GoBackCommand.CanExecute(null));
        }
        finally
        {
            release.Set();
        }

        await execution;
        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.ExecuteCompressCommand.CanExecute(null));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private CompressViewModel CreateViewModel(
        CompressionServiceStub service,
        IEnumerable<string>? initialFiles = null)
    {
        return new CompressViewModel(
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
        CompressionOptions options)
    {
        var outputPath = Path.Combine(
            options.OutputDirectory,
            $"{Path.GetFileNameWithoutExtension(input.FilePath)}_Compressed.pdf");
        File.Copy(input.FilePath, outputPath, overwrite: true);
        return new OperationResult(true, outputPath, string.Empty);
    }

    private sealed class CompressionServiceStub(
        Func<PdfFile, CompressionOptions, OperationResult> handler) : ICompressionService
    {
        public List<(PdfFile Input, CompressionOptions Options)> Calls { get; } = [];

        public OperationResult Compress(PdfFile input, CompressionOptions options)
        {
            Calls.Add((input, options));
            return handler(input, options);
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
