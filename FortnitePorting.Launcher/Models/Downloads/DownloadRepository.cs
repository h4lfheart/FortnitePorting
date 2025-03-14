using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AsyncImageLoader;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Launcher.Application;
using FortnitePorting.Launcher.Models.API.Response;
using Newtonsoft.Json;

namespace FortnitePorting.Launcher.Models.Downloads;

public partial class DownloadRepository : ObservableObject
{
    [ObservableProperty] private string _id;
    [ObservableProperty] private string _title;
    [ObservableProperty] private string _description;
    [ObservableProperty] private string? _iconUrl;
    [ObservableProperty] private string _repositoryUrl;

    [ObservableProperty] private ObservableCollection<DownloadVersion> _versions = [];
    
    [ObservableProperty] private bool _isFilterEnabled = true;
    
    [JsonIgnore] public Task<Bitmap?> IconImage => ImageLoader.AsyncImageLoader.ProvideImageAsync(IconUrl);


    public DownloadRepository(RepositoryResponse response, string repositoryUrl)
    {
        RepositoryUrl = repositoryUrl;
        SetPropertiesFrom(response);
    }

    public void SetPropertiesFrom(RepositoryResponse response)
    {
        Id = response.Id;
        Title = response.Title;
        Description = response.Description;
        IconUrl = response.Icon;
        Versions = [..response.Versions.Select(version => new DownloadVersion
        {
            ParentRepository = this,
            Version = version.Version,
            ExecutableUrl = version.ExecutableURL,
            UploadTime = version.UploadTime
        })];
    }

    public async Task Refresh()
    {
        if (await ApiVM.FortnitePorting.GetRepositoryAsync(RepositoryUrl) is not { } response) return;

        SetPropertiesFrom(response);
    }
}