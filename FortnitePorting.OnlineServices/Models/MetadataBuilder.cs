using FortnitePorting.OnlineServices.Packet;
using Newtonsoft.Json;
using Serilog;

namespace FortnitePorting.OnlineServices.Models;

public class MetadataBuilder : IDualSerialize
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

    public MetadataBuilder WithSuffix(string suffix)
    {
        var newArguments = new Dictionary<string, object>();
        foreach (var (key, value) in Arguments)
        {
            var newKeyName = $"{key}_{suffix}";
            newArguments[newKeyName] = value;
        }

        Arguments = newArguments;

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

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Arguments.Count);
        foreach (var (key, value) in Arguments)
        {
            writer.Write(key);

            switch (value)
            {
                case int i:
                {
                    writer.Write((byte) EPropertyType.Int);
                    writer.Write(i);
                    break;
                }
                case float f:
                {
                    writer.Write((byte) EPropertyType.Float);
                    writer.Write(f);
                    break;
                }
                case bool b:
                {
                    writer.Write((byte) EPropertyType.Bool);
                    writer.Write(b);
                    break;
                }
                case string str:
                {
                    writer.Write((byte) EPropertyType.String);
                    writer.Write(str);
                    break;
                }
                case Guid guid:
                {
                    writer.Write((byte) EPropertyType.Guid);
                    writer.Write(guid.ToString());
                    break;
                }
                case Identification identification:
                {
                    writer.Write((byte) EPropertyType.Identification);
                    identification.Serialize(writer);
                    break;
                }
                case DateTime time:
                {
                    writer.Write((byte) EPropertyType.DateTime);
                    writer.Write(time.ToBinary());
                    break;
                }
                default:
                    throw new NotImplementedException();
            }
        }
    }

    public void Deserialize(BinaryReader reader)
    {
        var argumentCount = reader.ReadInt32();
        for (var i = 0; i < argumentCount; i++)
        {
            var key = reader.ReadString();
            var propertyType = (EPropertyType) reader.ReadByte();

            Arguments[key] = propertyType switch
            {
                EPropertyType.Int => reader.ReadInt32(),
                EPropertyType.Float => reader.ReadSingle(),
                EPropertyType.Bool => reader.ReadBoolean(),
                EPropertyType.Guid => new Guid(reader.ReadString()),
                EPropertyType.String => reader.ReadString(),
                EPropertyType.Identification => IDualSerialize.Deserialize<Identification>(reader),
                EPropertyType.DateTime => DateTime.FromBinary(reader.ReadInt64()),
                _ => throw new NotImplementedException()
            };
        }
    }
}

public enum EPropertyType : byte
{
    Int,
    Float,
    Bool,
    Guid,
    String,
    Identification,
    DateTime
}