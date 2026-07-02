using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;

namespace PackWatch.App.ViewModels;

public sealed partial class SettingsPageViewModel : ObservableObject
{
    private const string RtspCameraMode = "RTSP Camera";

    public IReadOnlyList<string> CameraModes { get; } = ["Webcam", RtspCameraMode];

    public IReadOnlyList<string> ResolutionOptions { get; } = ["1280x720", "1920x1080", "2560x1440"];

    public IReadOnlyList<int> FramesPerSecondOptions { get; } = [15, 24, 30, 60];

    public IReadOnlyList<string> VideoFormatOptions { get; } = ["mp4", "mkv"];

    public IReadOnlyList<string> RetentionOptions { get; } = ["30 days", "60 days", "90 days", "Never"];

    public IReadOnlyList<string> LoggingLevelOptions { get; } = ["Verbose", "Information", "Warning", "Error"];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRtspCamera))]
    private string cameraMode = "Webcam";

    [ObservableProperty]
    private int webcamDeviceIndex;

    [ObservableProperty]
    private string rtspUrl = string.Empty;

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string saveFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
        "PackWatch");

    [ObservableProperty]
    private int framesPerSecond = 30;

    [ObservableProperty]
    private string resolution = "1920x1080";

    [ObservableProperty]
    private int bitrateKbps = 6000;

    [ObservableProperty]
    private string videoFormat = "mp4";

    [ObservableProperty]
    private string retentionPolicy = "90 days";

    [ObservableProperty]
    private int roiX = 120;

    [ObservableProperty]
    private int roiY = 120;

    [ObservableProperty]
    private int roiWidth = 720;

    [ObservableProperty]
    private int roiHeight = 260;

    [ObservableProperty]
    private double stableBarcodeMilliseconds = 750;

    [ObservableProperty]
    private bool enableQrCode = true;

    [ObservableProperty]
    private bool enableCode128 = true;

    [ObservableProperty]
    private bool enableCode39 = true;

    [ObservableProperty]
    private bool enableEan13 = true;

    [ObservableProperty]
    private string loggingLevel = "Information";

    [ObservableProperty]
    private string statusMessage = "Settings are ready to edit.";

    public bool IsRtspCamera => string.Equals(CameraMode, RtspCameraMode, StringComparison.Ordinal);

    [RelayCommand]
    private Task TestConnectionAsync(CancellationToken cancellationToken)
    {
        StatusMessage = IsRtspCamera && string.IsNullOrWhiteSpace(RtspUrl)
            ? "Enter an RTSP URL before testing the camera connection."
            : $"Connection check queued for {CameraMode}.";

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task PreviewAsync(CancellationToken cancellationToken)
    {
        StatusMessage = $"Preview will use {CameraMode} at {Resolution} / {FramesPerSecond} FPS.";
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task SaveAsync(CancellationToken cancellationToken)
    {
        var enabledFormats = new[]
        {
            EnableQrCode ? "QR" : null,
            EnableCode128 ? "Code128" : null,
            EnableCode39 ? "Code39" : null,
            EnableEan13 ? "EAN13" : null
        }.Where(format => format is not null);

        StatusMessage = $"Saved current UI settings: {CameraMode}, {VideoFormat}, {string.Join(", ", enabledFormats)}.";
        return Task.CompletedTask;
    }
}
