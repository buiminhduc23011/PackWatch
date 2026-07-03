namespace PackWatch.App.Services;

public sealed class AppStatusService : IAppStatusService
{
    private string _currentStatus = "Ready for local validation.";

    public event EventHandler<string>? StatusChanged;

    public string CurrentStatus => _currentStatus;

    public void SetStatus(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        _currentStatus = message.Trim();
        StatusChanged?.Invoke(this, _currentStatus);
    }
}
