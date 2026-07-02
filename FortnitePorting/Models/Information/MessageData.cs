using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Material.Icons;

namespace FortnitePorting.Models.Information;
public partial class MessageData : ObservableObject
{
    [ObservableProperty] private string _title;
    [ObservableProperty] private string _message;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(SeverityIcon))] private InfoBarSeverity _severity;
    [ObservableProperty] private bool _autoClose;
    [ObservableProperty] private string _id;
    [ObservableProperty] private float _closeTime;

    [ObservableProperty] private bool _useButton;
    [ObservableProperty] private string _buttonTitle;
    [ObservableProperty] private RelayCommand _buttonCommand;

    [ObservableProperty] private bool _useProgress;
    [ObservableProperty] private int _progressCurrent;
    [ObservableProperty] private int _progressTotal;

    public MaterialIconKind SeverityIcon => Severity switch
    {
        InfoBarSeverity.Informational => MaterialIconKind.Information,
        InfoBarSeverity.Success => MaterialIconKind.CheckCircle,
        InfoBarSeverity.Warning => MaterialIconKind.AlertCircle,
        InfoBarSeverity.Error => MaterialIconKind.CloseCircle,
        _ => MaterialIconKind.Information
    };

    public MessageData(string title, string message, InfoBarSeverity severity = InfoBarSeverity.Informational, bool autoClose = true, string id = "", float closeTime = 2.0f,
        bool useButton = false, string buttonTitle = "", Action? buttonCommand = null,
        bool useProgress = false, int progressCurrent = 0, int progressTotal = 0)
    {
        Title = title;
        Message = message;
        Severity = severity;
        AutoClose = autoClose;
        Id = !string.IsNullOrEmpty(id) ? id : Guid.NewGuid().ToString();
        CloseTime = closeTime;
        UseButton = useButton;
        ButtonTitle = buttonTitle;
        ButtonCommand = new RelayCommand(buttonCommand ?? (() => { }));
        UseProgress = useProgress;
        ProgressCurrent = progressCurrent;
        ProgressTotal = progressTotal;
    }

    [RelayCommand]
    public async Task Close()
    {
        Info.CloseMessage(Id);
    }
}