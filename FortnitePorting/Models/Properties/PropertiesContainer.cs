using CommunityToolkit.Mvvm.ComponentModel;

namespace FortnitePorting.Models.Properties;

public partial class PropertiesContainer : ObservableObject
{
    [ObservableProperty] private string _assetName;
    [ObservableProperty] private string _propertiesData;

}