namespace PackWatch.Persistence.Services;

internal static class LocalPackWatchPaths
{
    private static readonly string RootDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PackWatch");

    public static string StateDirectory => Ensure(Path.Combine(RootDirectory, "State"));

    public static string SessionDirectory => Ensure(Path.Combine(RootDirectory, "Sessions"));

    public static string SettingsFilePath => Path.Combine(StateDirectory, "settings.json");

    public static string HistoryFilePath => Path.Combine(StateDirectory, "history.json");

    private static string Ensure(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
