using System.IO;
using System.Threading.Tasks;
using CUE4Parse.Utils;
using FortnitePorting.Installer.Models.API;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.ViewModels;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

namespace FortnitePorting.Installer.ViewModels;

public class APIViewModel : APIViewModelBase
{
    public readonly FortnitePortingAPI FortnitePorting;

    public APIViewModel()
    {
        FortnitePorting = new FortnitePortingAPI(_client);
    }
}