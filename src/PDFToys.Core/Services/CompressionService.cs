using PDFToys.Core.Contracts;
using PDFToys.Core.Models;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;

namespace PDFToys.Core.Services;

public sealed class CompressionService : ServiceBase, ICompressionService
{
    private const int NormalJpegQuality = 60;
    private const int MaximumJpegQuality = 30;

    private readonly IImageCompressor _imageCompressor;

    public CompressionService(IImageCompressor imageCompressor)
    {
        _imageCompressor = imageCompressor;
    }

    public OperationResult Compress(PdfFile input, CompressionOptions options)
    {
        return ExecuteSafe(() =>
        {
            var optionsError = ValidateOptionsNotNull(options);
            if (optionsError != null)
            {
                return optionsError;
            }

            var validationError = ValidateStandardInputs(input, options!.OutputDirectory);
            if (validationError != null)
            {
                return validationError;
            }

            var outputPath = PrepareOutputEnvironment(input.FilePath, options.OutputDirectory, "Compressed");

            // Modify is required here to replace embedded image streams in place
            using var document = PdfReader.Open(input.FilePath, PdfDocumentOpenMode.Modify);

            for (var i = 0; i < document.Pages.Count; i++)
            {
                var page = document.Pages[i];
                var resources = page.Elements.GetDictionary("/Resources");
                if (resources is null)
                {
                    continue;
                }

                var xObjects = resources.Elements.GetDictionary("/XObject");
                if (xObjects is null)
                {
                    continue;
                }

                var items = xObjects.Elements.Values.OfType<PdfReference>().ToList();
                foreach (var item in items)
                {
                    if (item.Value is not PdfDictionary xObjectDict)
                    {
                        continue;
                    }

                    if (xObjectDict.Elements.GetString("/Subtype") != "/Image")
                    {
                        continue;
                    }

                    var originalBytes = xObjectDict.Stream.Value;
                    var qualityToUse = GetJpegQuality(options.Level);
                    var compressedImage = _imageCompressor.Compress(originalBytes, qualityToUse);

                    if (compressedImage is not null)
                    {
                        ApplyCompressedImage(xObjectDict, compressedImage, originalBytes);
                    }
                }
            }

            document.Options.CompressContentStreams = true;
            document.Options.NoCompression = false;
            document.Options.FlateEncodeMode = options.Level switch
            {
                CompressionLevel.Maximum => PdfFlateEncodeMode.BestCompression,
                _ => PdfFlateEncodeMode.Default
            };

            document.Save(outputPath);

            var inputSize = new FileInfo(input.FilePath).Length;
            if (new FileInfo(outputPath).Length > inputSize)
            {
                File.Copy(input.FilePath, outputPath, overwrite: true);
            }

            return new OperationResult(true, Path.GetFullPath(outputPath), string.Empty);
        });
    }

    private static int GetJpegQuality(CompressionLevel level) =>
        level switch
        {
            CompressionLevel.Maximum => MaximumJpegQuality,
            _ => NormalJpegQuality
        };

    private static void ApplyCompressedImage(
        PdfDictionary xObjectDict,
        CompressedImageResult result,
        byte[] originalBytes)
    {
        if (result.Bytes.Length >= originalBytes.Length)
        {
            return;
        }

        xObjectDict.Stream.Value = result.Bytes;
        xObjectDict.Elements.SetName("/Filter", "/DCTDecode");
        xObjectDict.Elements.SetInteger("/Length", result.Bytes.Length);
        xObjectDict.Elements.SetInteger("/Width", result.Width);
        xObjectDict.Elements.SetInteger("/Height", result.Height);
        xObjectDict.Elements.SetInteger("/BitsPerComponent", 8);
        xObjectDict.Elements.SetName("/ColorSpace", "/DeviceRGB");
        xObjectDict.Elements.Remove("/DecodeParms");
        xObjectDict.Elements.Remove("/SMask");
    }
}
