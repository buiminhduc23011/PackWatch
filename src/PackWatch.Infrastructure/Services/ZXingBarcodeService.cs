using PackWatch.Application.Abstractions.Barcodes;
using PackWatch.Application.Abstractions.Media;
using ZXing;
using ZXing.Common;

namespace PackWatch.Infrastructure.Services;

internal sealed class ZXingBarcodeService : IBarcodeService
{
    public Task<BarcodeDetectionResult?> DetectAsync(
        VideoFrame frame,
        BarcodeDetectionOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(options);

        // Copy buffer to avoid issues if the source frame is reused.
        var bufferCopy = frame.Buffer.ToArray();
        var width = frame.Width;
        var height = frame.Height;

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Build luminance source directly from the BGRA pixel buffer — no System.Drawing needed.
            var luminanceSource = new RGBLuminanceSource(bufferCopy, width, height, RGBLuminanceSource.BitmapFormat.BGRA32);

            var reader = new BarcodeReaderGeneric
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    TryInverted = true,
                    PossibleFormats = [BarcodeFormat.QR_CODE]
                }
            };

            var result = reader.Decode(luminanceSource);

            if (result is null)
            {
                return null;
            }

            return new BarcodeDetectionResult(
                result.Text,
                BarcodeFormatKind.QrCode,
                MapBounds(result.ResultPoints, width, height),
                DateTimeOffset.Now);
        }, cancellationToken);
    }

    private static FrameRegion MapBounds(ResultPoint[]? points, int frameWidth, int frameHeight)
    {
        if (points is null || points.Length == 0)
        {
            return new FrameRegion(0, 0, frameWidth, frameHeight);
        }

        var minX = (int)points.Min(p => p.X);
        var minY = (int)points.Min(p => p.Y);
        var maxX = (int)points.Max(p => p.X);
        var maxY = (int)points.Max(p => p.Y);

        return new FrameRegion(minX, minY, maxX - minX, maxY - minY);
    }
}
