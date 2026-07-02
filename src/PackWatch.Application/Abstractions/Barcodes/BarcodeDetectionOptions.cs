using PackWatch.Application.Abstractions.Media;

namespace PackWatch.Application.Abstractions.Barcodes;

public sealed record BarcodeDetectionOptions(
    FrameRegion RegionOfInterest,
    TimeSpan StableTime,
    TimeSpan ScanInterval,
    IReadOnlyCollection<BarcodeFormatKind> EnabledFormats);
