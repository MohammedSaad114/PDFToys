using System;
using System.IO;

namespace PDFToys.App.Services;

public static class OutputFileHelper
{
    public const string BackupExtension = ".pdftoys.bak";

    public static string? MoveToExpectedPath(string actualPath, string desiredPath)
    {
        if (string.IsNullOrWhiteSpace(actualPath) || string.IsNullOrWhiteSpace(desiredPath))
        {
            return null;
        }

        if (!File.Exists(actualPath))
        {
            return null;
        }

        if (string.Equals(actualPath, desiredPath, StringComparison.OrdinalIgnoreCase))
        {
            return actualPath;
        }

        try
        {
            File.Move(actualPath, desiredPath, overwrite: true);
            return desiredPath;
        }
        catch
        {
            return null;
        }
    }

    public static bool TryReplaceOriginal(string newFilePath, string originalInputPath)
    {
        if (string.IsNullOrWhiteSpace(newFilePath) || string.IsNullOrWhiteSpace(originalInputPath))
        {
            return false;
        }

        if (!File.Exists(newFilePath))
        {
            return false;
        }

        if (string.Equals(newFilePath, originalInputPath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        try
        {
            if (File.Exists(originalInputPath))
            {
                var backupPath = BuildBackupPath(originalInputPath);
                try
                {
                    File.Copy(originalInputPath, backupPath, overwrite: true);
                }
                catch
                {
                    // Best-effort backup; continue with replace if backup fails.
                }
            }

            File.Move(newFilePath, originalInputPath, overwrite: true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string BuildBackupPath(string originalInputPath)
    {
        return originalInputPath + BackupExtension;
    }
}
