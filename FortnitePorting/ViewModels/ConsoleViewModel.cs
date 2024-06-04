using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Models.Serilog;
using FortnitePorting.Shared.Services;

namespace FortnitePorting.ViewModels;

public partial class ConsoleViewModel : ViewModelBase
{
    [ObservableProperty] private ScrollViewer _scroll;
    
    public ObservableCollection<FortnitePortingLogEvent> Logs => FortnitePortingSink.Logs;

    public ConsoleViewModel()
    {
        FortnitePortingSink.Logs.CollectionChanged += (sender, args) =>
        {
            TaskService.RunDispatcher(() =>
            {
                var isScrolledToEnd = Math.Abs(Scroll.Offset.Y - Scroll.Extent.Height + Scroll.Viewport.Height) < 100;
                if (isScrolledToEnd)
                    Scroll.ScrollToEnd();
            });
        };
    }
}
