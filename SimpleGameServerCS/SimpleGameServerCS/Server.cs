using System.Net.Sockets;

namespace SimpleGameServerCS;

public class Server
{
    public void Start(int _port)
    {
        OpenTCPListner();
    }

    private void OpenTCPListner()
    {
        Socket _server = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        
    }
}