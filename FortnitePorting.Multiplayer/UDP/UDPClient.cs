using System.Net;
using System.Net.Sockets;
using FortnitePorting.Multiplayer.Data;

namespace FortnitePorting.Multiplayer.UDP;

public class UDPClient : UDPBase
{
    public UDPClient() : this(new IPEndPoint(IPAddress.Parse(MultiplayerGlobals.SOCKET_IP), MultiplayerGlobals.SOCKET_PORT))
    {
    }

    public UDPClient(IPEndPoint endpoint)
    {
        Client = new UdpClient();
        Client.Connect(endpoint);
    }

    public void Send(byte[] data)
    {
        Client.Send(data);
    }

    public void Send(BaseData data, UserData user)
    {
        var stream = new MemoryStream();
        var Ar = new BinaryWriter(stream);
        
        var header = new DataHeader(data.DataType);
        header.Serialize(Ar);
        user.Serialize(Ar);
        data.Serialize(Ar);
        
        Send(stream.GetBuffer());
    }

}