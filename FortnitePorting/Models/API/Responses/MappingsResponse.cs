using System;

namespace FortnitePorting.Models.API.Responses;

public class MappingsResponse
{
    public string URL;
    public string Filename;
    public long Length;
    public DateTime Uploaded;
    public MappingsMeta Meta;
}

public class MappingsMeta
{
    public string Version;
    public string CompressionMethod;
}