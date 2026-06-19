using System.Reflection;
using System.Runtime.InteropServices;
using PDFToys.Core.Contracts;
using PDFToys.Core.Models;

namespace PDFToys.Core.OS.Windows;

public sealed class OfficeComPdfConverter : IConversionStrategy
{
    private const int WdExportFormatPdf = 17;
    private const int XlFixedFormatTypePdf = 0;
    private const int PpSaveAsPdf = 32;
    private const int MsoTriStateFalse = 0;
    private const int PpAlertsNone = 1;

    public bool CanHandle(string inputExtension)
    {
        var ext = inputExtension.ToLowerInvariant();
        return ext is ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx";
    }

    public OperationResult Execute(string inputFilePath, ConversionOptions options, string outputFilePath)
    {
        var ext = Path.GetExtension(inputFilePath).ToLowerInvariant();

        switch (ext)
        {
            case ".doc":
            case ".docx":
                ConvertWord(inputFilePath, outputFilePath);
                break;
            case ".xls":
            case ".xlsx":
                ConvertExcel(inputFilePath, outputFilePath);
                break;
            case ".ppt":
            case ".pptx":
                ConvertPowerPoint(inputFilePath, outputFilePath);
                break;
            default:
                return new OperationResult(false, string.Empty, $"Unsupported Office format: {ext}");
        }

        return new OperationResult(true, Path.GetFullPath(outputFilePath), string.Empty);
    }

    private void ConvertWord(string inputPath, string outputPath)
    {
        object? app = null;
        object? document = null;

        try
        {
            app = CreateComInstance("Word.Application", "Microsoft Word");
            SetProperty(app, "Visible", false);
            SetProperty(app, "DisplayAlerts", MsoTriStateFalse);

            var documents = GetProperty(app, "Documents");
            document = InvokeMethod(documents, "Open", inputPath, false, true, false);

            InvokeMethod(document, "ExportAsFixedFormat", outputPath, WdExportFormatPdf);
        }
        finally
        {
            if (document is not null)
            {
                try { InvokeMethod(document, "Close", false); } catch { }
                ReleaseComObject(document);
            }
            if (app is not null)
            {
                try { InvokeMethod(app, "Quit"); } catch { }
                ReleaseComObject(app);
            }
        }
    }

    private void ConvertExcel(string inputPath, string outputPath)
    {
        object? app = null;
        object? workbook = null;

        try
        {
            app = CreateComInstance("Excel.Application", "Microsoft Excel");
            SetProperty(app, "Visible", false);
            SetProperty(app, "DisplayAlerts", false);

            var workbooks = GetProperty(app, "Workbooks");
            workbook = InvokeMethod(workbooks, "Open", inputPath, 0, true);

            InvokeMethod(workbook, "ExportAsFixedFormat", XlFixedFormatTypePdf, outputPath);
        }
        finally
        {
            if (workbook is not null)
            {
                try { InvokeMethod(workbook, "Close", false); } catch { }
                ReleaseComObject(workbook);
            }
            if (app is not null)
            {
                try { InvokeMethod(app, "Quit"); } catch { }
                ReleaseComObject(app);
            }
        }
    }

    private void ConvertPowerPoint(string inputPath, string outputPath)
    {
        object? app = null;
        object? presentation = null;
        var outputFullPath = Path.GetFullPath(outputPath);

        try
        {
            app = CreateComInstance("PowerPoint.Application", "Microsoft PowerPoint");
            TrySetProperty(app, "Visible", MsoTriStateFalse);
            SetProperty(app, "DisplayAlerts", PpAlertsNone);

            var presentations = GetProperty(app, "Presentations");
            presentation = InvokeMethod(presentations, "Open", Path.GetFullPath(inputPath), MsoTriStateFalse, MsoTriStateFalse, MsoTriStateFalse);

            InvokeMethod(presentation, "SaveAs", outputFullPath, PpSaveAsPdf);

            if (!File.Exists(outputFullPath))
            {
                throw new InvalidOperationException($"PowerPoint did not create the PDF at '{outputFullPath}'.");
            }
        }
        finally
        {
            if (presentation is not null)
            {
                try { InvokeMethod(presentation, "Close"); } catch { }
                ReleaseComObject(presentation);
            }
            if (app is not null)
            {
                try { InvokeMethod(app, "Quit"); } catch { }
                ReleaseComObject(app);
            }
        }
    }

    private Exception UnwrapInvocationException(Exception exception)
    {
        if (exception is TargetInvocationException { InnerException: { } inner })
        {
            return inner;
        }
        return exception;
    }

    private object CreateComInstance(string progId, string displayName)
    {
        var type = Type.GetTypeFromProgID(progId, throwOnError: true);
        if (type is null) throw new InvalidOperationException($"{displayName} is not installed or cannot be started.");

        return Activator.CreateInstance(type)
            ?? throw new InvalidOperationException($"{displayName} is not installed or cannot be started.");
    }

    private object GetProperty(object target, string propertyName)
    {
        try
        {
            return target.GetType().InvokeMember(propertyName, BindingFlags.GetProperty, null, target, null)!;
        }
        catch (Exception ex) { throw UnwrapInvocationException(ex); }
    }

    private void SetProperty(object target, string propertyName, object value)
    {
        try
        {
            target.GetType().InvokeMember(propertyName, BindingFlags.SetProperty, null, target, [value]);
        }
        catch (Exception ex) { throw UnwrapInvocationException(ex); }
    }

    private void TrySetProperty(object target, string propertyName, object value)
    {
        try { SetProperty(target, propertyName, value); }
        catch { /* Some Office installs block hiding the app window */ }
    }

    private object InvokeMethod(object target, string methodName, params object[] args)
    {
        try
        {
            return target.GetType().InvokeMember(methodName, BindingFlags.InvokeMethod, null, target, args)!;
        }
        catch (Exception ex) { throw UnwrapInvocationException(ex); }
    }

    private void ReleaseComObject(object comObject)
    {
        if (Marshal.IsComObject(comObject))
        {
            Marshal.FinalReleaseComObject(comObject);
        }
    }
}