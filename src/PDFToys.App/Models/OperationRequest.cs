using System.Collections.Generic;

namespace PDFToys.App.Models;

public enum PdfToysOperation
{
    Home,
    Merge,
    Split,
    Compress,
    Protect,
    Unlock,
    Rotate,
    ExtractPages,
    OrganizePages,
    Rearrange,
    ConvertToPdf,
    ConvertFromPdf,
    PdfToJpg,
    PdfToJpeg,
    PdfToPng,
    PdfToMarkdown,
    CombineToPdf,
    ConvertEachToPdf
}

public sealed record OperationRequest(
    PdfToysOperation Operation,
    IReadOnlyList<string> Inputs,
    string? TargetFormat = null,
    string? Workspace = null);
