using CommunityToolkit.Mvvm.ComponentModel;

namespace PackWatch.App.ViewModels;

public sealed partial class SettingsPageViewModel : ObservableObject
{
    [ObservableProperty]
    private string cameraMode = "Webcam";

    [ObservableProperty]
    private string saveFolder = "Configured in later phase";

    [ObservableProperty]
    private int stableBarcodeMilliseconds = 750;

    [ObservableProperty]
    private string loggingLevel = "Information";
}
