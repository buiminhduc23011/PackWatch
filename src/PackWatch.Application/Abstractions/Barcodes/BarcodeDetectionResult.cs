using PackWatch.Application.Abstractions.Media;

namespace PackWatch.Application.Abstractions.Barcodes;

public sealed record BarcodeDetectionResult(
    string Value,
    BarcodeFormatKind Format,
    FrameRegion Bounds,
    DateTimeOffset DetectedAt);
