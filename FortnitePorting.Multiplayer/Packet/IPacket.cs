using FortnitePorting.Multiplayer.Models;

namespace FortnitePorting.Multiplayer.Packet;

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