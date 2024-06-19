using CUE4Parse_Conversion.ActorX;
using CUE4Parse_Conversion.UEFormat;
using CUE4Parse.UE4.Writers;
using Tmds.DBus.Protocol;

namespace FortnitePorting.Models.Sockets;

public class BaseSocketMessage : ISerializable
{
    public virtual ESocketMessageType Type { get; set; }

    public virtual void Serialize(FArchiveWriter Ar)
    {
        Ar.WriteFString("FPSOCKET");
        Ar.Write((byte) Type);
    }
}

public class RegisterSocketMessage : BaseSocketMessage
{
    public override ESocketMessageType Type => ESocketMessageType.Register;

    public string Name;

    public RegisterSocketMessage(string name)
    {
        Name = name;
    }

    public override void Serialize(FArchiveWriter Ar)
    {
        base.Serialize(Ar);
        
        Ar.WriteFString(Name);
    }
}

public class UnregisterSocketMessage : BaseSocketMessage
{
    public override ESocketMessageType Type => ESocketMessageType.Unregister;
}

public class TextSocketMessage : BaseSocketMessage
{
    public override ESocketMessageType Type => ESocketMessageType.Text;

    public string Text;

    public TextSocketMessage(string text)
    {
        Text = text;
    }

    public override void Serialize(FArchiveWriter Ar)
    {
        base.Serialize(Ar);
        
        Ar.WriteFString(Text);
    }
}

public class MessageSocketMessage : BaseSocketMessage
{
    public override ESocketMessageType Type => ESocketMessageType.Message;

    public string Text;
    public string TargetName;

    public MessageSocketMessage(string targetName, string text)
    {
        Text = text;
        TargetName = targetName;
    }

    public override void Serialize(FArchiveWriter Ar)
    {
        base.Serialize(Ar);
        
        Ar.WriteFString(TargetName);
        Ar.WriteFString(Text);
    }
}

public class ExportSocketMessage : BaseSocketMessage
{
    public override ESocketMessageType Type => ESocketMessageType.Export;

    public string Text;
    public string TargetName;

    public ExportSocketMessage(string targetName, string text)
    {
        Text = text;
        TargetName = targetName;
    }

    public override void Serialize(FArchiveWriter Ar)
    {
        base.Serialize(Ar);
        
        Ar.WriteFString(TargetName);
        Ar.WriteFString(Text);
    }
}

public enum ESocketMessageType : byte
{
    Register,
    Text,
    Unregister,
    Message,
    Export
}

public static class SocketMessageExtensions
{
    public static byte[] GetBytes(this BaseSocketMessage message)
    {
        var writer = new FArchiveWriter();
        message.Serialize(writer);
        return writer.GetBuffer();
    }
}