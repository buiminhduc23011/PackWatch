using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PackWatch.App.Navigation;
using PackWatch.App.Services;
using PackWatch.Application.Abstractions.Barcodes;
using PackWatch.Application.Abstractions.Cameras;
using PackWatch.Application.Abstractions.Media;
using PackWatch.Application.Abstractions.Settings;
using PackWatch.Application.Navigation;
using System.IO;
using System.Net.Sockets;

namespace PackWatch.App.ViewModels;

public sealed partial class SettingsPageViewModel : ObservableObject, INavigationAware
{
    private const string WebcamCameraMode = "Webcam";
    private const string RtspCameraMode = "RTSP Camera";
    private const string LaptopWebcamProfile = "Laptop webcam (device 0)";
    private const string SecondaryWebcamProfile = "USB webcam (device 1)";
    private const string CustomWebcamProfile = "Custom device index";

    private readonly ISettingsService _settingsService;
    private readonly IAppStatusService _appStatusService;
    private readonly IDesktopShellService _desktopShellService;
    private readonly INavigationService _navigationService;

    private bool _isHydrating;

    public SettingsPageViewModel(
        ISettingsService settingsService,
        IAppStatusService appStatusService,
        IDesktopShellService desktopShellService,
        INavigationService navigationService)
    {
        _settingsService = settingsService;
        _appStatusService = appStatusService;
        _desktopShellService = desktopShellService;
        _navigationService = navigationService;
    }

    public IReadOnlyList<string> CameraModes { get; } = [WebcamCameraMode, RtspCameraMode];

    public IReadOnlyList<string> WebcamProfiles { get; } = [LaptopWebcamProfile, SecondaryWebcamProfile, CustomWebcamProfile];

    public IReadOnlyList<string> ResolutionOptions { get; } = ["1280x720", "1920x1080", "2560x1440"];

    public IReadOnlyList<int> FramesPerSecondOptions { get; } = [15, 24, 30, 60];

    public IReadOnlyList<string> VideoFormatOptions { get; } = ["mp4", "mkv"];

    public IReadOnlyList<string> RetentionOptions { get; } = ["30 days", "60 days", "90 days", "120 days", "Never"];

