namespace PackWatch.App.Services;

public interface IDesktopShellService
{
    bool LaunchCameraApp();

    bool OpenFolder(string path);

    bool RevealPath(string path);

    bool OpenFile(string path);
}
