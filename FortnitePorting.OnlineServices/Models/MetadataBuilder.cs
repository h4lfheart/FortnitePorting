using FortnitePorting.OnlineServices.Packet;

namespace FortnitePorting.OnlineServices.Models;

public class MetadataBuilder
{
    private Dictionary<string, object> Arguments = [];

    public MetadataBuilder WithType(EPacketType packetType)
    {
        Arguments.Add("Type", packetType);
        return this;
    }
    
    public MetadataBuilder With(string key, object obj)
    {
        Arguments.Add(key, obj);
        return this;
    }
    
    public MetadataBuilder With(MetadataBuilder? builder)
    {
        if (builder is not null)
        {
            foreach (var (key, value) in builder.Arguments)
            {
                Arguments[key] = value;
            }
        }
        
        return this;
    }
    
    public Dictionary<string, object> Build()
    {
        return Arguments;
    }

    public static Dictionary<string, object> Empty(EPacketType type)
    {
        return new MetadataBuilder().WithType(type).Build();
    }
}