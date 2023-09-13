using System;
using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace FortnitePorting.Services.Endpoints.Models;

public class BroadcastResponse
{
    [J] public DateTime PushedTime;
    [J] public string Title;
    [J] public string Contents;
    [J] public string Version;
    [J] public bool IsActive;
}