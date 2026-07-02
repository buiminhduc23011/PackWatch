namespace PackWatch.Application.Abstractions.Logging;

public interface ILogService
{
    void Information(string messageTemplate, params object[] propertyValues);

    void Warning(Exception? exception, string messageTemplate, params object[] propertyValues);

    void Error(Exception exception, string messageTemplate, params object[] propertyValues);
}
