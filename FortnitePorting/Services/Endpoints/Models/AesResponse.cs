using System.Collections.Generic;
using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace FortnitePorting.Services.Endpoints.Models;

public class AesResponse
{
    [J] public string Version;
    [J] public string MainKey;
    [J] public List<DynamicKey> DynamicKeys;
}

public class DynamicKey
{
    [J] public string Name;
    [J] public string Key;
    [J] public string GUID;
}