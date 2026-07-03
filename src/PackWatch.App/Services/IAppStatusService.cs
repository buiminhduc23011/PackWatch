namespace PackWatch.App.Services;

public interface IAppStatusService
{
    event EventHandler<string>? StatusChanged;

    string CurrentStatus { get; }

    void SetStatus(string message);
}
