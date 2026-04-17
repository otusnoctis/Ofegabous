using System.Diagnostics;

namespace App.Services;

public sealed class SystemShellService
{
    public bool TryOpenDirectory(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{directoryPath}\"",
                UseShellExecute = true
            });

            return true;
        }
        catch
        {
            return false;
        }
    }
}
