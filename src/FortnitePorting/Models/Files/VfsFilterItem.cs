using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FortnitePorting.Models.Files;

public partial class VfsFilterItem(string vfsName) : ObservableObject
{
    [ObservableProperty] private bool _isChecked = false;

    public string VfsName { get; } = vfsName;

    public bool HasOptionalContent =>
        vfsName.Contains(".o")
        || vfsName.Contains("UEFN", StringComparison.OrdinalIgnoreCase);

    public string Group => HasOptionalContent ? "UEFN" : "Fortnite";
}
