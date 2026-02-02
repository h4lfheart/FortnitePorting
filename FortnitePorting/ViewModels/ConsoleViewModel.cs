using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Controls;
using FortnitePorting.Framework;
using FortnitePorting.Models.Article;
using FortnitePorting.Models.Information;
using FortnitePorting.Models.Supabase.Tables;

using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using ReactiveUI;
using Serilog;

namespace FortnitePorting.ViewModels;

public partial class ConsoleViewModel : ViewModelBase
{
    [ObservableProperty] private InfoService _info;

    [ObservableProperty] private ReadOnlyObservableCollection<FPLogEvent> _logs = new([]);

    [ObservableProperty] private string _searchFilter = string.Empty;
    [ObservableProperty] private ELogEventType _filterType = ELogEventType.None;

    public DynamicFadeScrollViewer Scroll;
    
    private const double AutoScrollThreshold = 400;

    public ConsoleViewModel(InfoService infoService)
    {
        Info = infoService;
    }

    public override async Task Initialize()
    {
        
        var filterObservable = this
            .WhenAnyValue(vm => vm.SearchFilter, vm => vm.FilterType)
            .Select(CreateFilter);
        
        Info.LogList.Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Buffer(TimeSpan.FromMilliseconds(100)) // Batch changes instead of throttling
            .Where(x => x.Count > 0) // Skip empty batches
            .FlattenBufferResult()
            .Filter(filterObservable)
            .Bind(out var readOnlyLogs)
            .Subscribe(_ => AutoScrollIfNeeded()); 

        Logs = readOnlyLogs;
    }

    [RelayCommand]
    private async Task OpenLog()
    {
        App.LaunchSelected(Info.LogFilePath);
    }

    private void AutoScrollIfNeeded()
    {
        TaskService.RunDispatcher(() =>
        {
            var distanceFromBottom = Scroll.Extent.Height - Scroll.Viewport.Height - Scroll.Offset.Y;
            if (distanceFromBottom <= AutoScrollThreshold)
                Scroll.ScrollToEnd();
        });
    }

    public override async Task OnViewOpened()
    {
        Scroll.ScrollToEnd();
    }

    private Func<FPLogEvent, bool> CreateFilter((string, ELogEventType) items)
    {
        return logEvent =>
        {
            var searchFilter = items.Item1;
            var eventType = items.Item2;
            return MiscExtensions.Filter(logEvent.Message, searchFilter) &&
                   (eventType == ELogEventType.None || logEvent.Level == eventType);
        };
    }
}
