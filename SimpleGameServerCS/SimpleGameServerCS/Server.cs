using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SimpleGameServerCS;

public class Server
{
    private Socket _listenSocket;
    
    private List<Socket> _clientSockets = new();
    public void Start(int _port)
    {
        OpenTCPListener(_port);
    }

    private void OpenTCPListener(int _port)
    {
        _listenSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPEndPoint _endPoint = new(IPAddress.Any, _port);
        _listenSocket.Bind(_endPoint);
        _listenSocket.Listen(10);
        _listenSocket.BeginAccept(AcceptCallback, null);
    }

    private void AcceptCallback(IAsyncResult _ar)
    {
        Socket _socket = _listenSocket.EndAccept(_ar);
        _clientSockets.Add(_socket);
        _socket.BeginReceive(new byte[1024], 0, 1024, SocketFlags.None, ReceiveCallback, _socket);
        Console.WriteLine($"New connection in {_socket.RemoteEndPoint}");
        _listenSocket.BeginAccept(AcceptCallback, null);
    }

    private void ReceiveCallback(IAsyncResult _ar)
    {
        Console.WriteLine($"Received");
    }
}