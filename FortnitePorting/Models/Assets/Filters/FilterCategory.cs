using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Shared;

namespace FortnitePorting.Models.Assets.Filters;

public partial class FilterCategory(string title, List<EExportType>? allowedTypes = null) : ObservableObject
{
    [ObservableProperty] private string _title = title;
    [ObservableProperty] private ObservableCollection<FilterItem> _filters = [];
    [ObservableProperty] private bool _isVisible = true;

    public List<EExportType> AllowedTypes = allowedTypes ?? [];
}