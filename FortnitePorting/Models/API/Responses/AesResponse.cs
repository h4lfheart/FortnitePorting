using System.Collections.Generic;

namespace FortnitePorting.Models.API.Responses;

public class AesResponse
{
    public string Version;
    public string MainKey;
    public List<DynamicKey> DynamicKeys;
}

public class DynamicKey
{
    public string Key;
    public string GUID;
}
