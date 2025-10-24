using System;

namespace FortnitePorting.Models.API.Responses;

public class MappingsResponse
{
    public string Version;
    public DateTime? Updated;
    public string Url;

    public DateTime GetCreationTime()
    {
        return Updated ?? DateTime.Now;
    }
}