    public IReadOnlyList<string> LoggingLevelOptions { get; } = ["Verbose", "Information", "Warning", "Error"];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRtspCamera))]
    [NotifyPropertyChangedFor(nameof(IsWebcamCamera))]
    [NotifyPropertyChangedFor(nameof(CameraModeBadge))]
    [NotifyPropertyChangedFor(nameof(CameraSourceSummary))]
    [NotifyPropertyChangedFor(nameof(CameraQuickTestTitle))]
    [NotifyPropertyChangedFor(nameof(CameraQuickTestDescription))]
    private string cameraMode = WebcamCameraMode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CameraSourceSummary))]
    [NotifyPropertyChangedFor(nameof(CameraQuickTestTitle))]
    [NotifyPropertyChangedFor(nameof(CameraQuickTestDescription))]
    private int webcamDeviceIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CameraSourceSummary))]
    private string rtspUrl = string.Empty;

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RecordingSummary))]
    private string saveFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
        "PackWatch");

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RecordingSummary))]
    [NotifyPropertyChangedFor(nameof(RecordingBadge))]
    private int framesPerSecond = 30;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RecordingSummary))]
    private string resolution = "1920x1080";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RecordingSummary))]
    private int bitrateKbps = 6000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RecordingSummary))]
    private string videoFormat = "mp4";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RetentionSummary))]
    private string retentionPolicy = "120 days";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RoiSummary))]
    private int roiX = 120;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RoiSummary))]
    private int roiY = 120;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RoiSummary))]
    private int roiWidth = 720;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RoiSummary))]
    private int roiHeight = 260;

    [ObservableProperty]
    private double stableBarcodeMilliseconds = 750;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BarcodeTypesSummary))]
    private bool enableQrCode = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BarcodeTypesSummary))]
    private bool enableCode128 = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BarcodeTypesSummary))]
    private bool enableCode39 = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BarcodeTypesSummary))]
    private bool enableEan13 = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LoggingSummary))]
    private string loggingLevel = "Information";

    [ObservableProperty]
    private string statusMessage = "Settings are ready to edit.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CameraSourceSummary))]
    [NotifyPropertyChangedFor(nameof(CameraQuickTestDescription))]
    private string selectedWebcamProfile = LaptopWebcamProfile;

    public bool IsRtspCamera => string.Equals(CameraMode, RtspCameraMode, StringComparison.Ordinal);

    public bool IsWebcamCamera => string.Equals(CameraMode, WebcamCameraMode, StringComparison.Ordinal);

    public string CameraModeBadge => IsRtspCamera ? "Network stream" : "Local camera";

    public string CameraSourceSummary => IsRtspCamera
        ? string.IsNullOrWhiteSpace(RtspUrl)
            ? string.Empty
            : $"RTSP URL: {RtspUrl}"
        : $"Webcam: device {WebcamDeviceIndex}";

    public string CameraQuickTestTitle => IsRtspCamera
        ? "Switch back to the laptop camera for desk checks"
        : WebcamDeviceIndex == 0
            ? "Laptop webcam is ready for the fastest local preview"
            : $"Webcam device {WebcamDeviceIndex} is selected for local preview";

    public string CameraQuickTestDescription => IsRtspCamera
        ? "Keep RTSP credentials below for the line camera, but you can jump back to the laptop webcam in one click whenever you want a fast sanity check."
        : SelectedWebcamProfile == LaptopWebcamProfile
            ? "Built-in webcam usually maps to device 0 on Windows, so this preset is the quickest way to validate framing, focus, and barcode ROI on the same laptop."
            : SelectedWebcamProfile == SecondaryWebcamProfile
                ? "Use this when you have a USB webcam attached as the second local camera device."
                : "Manual device index stays available for machines where Windows orders cameras differently.";

    public string RecordingBadge => $"{FramesPerSecond} FPS";

    public string RecordingSummary => $"{Resolution} | {FramesPerSecond} FPS | {BitrateKbps:N0} Kbps | {VideoFormat.ToUpperInvariant()}";

    public string RoiSummary => $"{RoiWidth} x {RoiHeight} at X {RoiX} / Y {RoiY}";

    public string BarcodeTypesSummary => $"{GetEnabledBarcodeTypes().Count} formats enabled";

    public string RetentionSummary => $"Auto-clean: {RetentionPolicy}";

    public string LoggingSummary => $"Level: {LoggingLevel}";

    public void OnNavigatedTo()
    {
        _ = LoadAsync();
    }

    public void OnNavigatedFrom()
    {
    }

    partial void OnWebcamDeviceIndexChanged(int value)
    {
        if (_isHydrating)
        {
            return;
        }

        var profile = value switch
        {
            0 => LaptopWebcamProfile,
            1 => SecondaryWebcamProfile,
            _ => CustomWebcamProfile
        };

        if (!string.Equals(SelectedWebcamProfile, profile, StringComparison.Ordinal))
        {
            SelectedWebcamProfile = profile;
        }
    }

    partial void OnSelectedWebcamProfileChanged(string value)
    {
        if (_isHydrating)
        {
            return;
        }

        var nextIndex = value switch
        {
            LaptopWebcamProfile => 0,
            SecondaryWebcamProfile => 1,
            _ => WebcamDeviceIndex
        };

        if (WebcamDeviceIndex != nextIndex)
        {
            WebcamDeviceIndex = nextIndex;
        }
    }

    [RelayCommand]
    private void UseLaptopWebcamQuickTest()
    {
        CameraMode = WebcamCameraMode;
        SelectedWebcamProfile = LaptopWebcamProfile;
        WebcamDeviceIndex = 0;
        StatusMessage = "Laptop webcam on device 0 is now the shared source for both live preview and recording. Open Home to see the embedded preview.";
        _appStatusService.SetStatus(StatusMessage);
    }

    [RelayCommand]
    private async Task TestConnectionAsync(CancellationToken cancellationToken)
    {
        if (IsWebcamCamera)
        {
            StatusMessage = $"Webcam device {WebcamDeviceIndex} is ready as the shared live and recording source. Use Preview to launch Windows Camera for a hardware check.";
            _appStatusService.SetStatus(StatusMessage);
            return;
        }

        if (!TryBuildRtspUri(out var rtspUri, out var validationMessage))
        {
            StatusMessage = validationMessage;
            _appStatusService.SetStatus(validationMessage);
            return;
        }

        try
        {
            using var tcpClient = new TcpClient();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(3));

            var port = rtspUri.Port > 0 ? rtspUri.Port : 554;
            await tcpClient.ConnectAsync(rtspUri.Host, port, timeoutCts.Token);

            StatusMessage = $"Connected to {rtspUri.Host}:{port}. The RTSP endpoint is reachable from this machine.";
            _appStatusService.SetStatus(StatusMessage);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = $"Timed out while checking {rtspUri.Host}.";
            _appStatusService.SetStatus(StatusMessage);
        }
        catch (Exception exception)
        {
            StatusMessage = $"Could not reach {rtspUri.Host}:{rtspUri.Port}. {exception.Message}";
            _appStatusService.SetStatus(StatusMessage);
        }
    }

    [RelayCommand]
    private Task PreviewAsync(CancellationToken cancellationToken)
    {
        if (IsWebcamCamera)
        {
            _navigationService.NavigateTo(ApplicationPage.Home);
            StatusMessage = "Opening Home so the selected webcam can appear inside the live preview surface.";
            _appStatusService.SetStatus(StatusMessage);

            return Task.CompletedTask;
        }

        StatusMessage = "RTSP preview is not embedded yet. Use Test Connection to verify reachability, then run a validation session from Home.";
        _appStatusService.SetStatus(StatusMessage);
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task OpenSaveFolderAsync(CancellationToken cancellationToken)
    {
        await EnsureSaveFolderExistsAsync(cancellationToken);

        if (_desktopShellService.OpenFolder(SaveFolder))
        {
            StatusMessage = $"Opened save folder: {SaveFolder}";
            _appStatusService.SetStatus(StatusMessage);
            return;
        }

        StatusMessage = "Could not open the save folder.";
        _appStatusService.SetStatus(StatusMessage);
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        if (!TryBuildSettings(out var settings, out var validationMessage))
        {
            StatusMessage = validationMessage;
            _appStatusService.SetStatus(validationMessage);
            return;
        }

        await _settingsService.SaveAsync(settings, cancellationToken);
        StatusMessage = $"Saved the shared camera source and capture profile for {CameraMode}.";
        _appStatusService.SetStatus(StatusMessage);
    }

    private async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _settingsService.GetAsync(cancellationToken);

            _isHydrating = true;

            CameraMode = settings.Camera.SourceKind == CameraSourceKind.Rtsp
                ? RtspCameraMode
                : WebcamCameraMode;
            WebcamDeviceIndex = settings.Camera.DeviceIndex ?? 0;
            SelectedWebcamProfile = GetWebcamProfile(WebcamDeviceIndex, settings.Camera.DeviceName);
            RtspUrl = settings.Camera.RtspUri?.ToString() ?? string.Empty;
            Username = settings.Camera.Username ?? string.Empty;
            Password = settings.Camera.Password ?? string.Empty;
            SaveFolder = settings.SaveFolder;
            FramesPerSecond = settings.FramesPerSecond;
            Resolution = settings.Resolution;
            BitrateKbps = settings.BitrateKbps;
            VideoFormat = settings.VideoFormat;
            RetentionPolicy = MapRetentionPolicy(settings.RetentionDays);
            RoiX = settings.BarcodeRegionOfInterest.X;
            RoiY = settings.BarcodeRegionOfInterest.Y;
            RoiWidth = settings.BarcodeRegionOfInterest.Width;
            RoiHeight = settings.BarcodeRegionOfInterest.Height;
            StableBarcodeMilliseconds = settings.BarcodeStableMilliseconds;
            EnableQrCode = settings.EnabledBarcodeFormats.Contains(BarcodeFormatKind.QrCode);
            EnableCode128 = settings.EnabledBarcodeFormats.Contains(BarcodeFormatKind.Code128);
            EnableCode39 = settings.EnabledBarcodeFormats.Contains(BarcodeFormatKind.Code39);
            EnableEan13 = settings.EnabledBarcodeFormats.Contains(BarcodeFormatKind.Ean13);
            LoggingLevel = settings.LoggingLevel;

            _isHydrating = false;
            StatusMessage = "Loaded the current PackWatch profile from local storage.";
        }
        catch (Exception exception)
        {
            _isHydrating = false;
            StatusMessage = "Could not load the local PackWatch profile.";
            _appStatusService.SetStatus($"Failed to load settings: {exception.Message}");
        }
    }

    private bool TryBuildSettings(out ApplicationSettings settings, out string validationMessage)
    {
        if (string.IsNullOrWhiteSpace(SaveFolder))
        {
            settings = default!;
            validationMessage = "Choose a save folder before saving settings.";
            return false;
        }

        CameraConnectionOptions cameraConnectionOptions;

        if (IsRtspCamera)
        {
            if (!TryBuildRtspUri(out var rtspUri, out validationMessage))
            {
                settings = default!;
                return false;
            }

            cameraConnectionOptions = new CameraConnectionOptions(
                CameraSourceKind.Rtsp,
                DeviceName: null,
                DeviceIndex: null,
                RtspUri: rtspUri,
                Username: string.IsNullOrWhiteSpace(Username) ? null : Username.Trim(),
                Password: string.IsNullOrWhiteSpace(Password) ? null : Password);
        }
        else
        {
            cameraConnectionOptions = new CameraConnectionOptions(
                CameraSourceKind.Webcam,
                DeviceName: SelectedWebcamProfile,
                DeviceIndex: WebcamDeviceIndex,
                RtspUri: null,
                Username: null,
                Password: null);
            validationMessage = string.Empty;
        }

        settings = new ApplicationSettings(
            cameraConnectionOptions,
            SaveFolder.Trim(),
            FramesPerSecond,
            Resolution,
            BitrateKbps,
            VideoFormat,
            ParseRetentionDays(RetentionPolicy),
            new FrameRegion(RoiX, RoiY, RoiWidth, RoiHeight),
            Convert.ToInt32(StableBarcodeMilliseconds),
            GetEnabledBarcodeTypes(),
            LoggingLevel);

        return true;
    }

    private bool TryBuildRtspUri(out Uri rtspUri, out string validationMessage)
    {
        if (string.IsNullOrWhiteSpace(RtspUrl))
        {
            rtspUri = null!;
            validationMessage = "Enter an RTSP URL before saving or testing the network camera.";
            return false;
        }

        if (!Uri.TryCreate(RtspUrl.Trim(), UriKind.Absolute, out var parsedUri)
            || parsedUri is null
            || (parsedUri.Scheme != "rtsp" && parsedUri.Scheme != "rtsps"))
        {
            rtspUri = null!;
            validationMessage = "Use a valid RTSP URL such as rtsp://camera-host:554/stream.";
            return false;
        }

        rtspUri = parsedUri;
        validationMessage = string.Empty;
        return true;
    }

    private async Task EnsureSaveFolderExistsAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(SaveFolder))
        {
            return;
        }

        await Task.Run(() => Directory.CreateDirectory(SaveFolder.Trim()), cancellationToken);
    }

    private static string GetWebcamProfile(int deviceIndex, string? deviceName)
    {
        if (!string.IsNullOrWhiteSpace(deviceName) && deviceName.Contains("Laptop webcam", StringComparison.OrdinalIgnoreCase))
        {
            return LaptopWebcamProfile;
        }

        return deviceIndex switch
        {
            0 => LaptopWebcamProfile,
            1 => SecondaryWebcamProfile,
            _ => CustomWebcamProfile
        };
    }

    private static string MapRetentionPolicy(int retentionDays)
    {
        return retentionDays <= 0
            ? "Never"
            : $"{retentionDays} days";
    }

    private static int ParseRetentionDays(string retentionPolicy)
    {
        return retentionPolicy == "Never"
            ? 0
            : int.TryParse(retentionPolicy.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0], out var days)
                ? days
                : 120;
    }

    private IReadOnlyList<BarcodeFormatKind> GetEnabledBarcodeTypes()
    {
        return new BarcodeFormatKind?[]
        {
            EnableQrCode ? BarcodeFormatKind.QrCode : null,
            EnableCode128 ? BarcodeFormatKind.Code128 : null,
            EnableCode39 ? BarcodeFormatKind.Code39 : null,
            EnableEan13 ? BarcodeFormatKind.Ean13 : null
        }.Where(format => format is not null)
         .Cast<BarcodeFormatKind>()
         .ToList();
    }
}
