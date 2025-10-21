using System;

namespace FortnitePorting.Models.API.Responses;

public class MappingsResponse
{
    public string Version;
    public DateTime? Updated;
    public MappingsMeta Mappings;

    public DateTime GetCreationTime()
    {
        return Updated ?? DateTime.Now;
    }
}

public class MappingsMeta
{
    public string? ZStandard;
    public string? Brotli;
    public string? Oodle; // Assume this is an option

    public string? GetMappingsURL()
    {
        return ZStandard ?? Brotli ?? Oodle;
    }
}