namespace FortnitePorting.Framework.ViewModels.Endpoints.Models;

public class ReleaseResponse
{
    public string Version;
    public string DownloadUrl;

    public DependencyResponse[] Dependencies;
}

public class DependencyResponse
{
    public string Name;
    public string URL;
}