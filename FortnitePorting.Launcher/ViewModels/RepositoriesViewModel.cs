using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Launcher.Application;
using FortnitePorting.Launcher.Models.API.Response;
using FortnitePorting.Launcher.Models.Downloads;
using FortnitePorting.Launcher.Models.Repository;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Framework;

namespace FortnitePorting.Launcher.ViewModels;

public partial class RepositoriesViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<DownloadRepository> _repositories = [];

    public override async Task Initialize()
    {
        await Refresh();

        await ProfilesVM.UpdateRepositoryProfiles();
    }

    [RelayCommand]
    public async Task Refresh()
    {
        Repositories.Clear();
        
        foreach (var repositoryUrlContainer in AppSettings.Current.Repositories)
        {
            if (await ApiVM.FortnitePorting.GetRepositoryAsync(repositoryUrlContainer.RepositoryUrl) is not { } repositoryResponse) continue;

            Repositories.Add(new DownloadRepository(repositoryResponse, repositoryUrlContainer.RepositoryUrl));
        }
    }

    public async Task AddRepository()
    {
        var textBox = new TextBox
        {
            Watermark = "Repository URL"
        };
        
        var dialog = new ContentDialog
        {
            Title = "Add New Repository",
            Content = textBox,
            CloseButtonText = "Cancel",
            PrimaryButtonText = "Add",
            PrimaryButtonCommand = new RelayCommand(async () =>
            {
                var newUrl = textBox.Text;
                if (string.IsNullOrWhiteSpace(newUrl)) return;

                if (AppSettings.Current.Repositories.Any(repo => repo.RepositoryUrl.Equals(newUrl)))
                {
                    AppWM.Message("Repositories", $"A repository already exists with the url \"{newUrl}\"");
                    return;
                }
                
                AppSettings.Current.Repositories.Add(new RepositoryUrlContainer(newUrl));
                await Refresh();
                await DownloadsVM.Refresh();
            })
        };

        await dialog.ShowAsync();
    }
    
    public async Task Delete(DownloadRepository repository)
    {
        var dialog = new ContentDialog
        {
            Title = $"Remove \"{repository.Title}\"",
            Content = "Are you sure you want to remove this repository?",
            CloseButtonText = "No",
            PrimaryButtonText = "Yes",
            PrimaryButtonCommand = new RelayCommand(async () =>
            {
                AppSettings.Current.Repositories.RemoveAll(repo => repo.RepositoryUrl.Equals(repository.RepositoryUrl));
                await Refresh();
                await DownloadsVM.Refresh();
            })
        };

        await dialog.ShowAsync();
    }
    
}