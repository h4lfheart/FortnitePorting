using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FortnitePorting.Models.Information;

public partial class DialogData : ObservableObject
{
    [ObservableProperty] private bool _isOpen;
    [ObservableProperty] private string _title;
    [ObservableProperty] private string? _message;
    [ObservableProperty] private object? _content;
    [ObservableProperty] private ObservableCollection<DialogButton> _buttons = [];
    [ObservableProperty] private bool _canClose;

    [RelayCommand]
    public async Task Close()
    {
        await Info.DialogQueue.Close();
    }
}

public partial class DialogButton : ObservableObject
{
    [ObservableProperty] private string _text;
    [ObservableProperty] private Action _action;

    [RelayCommand]
    public async Task Execute()
    {
        Action();
        await Info.DialogQueue.Close();
    }
}