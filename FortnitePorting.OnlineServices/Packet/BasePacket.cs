using FortnitePorting.OnlineServices.Models;

namespace FortnitePorting.OnlineServices.Packet;

public abstract class BasePacket : BaseDualSerialize
{
    public abstract EPacketType PacketType { get; }
}

public enum EPacketType
{
    // General
    Connect,
    Disconnect,
    Ping,
    Permissions,
    
    // Chat
    Message,
    Reaction,
    OnlineUsers,
    Export,
    SetRole,
    MessageHistory,
    DeleteMessage,
    
    // Canvas
    RequestCanvasInfo,
    CanvasInfo,
    CanvasPlacementInfo,
    CanvasPixel,
    
    // Misc
    Title
}