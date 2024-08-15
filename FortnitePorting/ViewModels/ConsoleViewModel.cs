using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Models.Serilog;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using Serilog.Core;
using Serilog.Events;

namespace FortnitePorting.ViewModels;

public partial class ConsoleViewModel : ViewModelBase, ILogEventSink
{
    [ObservableProperty] private ScrollViewer _scroll;

    [ObservableProperty] private ObservableCollection<FortnitePortingLogEvent> _logs = [];

    public override async Task Initialize()
    {
        Logs.CollectionChanged += (sender, args) =>
        {
            TaskService.RunDispatcher(() =>
            {
                var isScrolledToEnd = Math.Abs(Scroll.Offset.Y - Scroll.Extent.Height + Scroll.Viewport.Height) < 500;
                if (isScrolledToEnd)
                    Scroll.ScrollToEnd();
            });
        };
    }

    public override async Task OnViewOpened()
    {
        Scroll.ScrollToEnd();
    }

    public void Emit(LogEvent logEvent)
    {
        Logs.Add(new FortnitePortingLogEvent(logEvent));
    }
}
