using System.Collections.Generic;
using System.Linq;

namespace PDFToys.App.Services;

public static class BatchStatusFormatter
{
    public static string FormatCompletion(
        string successVerb,
        int succeeded,
        int total,
        IReadOnlyList<string> failedFiles,
        IReadOnlyList<string> failureMessages)
    {
        if (failedFiles.Count == 0)
        {
            return $"{successVerb} {succeeded}/{total} files successfully!";
        }

        var summary = $"{successVerb} {succeeded}/{total} files successfully, {failedFiles.Count} failed.";
        var details = FormatFailureDetails(failedFiles, failureMessages);
        return string.IsNullOrWhiteSpace(details) ? summary : $"{summary} {details}";
    }

    public static string FormatFailureDetails(
        IReadOnlyList<string> failedFiles,
        IReadOnlyList<string> failureMessages)
    {
        if (failedFiles.Count == 0)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        for (var i = 0; i < failedFiles.Count; i++)
        {
            var file = failedFiles[i];
            var message = i < failureMessages.Count ? failureMessages[i] : "Unexpected error.";
            parts.Add($"{file}: {message}");
        }

        return $"Failures: {string.Join("; ", parts)}";
    }

    public static string FormatErrors(IReadOnlyList<string> errors)
    {
        if (errors.Count == 0)
        {
            return "Operation failed.";
        }

        return errors.Count == 1
            ? errors[0]
            : string.Join("; ", errors);
    }
}
