using System;
using System.Collections.Generic;

namespace FortnitePorting.Models.API.Responses;

public class RepositoryResponse
{
    public string Id;
    public string Title;
    public string Description;
    public string? Icon;
    public List<RepositoryVersion> Versions = [];
}

public class RepositoryVersion
{
    public FPVersion Version;
    public string ExecutableURL;
    public DateTime UploadTime;
}