using PackWatch.Application.Abstractions.Barcodes;
using PackWatch.Application.Abstractions.Cameras;
using PackWatch.Application.Abstractions.Media;

namespace PackWatch.Application.Abstractions.Settings;

public sealed record ApplicationSettings(
    CameraConnectionOptions Camera,
    string SaveFolder,
    int FramesPerSecond,
    string Resolution,
    int BitrateKbps,
    string VideoFormat,
    int RetentionDays,
    FrameRegion BarcodeRegionOfInterest,
    int BarcodeStableMilliseconds,
    IReadOnlyCollection<BarcodeFormatKind> EnabledBarcodeFormats,
    string LoggingLevel);
