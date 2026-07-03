using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PackWatch.App.Navigation;
using PackWatch.App.Services;
using PackWatch.Application.Abstractions.History;
using PackWatch.Application.Abstractions.Settings;
using PackWatch.Domain.Entities;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace PackWatch.App.ViewModels;

public sealed partial class HistoryPageViewModel : ObservableObject, INavigationAware
{
    private readonly IHistoryService _historyService;
    private readonly ISettingsService _settingsService;
    private readonly IDesktopShellService _desktopShellService;
    private readonly IAppStatusService _appStatusService;

    public ObservableCollection<HistoryRecordItem> Records { get; } = [];

    public IReadOnlyList<string> DateFilterOptions { get; } =
    [
        "Today",
        "Last 3 days",
        "Last 7 days",
        "All time"
    ];

    [ObservableProperty]
    private string selectedDateFilter = "Last 7 days";

    [ObservableProperty]
    private string searchOrderCode = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRecords))]
    [NotifyPropertyChangedFor(nameof(ResultsVisibility))]
    [NotifyPropertyChangedFor(nameof(EmptyStateVisibility))]
    [NotifyPropertyChangedFor(nameof(ResultsSummary))]
    private int resultCount;

    [ObservableProperty]
    private string emptyStateMessage = "No session artifacts yet. Start a validation run on Home to create the first record.";

    public HistoryPageViewModel(
        IHistoryService historyService,
        ISettingsService settingsService,
        IDesktopShellService desktopShellService,
        IAppStatusService appStatusService)
    {
        _historyService = historyService;
        _settingsService = settingsService;
        _desktopShellService = desktopShellService;
        _appStatusService = appStatusService;
    }

    public bool HasRecords => ResultCount > 0;

    public Visibility ResultsVisibility => HasRecords ? Visibility.Visible : Visibility.Collapsed;

    public Visibility EmptyStateVisibility => HasRecords ? Visibility.Collapsed : Visibility.Visible;

    public string ResultsSummary => ResultCount == 0
        ? "No local session artifacts yet"
        : $"{ResultCount} session artifact{(ResultCount == 1 ? string.Empty : "s")} found";

    public void OnNavigatedTo()
    {
        _ = RefreshAsync(announceStatus: false);
    }

    partial void OnSelectedDateFilterChanged(string value)
    {
        _ = RefreshAsync(announceStatus: false);
    }

    partial void OnSearchOrderCodeChanged(string value)
    {
        _ = RefreshAsync(announceStatus: false);
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        await RefreshAsync(announceStatus: true, cancellationToken);
    }

    [RelayCommand]
    private async Task OpenFolderAsync(CancellationToken cancellationToken)
    {
        var settings = await _settingsService.GetAsync(cancellationToken);

        if (_desktopShellService.OpenFolder(settings.SaveFolder))
        {
            _appStatusService.SetStatus($"Opened the local session folder: {settings.SaveFolder}");
            return;
        }

        _appStatusService.SetStatus("Could not open the local session folder.");
    }

    [RelayCommand]
    private void RevealArtifact(HistoryRecordItem? item)
    {
        if (item is null)
        {
            _appStatusService.SetStatus("Choose a history row before opening its artifact.");
            return;
        }

        if (_desktopShellService.RevealPath(item.ArtifactPath))
        {
            _appStatusService.SetStatus($"Opened the folder for {item.ArtifactName}.");
            return;
        }

        _appStatusService.SetStatus("Could not open the selected artifact.");
    }

    private async Task RefreshAsync(bool announceStatus, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = BuildQuery();
            var records = await _historyService.SearchAsync(query, cancellationToken);

            Records.Clear();

            foreach (var record in records.Select(MapToViewModel))
            {
                Records.Add(record);
            }

            ResultCount = Records.Count;
            EmptyStateMessage = HasRecords
                ? string.Empty
                : "No matching validation sessions were found. Try a wider date filter or start a new run from Home.";

            if (announceStatus)
            {
                _appStatusService.SetStatus(ResultCount == 0
                    ? "History is empty for the current filter."
                    : $"Loaded {ResultCount} session artifacts from the local history.");
            }
        }
        catch (Exception exception)
        {
            Records.Clear();
            ResultCount = 0;
            EmptyStateMessage = "History could not be loaded from local storage.";
            _appStatusService.SetStatus($"Failed to load history: {exception.Message}");
        }
    }

    private OrderHistoryQuery BuildQuery()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        return SelectedDateFilter switch
        {
            "Today" => new OrderHistoryQuery(SearchOrderCode, today, today),
            "Last 3 days" => new OrderHistoryQuery(SearchOrderCode, today.AddDays(-2), today),
            "Last 7 days" => new OrderHistoryQuery(SearchOrderCode, today.AddDays(-6), today),
            _ => new OrderHistoryQuery(SearchOrderCode, null, null)
        };
    }

    private static HistoryRecordItem MapToViewModel(OrderRecord record)
    {
        return new HistoryRecordItem
        {
            OrderCode = record.OrderCode,
            BadgeText = BuildBadge(record.OrderCode),
            CameraName = record.CameraName,
            StartedAtText = record.StartTime.LocalDateTime.ToString("dd MMM yyyy HH:mm"),
            EndedAtText = record.EndTime?.LocalDateTime.ToString("dd MMM yyyy HH:mm") ?? "-",
            DurationText = record.Duration?.ToString(@"hh\:mm\:ss") ?? "-",
            ArtifactName = Path.GetFileName(record.VideoPath),
            ArtifactPath = record.VideoPath
        };
    }

    private static string BuildBadge(string orderCode)
    {
        var trimmed = orderCode.Trim().ToUpperInvariant();

        if (trimmed.Length <= 4)
        {
            return trimmed;
        }

        return trimmed[^4..];
    }
}
