using PDFToys.App.Models;
using System.Collections.Generic;
using System.Linq;

namespace PDFToys.App.Services;

public enum StartupRouteKind
{
    Home,
    Merge,
    Split,
    Compress,
    Protect,
    OrganizePages,
    Unlock,
    ConvertExport,
    HeadlessConversion,
    Placeholder
}

public sealed record StartupRoute(
    StartupRouteKind Kind,
    PdfToysOperation Operation,
    IReadOnlyList<string> Inputs,
    string? TargetFormat,
    string? Workspace,
    bool IsPlaceholder);

public sealed class StartupOperationRouter
{
    public StartupRoute BuildRoute(OperationRequest request)
    {
        var sanitizedInputs = request.Inputs
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToList();

        if (request.Operation == PdfToysOperation.ConvertFromPdf)
        {
            return BuildConvertFromPdfRoute(sanitizedInputs, request.TargetFormat, request.Workspace);
        }

        return request.Operation switch
        {
            PdfToysOperation.Home => new StartupRoute(
                StartupRouteKind.Home,
                request.Operation,
                sanitizedInputs,
                request.TargetFormat,
                request.Workspace,
                false),
            PdfToysOperation.Merge => new StartupRoute(
                StartupRouteKind.Merge,
                request.Operation,
                sanitizedInputs,
                request.TargetFormat,
                request.Workspace,
                false),
            PdfToysOperation.Split => new StartupRoute(
                StartupRouteKind.Split,
                request.Operation,
                sanitizedInputs,
                request.TargetFormat,
                request.Workspace,
                false),
            PdfToysOperation.Compress => new StartupRoute(
                StartupRouteKind.Compress,
                request.Operation,
                sanitizedInputs,
                request.TargetFormat,
                request.Workspace,
                false),
            PdfToysOperation.Protect => new StartupRoute(
                StartupRouteKind.Protect,
                request.Operation,
                sanitizedInputs,
                request.TargetFormat,
                request.Workspace,
                false),
            PdfToysOperation.OrganizePages => new StartupRoute(
                StartupRouteKind.OrganizePages,
                request.Operation,
                sanitizedInputs,
                request.TargetFormat,
                request.Workspace,
                false),
            PdfToysOperation.Unlock => new StartupRoute(
                StartupRouteKind.Unlock,
                request.Operation,
                sanitizedInputs,
                request.TargetFormat,
                request.Workspace,
                false),
            PdfToysOperation.Rotate => new StartupRoute(
                StartupRouteKind.OrganizePages,
                PdfToysOperation.OrganizePages,
                sanitizedInputs,
                request.TargetFormat,
                request.Workspace,
                false),
            PdfToysOperation.ExtractPages => new StartupRoute(
                StartupRouteKind.OrganizePages,
                PdfToysOperation.OrganizePages,
                sanitizedInputs,
                request.TargetFormat,
                request.Workspace,
                false),
            PdfToysOperation.Rearrange => new StartupRoute(
                StartupRouteKind.OrganizePages,
                PdfToysOperation.OrganizePages,
                TakeFirstInput(sanitizedInputs),
                request.TargetFormat,
                request.Workspace,
                false),
            PdfToysOperation.ConvertToPdf => new StartupRoute(
                StartupRouteKind.HeadlessConversion,
                request.Operation,
                sanitizedInputs,
                request.TargetFormat,
                request.Workspace,
                false),
            PdfToysOperation.CombineToPdf => new StartupRoute(
                StartupRouteKind.HeadlessConversion,
                request.Operation,
                sanitizedInputs,
                request.TargetFormat,
                request.Workspace,
                false),
            PdfToysOperation.ConvertEachToPdf => new StartupRoute(
                StartupRouteKind.HeadlessConversion,
                request.Operation,
                sanitizedInputs,
                request.TargetFormat,
                request.Workspace,
                false),
            PdfToysOperation.PdfToJpg => new StartupRoute(
                StartupRouteKind.HeadlessConversion,
                request.Operation,
                sanitizedInputs,
                request.TargetFormat,
                request.Workspace,
                false),
            PdfToysOperation.PdfToJpeg => new StartupRoute(
                StartupRouteKind.HeadlessConversion,
                request.Operation,
                sanitizedInputs,
                request.TargetFormat,
                request.Workspace,
                false),
            PdfToysOperation.PdfToPng => new StartupRoute(
                StartupRouteKind.HeadlessConversion,
                request.Operation,
                sanitizedInputs,
                request.TargetFormat,
                request.Workspace,
                false),
            PdfToysOperation.PdfToMarkdown => new StartupRoute(
                StartupRouteKind.HeadlessConversion,
                request.Operation,
                sanitizedInputs,
                request.TargetFormat,
                request.Workspace,
                false),
            _ => new StartupRoute(
                StartupRouteKind.Placeholder,
                request.Operation,
                sanitizedInputs,
                request.TargetFormat,
                request.Workspace,
                true)
        };
    }

    private static StartupRoute BuildConvertFromPdfRoute(
        IReadOnlyList<string> inputs,
        string? targetFormat,
        string? workspace)
    {
        if (ConvertFromPdfTargetResolver.TryResolve(targetFormat, out var resolvedOperation))
        {
            return new StartupRoute(
                StartupRouteKind.HeadlessConversion,
                resolvedOperation,
                inputs,
                targetFormat,
                workspace,
                false);
        }

        return new StartupRoute(
            StartupRouteKind.HeadlessConversion,
            PdfToysOperation.ConvertFromPdf,
            inputs,
            targetFormat,
            workspace,
            false);
    }

    private static IReadOnlyList<string> TakeFirstInput(IReadOnlyList<string> inputs)
    {
        if (inputs.Count == 0)
        {
            return inputs;
        }

        return [inputs[0]];
    }
}
