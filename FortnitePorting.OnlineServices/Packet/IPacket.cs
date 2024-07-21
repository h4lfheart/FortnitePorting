using FortnitePorting.OnlineServices.Models;

namespace FortnitePorting.OnlineServices.Packet;

public interface IPacket : IDualSerialize
{
    public EPacketType PacketType { get; }
}

public enum EPacketType
{
    Connect,
    Disconnect,
    Ping,
    Permissions,
    Message,
    Reaction,
    OnlineUsers,
    Export,
    
    // Owner
    SetRole,
}