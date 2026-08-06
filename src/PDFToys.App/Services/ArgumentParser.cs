using System;
using System.Collections.Generic;
using PDFToys.App.Models;

namespace PDFToys.App.Services;

public sealed class ArgumentParser
{
    public OperationRequest Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return new OperationRequest(PdfToysOperation.Home, []);
        }

        var operation = PdfToysOperation.Home;
        var inputs = new List<string>();
        string? targetFormat = null;
        string? workspace = null;

        for (var i = 0; i < args.Length; i++)
        {
            var current = args[i];
            if (current.Equals("--contract-version", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                i++;
                continue;
            }

            if (current.Equals("--command", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                operation = ParseOperation(args[++i]);
                continue;
            }

            if ((current.Equals("--operation", StringComparison.OrdinalIgnoreCase) ||
                 current.Equals("--mode", StringComparison.OrdinalIgnoreCase)) &&
                i + 1 < args.Length)
            {
                operation = ParseOperation(args[++i]);
                continue;
            }

            if (current.Equals("--input", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                inputs.Add(args[++i]);
                continue;
            }

            if (current.Equals("--target", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                targetFormat = args[++i];
                continue;
            }

            if (current.Equals("--workspace", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                workspace = args[++i];
            }
        }

        return new OperationRequest(operation, inputs, targetFormat, workspace);
    }

    private static PdfToysOperation ParseOperation(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "merge" => PdfToysOperation.Merge,
            "split" => PdfToysOperation.Split,
            "compress" => PdfToysOperation.Compress,
            "protect" => PdfToysOperation.Protect,
            "unlock" => PdfToysOperation.Unlock,
            "rotate" => PdfToysOperation.OrganizePages,
            "extract-pages" => PdfToysOperation.OrganizePages,
            "extractpages" => PdfToysOperation.OrganizePages,
            "organize-pages" => PdfToysOperation.OrganizePages,
            "organizepages" => PdfToysOperation.OrganizePages,
            "rearrange" => PdfToysOperation.Rearrange,
            "convert-to-pdf" => PdfToysOperation.ConvertToPdf,
            "converttopdf" => PdfToysOperation.ConvertToPdf,
            "to-pdf" => PdfToysOperation.ConvertToPdf,
            "convert-from-pdf" => PdfToysOperation.ConvertFromPdf,
            "convertfrompdf" => PdfToysOperation.ConvertFromPdf,
            "pdf-to-jpg" => PdfToysOperation.PdfToJpg,
            "pdf-to-jpeg" => PdfToysOperation.PdfToJpeg,
            "pdf-to-png" => PdfToysOperation.PdfToPng,
            "pdf-to-markdown" => PdfToysOperation.PdfToMarkdown,
            "combine-to-pdf" => PdfToysOperation.CombineToPdf,
            "convert-each-to-pdf" => PdfToysOperation.ConvertEachToPdf,
            "home" => PdfToysOperation.Home,
            _ => PdfToysOperation.Home
        };
    }
}
