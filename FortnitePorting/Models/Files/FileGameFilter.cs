using CommunityToolkit.Mvvm.ComponentModel;

namespace FortnitePorting.Models.Files;

public partial class FileGameFilter(string displayName, string? searchName = null) : ObservableObject
{
    [ObservableProperty] private string _displayName = displayName;
    [ObservableProperty] private string _searchName = searchName ?? displayName;
    [ObservableProperty] private bool _isChecked = true;
}