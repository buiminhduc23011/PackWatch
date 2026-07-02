namespace PackWatch.Application.Abstractions.Media;

public readonly record struct FrameRegion(int X, int Y, int Width, int Height)
{
    public bool IsEmpty => Width <= 0 || Height <= 0;
}
