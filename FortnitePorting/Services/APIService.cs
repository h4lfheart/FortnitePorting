using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FortnitePorting.Models.API;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;
using Serilog;

namespace FortnitePorting.Services;

public class APIService : IService
{
    public readonly FortnitePortingAPI FortnitePorting;
    public readonly FortnitePortingServerAPI FortnitePortingServer;
    public readonly EpicGamesAPI EpicGames;
    
    public APIService()
    {
        _client = new RestClient(new RestClientOptions
        {
            UserAgent = $"FortnitePorting/{Globals.VersionString}",
            MaxTimeout = 1000 * AppSettings.Debug.RequestTimeoutSeconds,
        }, configureSerialization: s => s.UseSerializer<JsonNetSerializer>());
        
        FortnitePorting = new FortnitePortingAPI(_client);
        FortnitePortingServer = new FortnitePortingServerAPI(_client);
        EpicGames = new EpicGamesAPI(_client);
    }

    protected readonly RestClient _client;
    
    public string GetUrl(RestRequest request)
    {
        return _client.BuildUri(request).ToString();
    }

    public async Task<byte[]?> GetBytesAsync(string url)
    {
        var request = new RestRequest(url);
        return await _client.DownloadDataAsync(request);
    }
    
    public byte[]? GetBytes(string url)
    {
        return GetBytesAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async Task<FileInfo> DownloadFileAsync(string url, string destination)
    {
        Log.Information("Downloading {url} to {destination}", url, destination);
        var request = new RestRequest(url);
        var data = await _client.DownloadDataAsync(request);
        if (data is null)
        {
            Log.Information("Failed to download {url} to {destination}", url, destination);
            return null;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        
        await File.WriteAllBytesAsync(destination, data);
        return new FileInfo(destination);
    }
    
    public async Task<FileInfo> DownloadFileAsync(string url, string destination, Action<float> progressAction)
    {
        Log.Information("Downloading {url} to {destination}", url, destination);
        
        using var httpClientInstance = new HttpClient(_client.Options.ConfigureMessageHandler?.Invoke(new HttpClientHandler()) ?? new HttpClientHandler());
        var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(url),
        };

        using var response = await httpClientInstance.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
        {
            Log.Information("Failed to download {url} to {destination}", url, destination);
            return null;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        
        await using var responseStream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = new FileStream(destination, FileMode.Create, FileAccess.Write);

        var buffer = new byte[8192];
        int bytesRead;

        var totalBytesRead = 0.0f;
        var totalByteCount = response.Content.Headers.ContentLength ?? -1;
        while ((bytesRead = await responseStream.ReadAsync(buffer)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
            totalBytesRead += bytesRead;

            progressAction(totalBytesRead / totalByteCount);
        }
        
        return new FileInfo(destination);
    }

    public async Task<FileInfo> DownloadFileAsync(string url, DirectoryInfo destination)
    {
        var outPath = Path.Combine(destination.FullName, Path.GetFileName(url));
        Log.Information("Downloading {url} to {destination}", url, outPath);
        
        var request = new RestRequest(url);
        var data = await _client.DownloadDataAsync(request);
        
        if (data is not null) 
            await File.WriteAllBytesAsync(outPath, data);
        else 
            Log.Information("Failed to download {url} to {destination}", url, destination);
        
        return new FileInfo(outPath);
    }
    
    public FileInfo DownloadFile(string url, DirectoryInfo destination)
    {
        return DownloadFileAsync(url, destination).GetAwaiter().GetResult();
    }

    public string? GetHash(string url)
    {
        var response = _client.Head(new RestRequest(url));
        var hashHeader = response.Headers?.FirstOrDefault(header => header.Name?.Equals("Hash") ?? false);
        return hashHeader?.Value?.ToString();
    }
}