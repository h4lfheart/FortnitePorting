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
    
    public bool ShowCloseButton => CanClose && Buttons.Count == 0;

    [RelayCommand]
    public async Task Close()
    {
        await Info.DialogQueue.Close();
    }

    partial void OnButtonsChanged(ObservableCollection<DialogButton>? oldValue, ObservableCollection<DialogButton>? newValue)
    {
        OnPropertyChanged(nameof(OnPropertyChanged));
    }
}

public partial class DialogButton : ObservableObject
{
    [ObservableProperty] private string _text;
    [ObservableProperty] private Action? _action;
    [ObservableProperty] private bool _isPrimary;

    [RelayCommand]
    public async Task Execute()
    {
        Action?.Invoke();
        await Info.DialogQueue.Close();
    }
}