using System.Collections.Generic;
using Newtonsoft.Json;

namespace FortnitePorting.Services.Endpoints.Models;

public class AesResponse
{
    [JsonProperty("version")]
    public string Version;
        
    [JsonProperty("mainKey")]
    public string MainKey;

    [JsonProperty("dynamicKeys")] 
    public List<DynamicKey> DynamicKeys;
}

public class DynamicKey
{
    [JsonProperty("name")]
    public string Name;
            
    [JsonProperty("key")]
    public string Key;
            
    [JsonProperty("guid")]
    public string GUID;
    
    /*[JsonProperty("keychain")]
    public string Keychain;
    
    [JsonProperty("fileCount")]
    public int FileCount;
    
    [JsonProperty("hasHighResTextures")]
    public bool HasHighResTextures;
    
    [JsonProperty("size")]
    public ChunkSize Size;*/
}

public class ChunkSize
{
    [JsonProperty("raw")]
    public long Raw;
            
    [JsonProperty("formatted")]
    public string Formatted;
}