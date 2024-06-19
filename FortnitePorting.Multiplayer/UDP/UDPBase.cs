using System.Net.Sockets;

namespace FortnitePorting.Multiplayer.UDP;

public abstract class UDPBase
{
    public int BufferSize = 4096;
    public event ReceiveData DataReceived;
    
    protected UdpClient Client;

    protected UDPBase()
    {
        Client = new UdpClient();
        Client.Client.ReceiveBufferSize = BufferSize;
        Client.Client.ReceiveBufferSize = BufferSize;
    }
    
    public async Task<UdpReceiveResult> Receive()
    { 
        return await Client.ReceiveAsync();
    }

    public virtual void OnDataReceived(UdpReceiveResult result)
    {
        DataReceived?.Invoke(result);
    }
}

public delegate void ReceiveData(UdpReceiveResult result);