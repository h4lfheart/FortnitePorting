using System;
using System.Windows;
using System.Windows.Threading;
using FortnitePorting.AppUtils;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class WrappedView
{
    public DispatcherTimer Timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private TimeSpan Elapsed;
    
    public WrappedView()
    {
        InitializeComponent();
        AppVM.WrappedVM = new WrappedViewModel();
        DataContext = AppVM.WrappedVM;
        
        Elapsed = AppSettings.Current.WrappedData.TimeSpentOpen + (DateTime.Now - AppSettings.Current.WrappedData.InstanceStart);
        AppVM.WrappedVM.TimeString = $"{Elapsed.Days}d {Elapsed.Hours}h {Elapsed.Minutes}m {Elapsed.Seconds}s";

        Timer.Tick += (sender, args) =>
        {
            Elapsed = AppSettings.Current.WrappedData.TimeSpentOpen + (DateTime.Now - AppSettings.Current.WrappedData.InstanceStart);
            AppVM.WrappedVM.TimeString = $"{Elapsed.Days}d {Elapsed.Hours}h {Elapsed.Minutes}m {Elapsed.Seconds}s";
        };
        Timer.Start();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await AppVM.WrappedVM.Initialize();
    }
}