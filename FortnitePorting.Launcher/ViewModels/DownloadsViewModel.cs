using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AvaloniaEdit.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using FortnitePorting.Launcher.Application;
using FortnitePorting.Launcher.Models.API.Response;
using FortnitePorting.Launcher.Models.Downloads;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Framework;
using ReactiveUI;
using Serilog;

namespace FortnitePorting.Launcher.ViewModels;

public partial class DownloadsViewModel : ViewModelBase
{
    [ObservableProperty] private string _searchFilter = string.Empty;
    [ObservableProperty] private ReadOnlyObservableCollection<DownloadVersion> _versions = new([]);
    [ObservableProperty] private ObservableCollection<DownloadRepository> _visibleRepositories = RepositoriesVM.Repositories;
    
    private readonly SourceList<DownloadVersion> DownloadVersions = new();

    public DownloadsViewModel()
    {
        var downloadFilter = this
            .WhenAnyValue(viewModel => viewModel.SearchFilter, viewModel => viewModel.VisibleRepositories)
            .Select(filterData =>
            {
                var (searchFilter, repositories) = filterData;
                return new Func<DownloadVersion, bool>(version => 
                    MiscExtensions.Filter(version.DisplayString, searchFilter)
                    && repositories.Any(repo => version.ParentRepository == repo && repo.IsFilterEnabled));
            });
        
        DownloadVersions.Connect()
            .Filter(downloadFilter)
            .Sort(SortExpressionComparer<DownloadVersion>.Descending(x => x.UploadTime))
            .Bind(out var versionCollection)
            .Subscribe();
        
        Versions = versionCollection;
    }

    public override async Task OnViewOpened()
    {
        OnPropertyChanged(nameof(VisibleRepositories));
        await Refresh();
    }

    [RelayCommand]
    public async Task Refresh()
    {
        DownloadVersions.Clear();
        
        foreach (var repository in RepositoriesVM.Repositories)
        {
            foreach (var version in repository.UnderlyingResponse.Versions)
            {
                var downloadVersion = new DownloadVersion
                {
                    ParentRepository = repository,
                    VersionString = version.VersionString,
                    ExecutableUrl = version.ExecutableURL,
                    UploadTime = version.UploadTime
                };
                
                if (downloadVersion.IsDownloaded 
                    && !AppSettings.Current.DownloadedVersions.Any(v => v.ExecutablePath.Equals(downloadVersion.ExecutableDownloadPath)))
                {
                    AppSettings.Current.DownloadedVersions.Add(downloadVersion.CreateInstallationVersion());
                }
                
                DownloadVersions.Add(downloadVersion);
            }
        }
    }
    
    public void FakeRefreshFilters()
    {
        var temp = VisibleRepositories;
        VisibleRepositories = [];
        VisibleRepositories = temp;
    }
}