using PDFToys.App.Models;
using PDFToys.Core.Contracts;
using PDFToys.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PDFToys.App.Services;

public sealed record HeadlessExecutionResult(
    int ExitCode,
    bool IsSuccess,
    IReadOnlyList<string> Outputs,
    IReadOnlyList<string> Errors,
    string LogPath);

public sealed class HeadlessConversionExecutor
{
    private readonly IConversionService _conversionService;
    private readonly IMergeService _mergeService;
    private readonly IExportService _exportService;

    public HeadlessConversionExecutor(
        IConversionService conversionService,
        IMergeService mergeService,
        IExportService exportService)
    {
        _conversionService = conversionService;
        _mergeService = mergeService;
        _exportService = exportService;
    }

    public HeadlessExecutionResult Execute(StartupRoute route)
    {
        var outputs = new List<string>();
        var errors = new List<string>();

        try
        {
            if (route.Kind != StartupRouteKind.HeadlessConversion)
            {
                errors.Add("Route is not configured for headless conversion.");
                var invalidLog = WriteLog(route, outputs, errors, 2);
                return new HeadlessExecutionResult(2, false, outputs, errors, invalidLog);
            }

            if (route.Inputs.Count == 0)
            {
                errors.Add("No input files were provided.");
                var invalidLog = WriteLog(route, outputs, errors, 2);
                return new HeadlessExecutionResult(2, false, outputs, errors, invalidLog);
            }

            var exitCode = ExecuteByOperation(route, outputs, errors);
            var logPath = WriteLog(route, outputs, errors, exitCode);
            return new HeadlessExecutionResult(exitCode, exitCode == 0, outputs, errors, logPath);
        }
        catch (Exception ex)
        {
            errors.Add($"Unhandled failure: {ex}");
            var crashLog = WriteLog(route, outputs, errors, 3);
            return new HeadlessExecutionResult(3, false, outputs, errors, crashLog);
        }
    }

    private int ExecuteByOperation(StartupRoute route, List<string> outputs, List<string> errors)
    {
        return route.Operation switch
        {
            PdfToysOperation.ConvertToPdf => ExecuteConvertToPdf(route.Inputs.First(), outputs, errors),
            PdfToysOperation.CombineToPdf => ExecuteCombineToPdf(route.Inputs, outputs, errors),
            PdfToysOperation.ConvertEachToPdf => ExecuteConvertEachToPdf(route.Inputs, outputs, errors),
            PdfToysOperation.PdfToJpg => ExecuteExportFromPdf(route.Inputs, PdfExportFormat.Jpg, outputs, errors),
            PdfToysOperation.PdfToJpeg => ExecuteExportFromPdf(route.Inputs, PdfExportFormat.Jpeg, outputs, errors),
            PdfToysOperation.PdfToPng => ExecuteExportFromPdf(route.Inputs, PdfExportFormat.Png, outputs, errors),
            PdfToysOperation.PdfToMarkdown => ExecuteExportFromPdf(route.Inputs, PdfExportFormat.Markdown, outputs, errors),
            PdfToysOperation.ConvertFromPdf => ReportConvertFromPdfError(errors, route.TargetFormat),
            _ => 2
        };
    }

    private static int ReportConvertFromPdfError(List<string> errors, string? targetFormat)
    {
        if (string.IsNullOrWhiteSpace(targetFormat))
        {
            errors.Add("convert-from-pdf requires --target (jpg, jpeg, png, markdown, or md).");
        }
        else
        {
            errors.Add($"Unsupported convert-from-pdf target '{targetFormat}'. Use jpg, jpeg, png, markdown, or md.");
        }

        return 2;
    }

    private int ExecuteConvertToPdf(string input, List<string> outputs, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            errors.Add("Invalid input path for to-pdf.");
            return 2;
        }

        var outputDirectory = ResolveOutputDirectory(input);
        var result = _conversionService.Convert(input, new ConversionOptions(outputDirectory));
        if (!result.IsSuccess)
        {
            errors.Add(result.ErrorMessage);
            return 1;
        }

        if (!string.IsNullOrWhiteSpace(result.OutputPath))
        {
            outputs.Add(result.OutputPath);
        }

