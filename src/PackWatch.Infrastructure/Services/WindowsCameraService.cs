using PackWatch.Application.Abstractions.Cameras;
using AppVideoFrame = PackWatch.Application.Abstractions.Media.VideoFrame;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;

namespace PackWatch.Infrastructure.Services;

internal sealed class WindowsCameraService : ICameraService
{
    private static readonly TimeSpan PreviewThrottle = TimeSpan.FromMilliseconds(120);

    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private readonly object _frameSync = new();

    private MediaCapture? _mediaCapture;
    private MediaFrameReader? _frameReader;
    private CameraConnectionOptions? _currentOptions;
    private AppVideoFrame? _lastFrame;
    private DateTimeOffset _lastPreviewFrameAt = DateTimeOffset.MinValue;
    private bool _isRecording;

    public event EventHandler<CameraFrameAvailableEventArgs>? FrameAvailable;

    public bool IsOpen => _mediaCapture is not null;

    public async Task OpenAsync(CameraConnectionOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        await _lifecycleGate.WaitAsync(cancellationToken);

        try
        {
            await CloseCoreAsync(cancellationToken);

            if (options.SourceKind != CameraSourceKind.Webcam)
            {
                throw new NotSupportedException("Embedded preview currently supports the shared webcam source. RTSP can still be validated from Settings.");
            }

            var device = await ResolveVideoDeviceAsync(options.DeviceIndex ?? 0, cancellationToken);
            var mediaCapture = new MediaCapture();
            var initializationSettings = new MediaCaptureInitializationSettings
            {
                VideoDeviceId = device.Id,
                StreamingCaptureMode = StreamingCaptureMode.Video,
                SharingMode = MediaCaptureSharingMode.SharedReadOnly,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu
            };

            await mediaCapture.InitializeAsync(initializationSettings).AsTask(cancellationToken);

            var frameSource = SelectColorSource(mediaCapture);
            var frameReader = await mediaCapture
                .CreateFrameReaderAsync(frameSource, MediaEncodingSubtypes.Bgra8)
                .AsTask(cancellationToken);

            frameReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;
            frameReader.FrameArrived += HandleFrameArrived;

            var startStatus = await frameReader.StartAsync().AsTask(cancellationToken);

            if (startStatus != MediaFrameReaderStartStatus.Success)
            {
                frameReader.FrameArrived -= HandleFrameArrived;
                frameReader.Dispose();
                mediaCapture.Dispose();
                throw new InvalidOperationException($"Windows could not start the selected webcam. MediaFrameReader returned {startStatus}.");
            }

            _mediaCapture = mediaCapture;
            _frameReader = frameReader;
            _currentOptions = options with { DeviceName = device.Name, DeviceIndex = options.DeviceIndex ?? 0 };
            _lastPreviewFrameAt = DateTimeOffset.MinValue;
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task CloseAsync(CancellationToken cancellationToken)
    {
        await _lifecycleGate.WaitAsync(cancellationToken);

        try
        {
            await CloseCoreAsync(cancellationToken);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task ReconnectAsync(CancellationToken cancellationToken)
    {
        var options = _currentOptions;

        if (options is null)
        {
            return;
        }

        await OpenAsync(options, cancellationToken);
    }

    public Task<AppVideoFrame?> SnapshotAsync(CancellationToken cancellationToken)
    {
        lock (_frameSync)
        {
            return Task.FromResult(CloneFrame(_lastFrame));
        }
    }

    public async Task StartRecordingAsync(string filePath, CancellationToken cancellationToken)
    {
        if (_mediaCapture is null)
        {
            throw new InvalidOperationException("The camera stream must be active to record a video.");
        }

        var folderPath = Path.GetDirectoryName(filePath);
        var fileName = Path.GetFileName(filePath);
        if (string.IsNullOrWhiteSpace(folderPath) || string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("A valid target video file path is required.", nameof(filePath));
        }

        Directory.CreateDirectory(folderPath);

        var folder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(folderPath);
        var storageFile = await folder.CreateFileAsync(fileName, Windows.Storage.CreationCollisionOption.ReplaceExisting);
        var profile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.HD720p);

        await _mediaCapture.StartRecordToStorageFileAsync(profile, storageFile).AsTask(cancellationToken);
        _isRecording = true;
    }

    public async Task StopRecordingAsync(CancellationToken cancellationToken)
    {
        if (_mediaCapture is null || !_isRecording)
        {
            return;
        }

        await _mediaCapture.StopRecordAsync().AsTask(cancellationToken);
        _isRecording = false;
    }

    private async void HandleFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        var now = DateTimeOffset.UtcNow;

        if (now - _lastPreviewFrameAt < PreviewThrottle)
        {
            return;
        }

        _lastPreviewFrameAt = now;

        using var mediaFrameReference = sender.TryAcquireLatestFrame();
        var softwareBitmap = mediaFrameReference?.VideoMediaFrame?.SoftwareBitmap;

        if (softwareBitmap is null)
        {
            return;
        }

        try
        {
            using var convertedBitmap = SoftwareBitmap.Convert(
                softwareBitmap,
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied);

            var buffer = new byte[convertedBitmap.PixelWidth * convertedBitmap.PixelHeight * 4];
            convertedBitmap.CopyToBuffer(buffer.AsBuffer());

            var previewFrame = new AppVideoFrame(
                buffer,
                convertedBitmap.PixelWidth,
                convertedBitmap.PixelHeight,
                DateTimeOffset.Now);

            lock (_frameSync)
            {
                _lastFrame = previewFrame;
            }

            FrameAvailable?.Invoke(this, new CameraFrameAvailableEventArgs(previewFrame));
        }
        catch
        {
            // The preview loop should stay alive even if one frame cannot be converted.
        }
    }

    private async Task CloseCoreAsync(CancellationToken cancellationToken)
    {
        if (_isRecording)
        {
            try
            {
                await StopRecordingAsync(cancellationToken);
            }
            catch
            {
                // Ignored
            }
        }

        if (_frameReader is not null)
        {
            _frameReader.FrameArrived -= HandleFrameArrived;

            try
            {
                await _frameReader.StopAsync().AsTask(cancellationToken);
            }
            catch
            {
                // Ignore shutdown failures while closing the camera.
            }

            _frameReader.Dispose();
            _frameReader = null;
        }

        _mediaCapture?.Dispose();
        _mediaCapture = null;

        lock (_frameSync)
        {
            _lastFrame = null;
        }
    }

    private static MediaFrameSource SelectColorSource(MediaCapture mediaCapture)
    {
        var preferredSource = mediaCapture.FrameSources.Values.FirstOrDefault(source =>
            source.Info.SourceKind == MediaFrameSourceKind.Color &&
            source.Info.MediaStreamType == MediaStreamType.VideoPreview);

        if (preferredSource is not null)
        {
            return preferredSource;
        }

        preferredSource = mediaCapture.FrameSources.Values.FirstOrDefault(source =>
            source.Info.SourceKind == MediaFrameSourceKind.Color &&
            source.Info.MediaStreamType == MediaStreamType.VideoRecord);

        return preferredSource
            ?? throw new InvalidOperationException("The selected webcam does not expose a color preview stream.");
    }

    private static async Task<DeviceInformation> ResolveVideoDeviceAsync(int preferredIndex, CancellationToken cancellationToken)
    {
        var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture).AsTask(cancellationToken);

        if (devices.Count == 0)
        {
            throw new InvalidOperationException("No webcam was detected on this machine.");
        }

        var safeIndex = Math.Clamp(preferredIndex, 0, devices.Count - 1);
        return devices[safeIndex];
    }

    private static AppVideoFrame? CloneFrame(AppVideoFrame? frame)
    {
        return frame is null
            ? null
            : frame with { Buffer = frame.Buffer.ToArray() };
    }
}
