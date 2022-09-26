using System;
using Newtonsoft.Json;

namespace FortnitePorting.Services.Endpoints.Models;

public class MappingsResponse
{
    [JsonProperty("url")]
    public string URL;
        
    [JsonProperty("filename")]
    public string Filename;
        
    [JsonProperty("length")]
    public long Length;
    
    [JsonProperty("uploaded")]
    public DateTime Uploaded;
        
    [JsonProperty("meta")]
    public MappingsMeta Meta;
    
}

public class MappingsMeta
{
    [JsonProperty("version")]
    public string Version;
            
    [JsonProperty("compressionMethod")]
    public string CompressionMethod;
}