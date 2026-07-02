using PackWatch.Application.Abstractions.Media;

namespace PackWatch.Application.Abstractions.Cameras;

public sealed class CameraFrameAvailableEventArgs : EventArgs
{
    public CameraFrameAvailableEventArgs(VideoFrame frame)
    {
        Frame = frame;
    }

    public VideoFrame Frame { get; }
}
