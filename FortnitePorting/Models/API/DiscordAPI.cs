using System.Net;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Services;
using Newtonsoft.Json;
using RestSharp;

namespace FortnitePorting.Models.API;

public class DiscordAPI : APIBase
{
    public const string IDENTIFICATION_URL = "https://discordapp.com/api/users/@me";
    public const string TEST_OAUTH_URL = "https://discordapp.com/api/users/@me";
    
    public DiscordAPI(RestClient client) : base(client)
    {
    }
    
    public async Task<IdentificationResponse?> GetIdentificationAsync()
    {
        var response = await ExecuteAsync(IDENTIFICATION_URL, 
            parameters: new HeaderParameter("Authorization", $"Bearer {AppSettings.Current.Discord.Auth.AccessToken}"));
        
        return response.StatusCode == HttpStatusCode.OK ? JsonConvert.DeserializeObject<IdentificationResponse>(response.Content) : null;
        
    }

    public IdentificationResponse? GetIdentification()
    {
        return GetIdentificationAsync().GetAwaiter().GetResult();
    }

    public async Task CheckAuthRefresh()
    {
        var response = await ExecuteAsync(TEST_OAUTH_URL, 
            parameters: new HeaderParameter("Authorization", $"Bearer {AppSettings.Current.Discord.Auth.AccessToken}"));
        
        if (response.StatusCode != HttpStatusCode.OK)
        {
            await TaskService.RunDispatcherAsync(async () =>
            {
                var discordIntegrationDialog = new ContentDialog
                {
                    Title = "Discord Integration",
                    Content = "Your discord authentication has expired. Please re-authenticate to continue using FortnitePorting's online features.",
                    CloseButtonText = "Ignore",
                    PrimaryButtonText = "Re-Authenticate",
                    PrimaryButtonCommand = new RelayCommand(async () => await AppSettings.Current.Discord.Authenticate())
                };

                await discordIntegrationDialog.ShowAsync();
            });
        }
    }
}