using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace FortnitePorting.Models.Help;

public partial class HelpArticle : ObservableObject
{
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ProperDescription))] private string _description = string.Empty;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ProperDescription))] private string _author = "Anonymous";
    [ObservableProperty] private DateTime _postTime;
    [ObservableProperty] private EHelpTag _tag;
    [ObservableProperty] private ObservableCollection<HelpSection> _sections = [];

    [JsonIgnore] public string ProperDescription => $"{(Description[^1] == '.' ? Description : Description + ".")} Written by {Author}.";
}

public enum EHelpTag
{
    Application,
    Blender,
    Unreal,
    Other
}