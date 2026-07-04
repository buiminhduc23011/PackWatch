using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PackWatch.App.Navigation;
using PackWatch.App.Services;
using PackWatch.Application.Abstractions.Barcodes;
using PackWatch.Application.Abstractions.Cameras;
using PackWatch.Application.Abstractions.Media;
using PackWatch.Application.Abstractions.Recording;
using PackWatch.Application.Abstractions.Settings;
using AppVideoFrame = PackWatch.Application.Abstractions.Media.VideoFrame;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PackWatch.App.ViewModels;

public sealed partial class HomePageViewModel : ObservableObject, INavigationAware
{
    private readonly ISettingsService _settingsService;
    private readonly IRecordingService _recordingService;
    private readonly IDesktopShellService _desktopShellService;
    private readonly IAppStatusService _appStatusService;
    private readonly ICameraService _cameraService;
    private readonly IBarcodeService _barcodeService;
    private readonly DispatcherTimer _recordingTimer;

    private ApplicationSettings? _currentSettings;
    private RecordingSession? _currentSession;
    private RecordingOptions? _cachedRecordingOptions;
    private string? _activeBarcode;
    private bool _isScanningBarcode;
    private int _barcodeDetectionCount;

    [ObservableProperty]
    private string cameraStatus = "Loading validation profile...";

    [ObservableProperty]
    private string recordingStatus = "Ready";

    [ObservableProperty]
    private string currentOrder = "-";

    [ObservableProperty]
    private string recordingTime = "00:00:00";

    [ObservableProperty]
    private string lastBarcode = "-";

    [ObservableProperty]
    private string fps = "-";

    [ObservableProperty]
    private string resolution = "-";

    [ObservableProperty]
    private string previewMessage = "Connecting the shared camera source...";

    [ObservableProperty]
    private string detectedBarcodeInput = string.Empty;

    [ObservableProperty]
    private string cameraSourceSummary = "Waiting for settings...";

