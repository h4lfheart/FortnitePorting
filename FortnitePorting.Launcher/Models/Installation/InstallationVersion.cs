using CommunityToolkit.Mvvm.ComponentModel;

namespace FortnitePorting.Launcher.Models.Installation;

public partial class InstallationVersion : ObservableObject
{
    [ObservableProperty] private string _name;
    [ObservableProperty] private string _repositoryName;
    [ObservableProperty] private string _executablePath;

    public override string ToString()
    {
        return Name;
    }
}