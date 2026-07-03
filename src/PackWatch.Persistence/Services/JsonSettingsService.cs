using PackWatch.Application.Abstractions.Barcodes;
using PackWatch.Application.Abstractions.Cameras;
using PackWatch.Application.Abstractions.Media;
using PackWatch.Application.Abstractions.Settings;
using System.Text.Json;

namespace PackWatch.Persistence.Services;

internal sealed class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<ApplicationSettings> GetAsync(CancellationToken cancellationToken)
    {
        var settingsFilePath = LocalPackWatchPaths.SettingsFilePath;

        if (!File.Exists(settingsFilePath))
        {
            var defaults = CreateDefaultSettings();
            await SaveAsync(defaults, cancellationToken);
            return defaults;
        }

        await using var stream = File.OpenRead(settingsFilePath);
        var settings = await JsonSerializer.DeserializeAsync<ApplicationSettings>(
            stream,
            SerializerOptions,
            cancellationToken);

        return settings ?? CreateDefaultSettings();
    }

    public async Task SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Directory.CreateDirectory(settings.SaveFolder);

        var settingsFilePath = LocalPackWatchPaths.SettingsFilePath;
        await using var stream = File.Create(settingsFilePath);
        await JsonSerializer.SerializeAsync(stream, settings, SerializerOptions, cancellationToken);
    }

    private static ApplicationSettings CreateDefaultSettings()
    {
        return new ApplicationSettings(
            new CameraConnectionOptions(
                CameraSourceKind.Webcam,
                DeviceName: "Laptop webcam (device 0)",
                DeviceIndex: 0,
                RtspUri: null,
                Username: null,
                Password: null),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "PackWatch"),
            FramesPerSecond: 30,
            Resolution: "1920x1080",
            BitrateKbps: 6000,
            VideoFormat: "mp4",
            RetentionDays: 90,
            BarcodeRegionOfInterest: new FrameRegion(120, 120, 720, 260),
            BarcodeStableMilliseconds: 750,
            EnabledBarcodeFormats:
            [
                BarcodeFormatKind.QrCode,
                BarcodeFormatKind.Code128,
                BarcodeFormatKind.Code39,
                BarcodeFormatKind.Ean13
            ],
            LoggingLevel: "Information");
    }
}
