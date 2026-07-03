using System.Diagnostics;
using System.IO;

namespace PackWatch.App.Services;

public sealed class DesktopShellService : IDesktopShellService
{
    public bool LaunchCameraApp()
    {
        return StartShellTarget("microsoft.windows.camera:");
    }

    public bool OpenFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var folderPath = File.Exists(path)
            ? Path.GetDirectoryName(path)
            : path;

        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return false;
        }

        Directory.CreateDirectory(folderPath);
        return StartShellTarget(folderPath);
    }

    public bool RevealPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (File.Exists(path))
        {
            return StartProcess("explorer.exe", $"/select,\"{path}\"");
        }

        return OpenFolder(path);
    }

    public bool OpenFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return false;
        }

        return StartShellTarget(path);
    }

    private static bool StartShellTarget(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = target,
                UseShellExecute = true
            });

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool StartProcess(string fileName, string arguments)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
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
