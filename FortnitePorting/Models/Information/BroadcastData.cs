using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FortnitePorting.Models.Information;

public partial class BroadcastData : ObservableObject
{
    [ObservableProperty] private bool _isOpen;
    [ObservableProperty] private string _title;
    [ObservableProperty] private string _description;
    [ObservableProperty] private DateTime _timestamp;

    [RelayCommand]
    public async Task Close()
    {
        await Info.BroadcastQueue.Close();
    }
}