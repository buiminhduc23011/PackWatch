using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PackWatch.App.ViewModels;

public sealed partial class HomePageViewModel : ObservableObject
{
    [ObservableProperty]
    private string cameraStatus = "Disconnected";

    [ObservableProperty]
    private string recordingStatus = "Idle";

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
    private string previewMessage = "Camera preview will be connected in Phase 3.";

    [RelayCommand]
    private Task StartAsync(CancellationToken cancellationToken)
    {
        CameraStatus = "Waiting for camera service";
        RecordingStatus = "Waiting for first barcode";
        PreviewMessage = "Phase 1 has wired the command path. Camera capture starts in Phase 3.";
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task StopAsync(CancellationToken cancellationToken)
    {
        RecordingStatus = "Idle";
        PreviewMessage = "Preview stopped.";
        return Task.CompletedTask;
    }
}
