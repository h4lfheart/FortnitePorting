using System.Collections.Generic;

namespace FortnitePorting.Models.API.Responses;

public class AesResponse
{
    public string Version;
    public AesKey MainKey;
    public List<AesKey> DynamicKeys;
}

public class AesKey
{
    public string Name;
    public string Key;
    public string GUID;
}
