using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.Core;
using ScottPlot;
using ScottPlot.Avalonia;
using ScottPlot.Plottables;
using ScottPlot.Rendering.RenderActions;

namespace FortnitePorting.Models.Leaderboard;

public partial class StatisticsModel : ObservableObject
{
    [ObservableProperty] private string _name;

    [ObservableProperty] private AvaPlot _graph;
    [ObservableProperty] private Scatter _scatter;
    [ObservableProperty] private Crosshair _crosshair;
    
    [ObservableProperty] private int _totalExports;
    [ObservableProperty] private int _instanceExports;
    [ObservableProperty] private int _uniqueExports;
    [ObservableProperty] private int _maximumExports;
    [ObservableProperty] private ObservableCollection<PersonalExport> _exports;

    public StatisticsModel(string name, TimeSpan delta, int dataCount, ObservableCollection<PersonalExport> sourceExports)
    {
        Name = name;
        
        var dataCutoffTime = DateTime.UtcNow - delta * dataCount;
        Exports = [..sourceExports.Where(export => export.TimeExported >= dataCutoffTime)];
        
        Graph = new AvaPlot();
        Graph.Plot.RenderManager.RenderActions.RemoveAll(action => action is ClearCanvas); // fix avalonia bg rendering bug
        Graph.Plot.FigureBackground.Color = Colors.Transparent;
        Graph.Plot.DataBackground.Color = Colors.Transparent;
        Graph.Plot.Axes.DateTimeTicksBottom();
        Graph.Plot.Layout.Frameless();
        Graph.Plot.HideGrid();
        Graph.Interaction.Disable();
        
        var startDateTime = DateTime.UtcNow - delta * (dataCount - 1);
        var times = new DateTime[dataCount];
        var values = new int[dataCount];
        for (var i = 0; i < dataCount; i++)
        {
            var time = startDateTime + i * delta;
            times[i] = time;
            values[i] = Exports.Count(export => IsValidExport(export.TimeExported, time, delta));
        }
        
        TotalExports = Exports.Count;
        InstanceExports = Exports.Select(export => export.InstanceGuid).Distinct().Count();
        MaximumExports = values.Max();
        UniqueExports = Exports.Select(export => export.ObjectPath).Distinct().Count();
        
        Graph.Plot.Axes.Left.Min = -25;
        Graph.Plot.Axes.Left.Max = MaximumExports + 25;
        
        Scatter = Graph.Plot.Add.Scatter(times, values);
        Scatter.Color = Color.FromHex("#bf57c9");
        Scatter.FillY = true;
        Scatter.FillYColor = Scatter.Color.WithAlpha(.2);

        Crosshair = Graph.Plot.Add.Crosshair(times.Min().ToOADate(), 0);
        Crosshair.IsVisible = false;
        Crosshair.LineColor = Color.FromHex("#bf57c9").WithAlpha(0.9f);
        Crosshair.VerticalLine.IsVisible = false;

    }

    public bool IsValidExport(DateTime exportTime, DateTime targetTime, TimeSpan deltaTime)
    {
        var matchesDay = exportTime.Date.Equals(targetTime.Date);
        var matchesHour = deltaTime <= TimeSpan.FromHours(1) ? exportTime.Hour.Equals(targetTime.Hour) : true;
        return matchesDay && matchesHour;
    }
}