        return 0;
    }

    private int ExecuteConvertEachToPdf(IReadOnlyList<string> inputs, List<string> outputs, List<string> errors)
    {
        var failed = false;
        foreach (var input in inputs)
        {
            var code = ExecuteConvertToPdf(input, outputs, errors);
            if (code != 0)
            {
                failed = true;
            }
        }

        return failed ? 1 : 0;
    }

    private int ExecuteCombineToPdf(IReadOnlyList<string> inputs, List<string> outputs, List<string> errors)
    {
        var convertedPdfPaths = new List<string>();
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"pdftoys-combine-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            foreach (var input in inputs)
            {
                var conversionResult = _conversionService.Convert(input, new ConversionOptions(tempDirectory));
                if (!conversionResult.IsSuccess || string.IsNullOrWhiteSpace(conversionResult.OutputPath))
                {
                    errors.Add(conversionResult.ErrorMessage);
                    return 1;
                }

                convertedPdfPaths.Add(conversionResult.OutputPath);
            }

            if (convertedPdfPaths.Count == 0)
            {
                errors.Add("No converted files available to combine.");
                return 2;
            }

            var mergeOutputDirectory = ResolveOutputDirectory(inputs[0]);
            var mergedName = $"combined_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
            var mergeResult = _mergeService.Merge(
                convertedPdfPaths.Select(path => new PdfFile(path)).ToArray(),
                new MergeOptions(mergeOutputDirectory, mergedName));
            if (!mergeResult.IsSuccess)
            {
                errors.Add(mergeResult.ErrorMessage);
                return 1;
            }

            if (!string.IsNullOrWhiteSpace(mergeResult.OutputPath))
            {
                outputs.Add(mergeResult.OutputPath);
            }

            return 0;
        }
        finally
        {
            TryDeleteDirectory(tempDirectory);
        }
    }

    private int ExecuteExportFromPdf(
        IReadOnlyList<string> inputs,
        PdfExportFormat format,
        List<string> outputs,
        List<string> errors)
    {
        var validInputs = inputs.Where(input => !string.IsNullOrWhiteSpace(input)).ToList();
        if (validInputs.Count == 0)
        {
            errors.Add("Invalid input PDF path.");
            return 2;
        }

        if (validInputs.Count != inputs.Count)
        {
            errors.Add("Invalid input PDF path.");
        }

        if (validInputs.Count == 1)
        {
            var input = validInputs[0];
            var outputDirectory = ResolveOutputDirectory(input);
            var exportResult = _exportService.Export(
                [new PdfFile(input)],
                new ExportOptions(outputDirectory, format));
            if (!exportResult.IsSuccess || string.IsNullOrWhiteSpace(exportResult.OutputPath))
            {
                errors.Add(exportResult.ErrorMessage);
                return 1;
            }

            outputs.Add(exportResult.OutputPath);
            return validInputs.Count == inputs.Count ? 0 : 1;
        }

        var batchOutputDirectory = ResolveOutputDirectory(validInputs[0]);
        var batchResult = _exportService.Export(
            validInputs.Select(path => new PdfFile(path)).ToList(),
            new ExportOptions(batchOutputDirectory, format));
        if (!batchResult.IsSuccess || string.IsNullOrWhiteSpace(batchResult.OutputPath))
        {
            errors.Add(batchResult.ErrorMessage);
            return 1;
        }

        outputs.Add(batchResult.OutputPath);
        return validInputs.Count == inputs.Count ? 0 : 1;
    }

    private static string ResolveOutputDirectory(string inputPath)
    {
        if (!string.IsNullOrWhiteSpace(inputPath))
        {
            var directory = Path.GetDirectoryName(inputPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                return directory;
            }
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    }

    private static string WriteLog(StartupRoute route, IReadOnlyList<string> outputs, IReadOnlyList<string> errors, int exitCode)
    {
        // Headless logs record paths and error messages only — never passwords or secrets.
        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PDFToys",
            "logs");
        Directory.CreateDirectory(logDirectory);

        var logPath = Path.Combine(logDirectory, $"headless_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.log");
        var builder = new StringBuilder();
        builder.AppendLine($"timestamp_utc={DateTime.UtcNow:O}");
        builder.AppendLine($"operation={route.Operation}");
        builder.AppendLine($"route_kind={route.Kind}");
        if (!string.IsNullOrWhiteSpace(route.TargetFormat))
        {
            builder.AppendLine($"target_format={route.TargetFormat}");
        }
        builder.AppendLine($"exit_code={exitCode}");
        builder.AppendLine("inputs:");
        foreach (var input in route.Inputs)
        {
            builder.AppendLine($"- {input}");
        }

        builder.AppendLine("outputs:");
        foreach (var output in outputs)
        {
            builder.AppendLine($"- {output}");
        }

        builder.AppendLine("errors:");
        foreach (var error in errors)
        {
            builder.AppendLine($"- {error}");
        }

        File.WriteAllText(logPath, builder.ToString());
        return logPath;
    }

    private static void TryDeleteDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Best-effort cleanup for temporary combine artifacts.
        }
    }
}
