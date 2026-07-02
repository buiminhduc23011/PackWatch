using PackWatch.Application.Abstractions.Media;

namespace PackWatch.Application.Abstractions.Barcodes;

public interface IBarcodeService
{
    Task<BarcodeDetectionResult?> DetectAsync(
        VideoFrame frame,
        BarcodeDetectionOptions options,
        CancellationToken cancellationToken);
}
