using System;
using System.Collections.Generic;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Objects.Core.Misc;
using Newtonsoft.Json;

namespace FortnitePorting.Models.API.Responses;

public class AesResponse
{
    public string Version;
    public string MainKey;
    public List<DynamicKey> DynamicKeys;
}

public class DynamicKey
{
    public string Name;
    public string Key;
    public string GUID;
}
