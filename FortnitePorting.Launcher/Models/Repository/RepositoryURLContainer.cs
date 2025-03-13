using CommunityToolkit.Mvvm.ComponentModel;

namespace FortnitePorting.Launcher.Models.Repository;

public partial class RepositoryUrlContainer(string repositoryUrl = "") : ObservableObject
{
    [ObservableProperty] private string _repositoryUrl = repositoryUrl;
}