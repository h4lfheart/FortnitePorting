using FortnitePorting.Framework.Extensions;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

namespace FortnitePorting.Framework.ViewModels;

public abstract class EndpointViewModelBase : ViewModelBase
{
    protected readonly RestClient Client;

    public EndpointViewModelBase(string userAgent = "FortnitePorting")
    {
        Client = new RestClient(new RestClientOptions
        {
            UserAgent = userAgent,
            MaxTimeout = 1000 * 10
        }, configureSerialization: s => s.UseSerializer<JsonNetSerializer>());
    }


    public byte[] DownloadFile(string url)
    {
        return DownloadFileAsync(url).GetAwaiter().GetResult();
    }

    public async Task<byte[]> DownloadFileAsync(string url)
    {
        var request = new RestRequest(url);
        var data = await Client.DownloadDataAsync(request);
        return data;
    }

    public FileInfo DownloadFile(string url, string destination)
    {
        return DownloadFileAsync(url, destination).GetAwaiter().GetResult();
    }

    public async Task<FileInfo> DownloadFileAsync(string url, string destination)
    {
        var request = new RestRequest(url);
        var data = await Client.DownloadDataAsync(request);
        if (data is not null) await File.WriteAllBytesAsync(destination, data);
        return new FileInfo(destination);
    }

    public FileInfo DownloadFile(string url, DirectoryInfo destination)
    {
        return DownloadFileAsync(url, destination).GetAwaiter().GetResult();
    }

    public async Task<FileInfo> DownloadFileAsync(string url, DirectoryInfo destination)
    {
        var outPath = Path.Combine(destination.FullName, url.SubstringAfterLast("/").SubstringAfterLast("\\"));
        var request = new RestRequest(url);
        var data = await Client.DownloadDataAsync(request);
        if (data is not null) await File.WriteAllBytesAsync(outPath, data);
        return new FileInfo(outPath);
    }
}