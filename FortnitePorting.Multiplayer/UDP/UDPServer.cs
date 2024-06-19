using System.Net;
using System.Net.Sockets;
using FortnitePorting.Multiplayer.Data;

namespace FortnitePorting.Multiplayer.UDP;

public class UDPServer : UDPBase
{
    private IPEndPoint Endpoint;

    public UDPServer() : this(new IPEndPoint(IPAddress.Any, MultiplayerGlobals.SOCKET_PORT))
    {
    }

    public UDPServer(IPEndPoint endpoint)
    {
        Endpoint = endpoint;
        Client = new UdpClient(Endpoint);
    }

    public void Send(byte[] data, IPEndPoint endpoint)
    {
        Client.Send(data, endpoint);
    }
    
    public void Send(BaseData data, UserData sender, IPEndPoint endPoint)
    {
        var stream = new MemoryStream();
        var Ar = new BinaryWriter(stream);
        
        var header = new DataHeader(data.DataType);
        header.Serialize(Ar);
        sender.Serialize(Ar);
        data.Serialize(Ar);
        
        Send(stream.GetBuffer(), endPoint);
    }

}