    [ObservableProperty]
    private string saveFolderSummary = "-";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SessionArtifactVisibility))]
    private string sessionArtifactPath = string.Empty;

    [ObservableProperty]
    private string sessionArtifactLabel = "No capture artifact yet.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewImageVisibility))]
    [NotifyPropertyChangedFor(nameof(PreviewPlaceholderVisibility))]
    private ImageSource? previewImage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    [NotifyPropertyChangedFor(nameof(IdleStateVisibility))]
    [NotifyPropertyChangedFor(nameof(ActiveStateVisibility))]
    [NotifyPropertyChangedFor(nameof(StartButtonText))]
    [NotifyPropertyChangedFor(nameof(ScanningVisibility))]
    [NotifyPropertyChangedFor(nameof(RecordingBadgeVisibility))]
    private bool isSessionActive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StartButtonText))]
    [NotifyPropertyChangedFor(nameof(ScanningVisibility))]
    [NotifyPropertyChangedFor(nameof(RecordingBadgeVisibility))]
    private bool isRecording;

    [ObservableProperty]
    private string barcodeDetectionCountText = "0";

    public HomePageViewModel(
        ISettingsService settingsService,
        IRecordingService recordingService,
        IDesktopShellService desktopShellService,
        IAppStatusService appStatusService,
        ICameraService cameraService,
        IBarcodeService barcodeService)
    {
        _settingsService = settingsService;
        _recordingService = recordingService;
        _desktopShellService = desktopShellService;
        _appStatusService = appStatusService;
        _cameraService = cameraService;
        _barcodeService = barcodeService;

        _cameraService.FrameAvailable += HandleFrameAvailable;

        _recordingTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _recordingTimer.Tick += HandleRecordingTimerTick;
    }

    public Visibility SessionArtifactVisibility => string.IsNullOrWhiteSpace(SessionArtifactPath)
        ? Visibility.Collapsed
        : Visibility.Visible;

    public Visibility PreviewImageVisibility => PreviewImage is null
        ? Visibility.Collapsed
        : Visibility.Visible;

    public Visibility PreviewPlaceholderVisibility => PreviewImage is null
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility IdleStateVisibility => IsSessionActive ? Visibility.Collapsed : Visibility.Visible;

    public Visibility ActiveStateVisibility => IsSessionActive ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ScanningVisibility => IsSessionActive && !IsRecording
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility RecordingBadgeVisibility => IsRecording
        ? Visibility.Visible
        : Visibility.Collapsed;

    public string StartButtonText => IsSessionActive
        ? (IsRecording ? "Recording..." : "Scanning...")
        : "Start Session";

    public void OnNavigatedTo()
    {
        _ = ActivateAsync();
    }

    public void OnNavigatedFrom()
    {
        if (IsSessionActive)
        {
            _ = StopAsync(CancellationToken.None);
        }

        if (_cameraService.IsOpen)
        {
            _ = _cameraService.CloseAsync(CancellationToken.None);
        }

        PreviewImage = null;
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = await EnsureSettingsAsync(cancellationToken);

        if (settings is null)
        {
            return;
        }

        if (settings.Camera.SourceKind == CameraSourceKind.Webcam && !_cameraService.IsOpen)
        {
            await EnsurePreviewAsync(settings, cancellationToken);
        }

        if (settings.Camera.SourceKind == CameraSourceKind.Rtsp && settings.Camera.RtspUri is null)
        {
            PreviewMessage = "RTSP mode is selected, but the stream URL is missing. Update it in Settings or switch to the laptop webcam for a quick test.";
            _appStatusService.SetStatus("Cannot start session because the RTSP URL is missing.");
            return;
        }

        // Cache recording options for reuse when switching barcodes.
        _cachedRecordingOptions = new RecordingOptions(
            DescribeCameraSource(settings.Camera),
            settings.FramesPerSecond,
            settings.Resolution,
            settings.BitrateKbps,
            settings.VideoFormat);

        // Enter scanning mode — do NOT start recording yet.
        IsSessionActive = true;
        IsRecording = false;
        _activeBarcode = null;
        _isScanningBarcode = false;
        _barcodeDetectionCount = 0;
        BarcodeDetectionCountText = "0";
        CurrentOrder = "-";
        LastBarcode = "-";
        DetectedBarcodeInput = string.Empty;
        RecordingTime = "00:00:00";
        RecordingStatus = "Scanning for barcode...";
        CameraStatus = BuildCameraStatus(settings.Camera, PreviewImage is not null);
        CameraSourceSummary = DescribeCameraSource(settings.Camera);
        PreviewMessage = PreviewImage is null
            ? "Session is active. Scanning for barcodes..."
            : PreviewMessage;

        _appStatusService.SetStatus("Session started. Show a barcode to the camera to begin recording.");
        PlaySessionStartSound();
    }

    [RelayCommand(CanExecute = nameof(CanStop))]
    private async Task StopAsync(CancellationToken cancellationToken)
    {
        // If currently recording, save the final video.
        if (_currentSession is not null && IsRecording)
        {
            if (_currentSettings?.Camera.SourceKind == CameraSourceKind.Webcam)
            {
                await _cameraService.StopRecordingAsync(cancellationToken);
            }

            var completedSession = await _recordingService.StopAsync(_currentSession, cancellationToken);

            SessionArtifactPath = completedSession.VideoPath;
            SessionArtifactLabel = Path.GetFileName(completedSession.VideoPath);

            _appStatusService.SetStatus($"Session {completedSession.OrderCode} saved to {completedSession.VideoPath}.");
        }

        _recordingTimer.Stop();
        _currentSession = null;
        _cachedRecordingOptions = null;
        _activeBarcode = null;
        _isScanningBarcode = false;
        IsRecording = false;
        IsSessionActive = false;
        RecordingStatus = PreviewImage is null ? "Ready for validation" : "Live preview ready";
        CameraStatus = _currentSettings is null
            ? "Ready"
            : BuildCameraStatus(_currentSettings.Camera, PreviewImage is not null);

        _appStatusService.SetStatus("Session stopped.");
        PlaySessionStopSound();
    }

    [RelayCommand]
    private void LaunchCameraApp()
    {
        if (_desktopShellService.LaunchCameraApp())
        {
            _appStatusService.SetStatus("Windows Camera was launched as a fallback hardware check.");
            return;
        }

        _appStatusService.SetStatus("Windows Camera could not be launched from this machine.");
    }

    [RelayCommand]
    private void OpenSaveFolder()
    {
        var targetPath = _currentSettings?.SaveFolder;

        if (!string.IsNullOrWhiteSpace(targetPath) && _desktopShellService.OpenFolder(targetPath))
        {
            _appStatusService.SetStatus($"Opened save folder: {targetPath}");
            return;
        }

        _appStatusService.SetStatus("Could not open the local save folder.");
    }

    [RelayCommand]
    private void RevealArtifact()
    {
        if (!string.IsNullOrWhiteSpace(SessionArtifactPath) && _desktopShellService.RevealPath(SessionArtifactPath))
        {
            _appStatusService.SetStatus($"Opened the folder for {SessionArtifactLabel}.");
            return;
        }

        _appStatusService.SetStatus("No saved artifact is available yet.");
    }

    private bool CanStart() => !IsSessionActive;

    private bool CanStop() => IsSessionActive;

    private async Task ActivateAsync()
    {
        await LoadDashboardAsync(announceStatus: false);

        if (_currentSettings is not null)
        {
            await EnsurePreviewAsync(_currentSettings, CancellationToken.None);
        }
    }

    private async Task<ApplicationSettings?> EnsureSettingsAsync(CancellationToken cancellationToken)
    {
        if (_currentSettings is not null)
        {
            return _currentSettings;
        }

        await LoadDashboardAsync(announceStatus: false, cancellationToken);
        return _currentSettings;
    }

    private async Task LoadDashboardAsync(bool announceStatus, CancellationToken cancellationToken = default)
    {
        try
        {
            _currentSettings = await _settingsService.GetAsync(cancellationToken);

            Fps = _currentSettings.FramesPerSecond.ToString(CultureInfo.InvariantCulture);
            Resolution = _currentSettings.Resolution;
            SaveFolderSummary = _currentSettings.SaveFolder;
            CameraSourceSummary = DescribeCameraSource(_currentSettings.Camera);
            SessionArtifactLabel = string.IsNullOrWhiteSpace(SessionArtifactPath)
                ? "No capture artifact yet."
                : Path.GetFileName(SessionArtifactPath);

            if (!IsSessionActive)
            {
                CameraStatus = BuildCameraStatus(_currentSettings.Camera, PreviewImage is not null);
                RecordingStatus = PreviewImage is null ? "Connecting preview" : "Live preview ready";
                PreviewMessage = BuildIdlePreviewMessage(_currentSettings);
            }

            if (announceStatus)
            {
                _appStatusService.SetStatus("Home is ready for a local validation run.");
            }
        }
        catch (Exception exception)
        {
            CameraStatus = "Profile load failed";
            PreviewImage = null;
            PreviewMessage = "PackWatch could not load the local validation profile. Check the saved settings and try again.";
            _appStatusService.SetStatus($"Failed to load the local validation profile: {exception.Message}");
        }
    }

    private async Task EnsurePreviewAsync(ApplicationSettings settings, CancellationToken cancellationToken)
    {
        if (settings.Camera.SourceKind != CameraSourceKind.Webcam)
        {
            PreviewImage = null;
            PreviewMessage = settings.Camera.RtspUri is null
                ? "RTSP mode is selected, but the stream URL is still missing. Complete it in Settings or switch back to the laptop webcam."
                : $"RTSP source {settings.Camera.RtspUri.Host} is selected. Embedded live preview is wired for the shared webcam source first.";
            CameraStatus = BuildCameraStatus(settings.Camera, hasLivePreview: false);
            return;
        }

        try
        {
            PreviewImage = null;
            PreviewMessage = "Connecting to the selected webcam...";
            await _cameraService.OpenAsync(settings.Camera, cancellationToken);
            CameraStatus = BuildCameraStatus(settings.Camera, hasLivePreview: false);
            RecordingStatus = IsSessionActive ? RecordingStatus : "Waiting for first visible frame";
            _appStatusService.SetStatus($"Connected to webcam device {settings.Camera.DeviceIndex ?? 0}. Waiting for the first frame.");
        }
        catch (Exception exception)
        {
            PreviewImage = null;
            CameraStatus = "Live preview unavailable";
            RecordingStatus = "Preview unavailable";
            PreviewMessage = $"The selected webcam could not be opened. {exception.Message}";
            _appStatusService.SetStatus($"Failed to start the webcam preview: {exception.Message}");
        }
    }

    private void HandleFrameAvailable(object? sender, CameraFrameAvailableEventArgs e)
    {
        var bitmapSource = CreateBitmapSource(e.Frame);

        if (bitmapSource is null)
        {
            return;
        }

        var app = System.Windows.Application.Current;
        if (app is null)
        {
            return;
        }

        _ = app.Dispatcher.InvokeAsync(() =>
        {
            PreviewImage = bitmapSource;
            CameraStatus = _currentSettings is null
                ? "Live preview active"
                : BuildCameraStatus(_currentSettings.Camera, hasLivePreview: true);

            if (!IsSessionActive)
            {
                RecordingStatus = "Live preview ready";
            }

            PreviewMessage = "Live preview is active.";
        });

        // Run barcode detection when session is active.
        if (IsSessionActive && !_isScanningBarcode)
        {
            _ = ScanBarcodeFromFrameAsync(e.Frame);
        }
    }

    private async Task ScanBarcodeFromFrameAsync(AppVideoFrame frame)
    {
        if (_currentSettings is null || _isScanningBarcode)
        {
            return;
        }

        _isScanningBarcode = true;

        try
        {
            var options = new BarcodeDetectionOptions(
                _currentSettings.BarcodeRegionOfInterest,
                TimeSpan.FromMilliseconds(_currentSettings.BarcodeStableMilliseconds),
                TimeSpan.FromMilliseconds(200),
                _currentSettings.EnabledBarcodeFormats);

            var result = await _barcodeService.DetectAsync(frame, options, CancellationToken.None);

            if (result is null)
            {
                _isScanningBarcode = false;
                return;
            }

            var detectedValue = result.Value.Trim();

            if (string.IsNullOrWhiteSpace(detectedValue))
            {
                _isScanningBarcode = false;
                return;
            }

            // Same barcode as active — ignore.
            if (string.Equals(detectedValue, _activeBarcode, StringComparison.OrdinalIgnoreCase))
            {
                _isScanningBarcode = false;
                return;
            }

            // New barcode detected — handle on UI thread.
            var app = System.Windows.Application.Current;
            if (app is null)
            {
                _isScanningBarcode = false;
                return;
            }

            _ = app.Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    await HandleNewBarcodeDetectedAsync(detectedValue);
                }
                finally
                {
                    _isScanningBarcode = false;
                }
            });
        }
        catch
        {
            _isScanningBarcode = false;
        }
    }

    private async Task HandleNewBarcodeDetectedAsync(string barcodeValue)
    {
        if (!IsSessionActive || _cachedRecordingOptions is null)
        {
            return;
        }

        // Play alert sound.
        PlayBarcodeDetectedSound();

        // If already recording, stop and save the current recording first.
        if (IsRecording && _currentSession is not null)
        {
            if (_currentSettings?.Camera.SourceKind == CameraSourceKind.Webcam)
            {
                await _cameraService.StopRecordingAsync(CancellationToken.None);
            }

            var completedSession = await _recordingService.StopAsync(_currentSession, CancellationToken.None);

            SessionArtifactPath = completedSession.VideoPath;
            SessionArtifactLabel = Path.GetFileName(completedSession.VideoPath);

            _recordingTimer.Stop();
            _appStatusService.SetStatus($"Order {completedSession.OrderCode} saved. Switching to {barcodeValue}...");
        }

        // Start a new recording with the detected barcode as the order code.
        _currentSession = await _recordingService.StartAsync(
            barcodeValue,
            _cachedRecordingOptions,
            CancellationToken.None);

        if (_currentSettings?.Camera.SourceKind == CameraSourceKind.Webcam)
        {
            await _cameraService.StartRecordingAsync(_currentSession.VideoPath, CancellationToken.None);
        }

        _activeBarcode = barcodeValue;
        _barcodeDetectionCount++;
        BarcodeDetectionCountText = _barcodeDetectionCount.ToString(CultureInfo.InvariantCulture);
        IsRecording = true;
        CurrentOrder = barcodeValue;
        LastBarcode = barcodeValue;
        DetectedBarcodeInput = barcodeValue;
        RecordingTime = "00:00:00";
        RecordingStatus = "Recording session artifact";
        SessionArtifactPath = _currentSession.VideoPath;
        SessionArtifactLabel = Path.GetFileName(_currentSession.VideoPath);

        _recordingTimer.Start();
        _appStatusService.SetStatus($"Recording started for order {barcodeValue}.");
    }

    private static void PlaySessionStartSound()
    {
        try
        {
            Console.Beep(800, 150);
            Console.Beep(1200, 150);
        }
        catch
        {
            // Sound playback is optional.
        }
    }

    private static void PlaySessionStopSound()
    {
        try
        {
            Console.Beep(600, 150);
            Console.Beep(400, 200);
        }
        catch
        {
            // Sound playback is optional.
        }
    }

    private static void PlayBarcodeDetectedSound()
    {
        try
        {
            Console.Beep(1000, 200);
        }
        catch
        {
            // Sound playback is optional.
        }
    }

    private void HandleRecordingTimerTick(object? sender, EventArgs e)
    {
        if (_currentSession is null)
        {
            RecordingTime = "00:00:00";
            return;
        }

        var elapsed = DateTimeOffset.Now - _currentSession.StartedAt;
        RecordingTime = elapsed.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
    }

    private static ImageSource? CreateBitmapSource(AppVideoFrame frame)
    {
        try
        {
            var bitmap = BitmapSource.Create(
                frame.Width,
                frame.Height,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                frame.Buffer,
                frame.Width * 4);

            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private static string BuildCameraStatus(CameraConnectionOptions camera, bool hasLivePreview)
    {
        if (camera.SourceKind == CameraSourceKind.Webcam)
        {
            return hasLivePreview
                ? $"Live webcam preview is active on device {camera.DeviceIndex ?? 0}"
                : $"Laptop/local webcam is armed on device {camera.DeviceIndex ?? 0}";
        }

        return camera.RtspUri is null
            ? "RTSP profile needs a stream URL"
            : $"RTSP source selected: {camera.RtspUri.Host}";
    }

    private static string DescribeCameraSource(CameraConnectionOptions camera)
    {
        if (camera.SourceKind == CameraSourceKind.Webcam)
        {
            return camera.DeviceName ?? $"Laptop webcam (device {camera.DeviceIndex ?? 0})";
        }

        return camera.RtspUri is null
            ? "RTSP camera"
            : $"RTSP {camera.RtspUri.Host}:{camera.RtspUri.Port}";
    }

    private static string BuildIdlePreviewMessage(ApplicationSettings settings)
    {
        return settings.Camera.SourceKind == CameraSourceKind.Webcam
            ? "Connecting to the selected webcam. The first live frame should appear here directly inside PackWatch."
            : settings.Camera.RtspUri is null
                ? "RTSP mode is selected, but the stream URL is still missing. Complete it in Settings or switch back to the laptop webcam."
                : $"RTSP profile for {settings.Camera.RtspUri.Host} is ready. Embedded live preview is wired for the shared webcam source first.";
    }
}
