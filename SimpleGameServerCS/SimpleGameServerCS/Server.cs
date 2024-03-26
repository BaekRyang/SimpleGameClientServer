using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SimpleGameServerCS;

public class Server
{
    private Socket       _listenSocket;
    private List<Socket> _clientSockets = new();
    private byte[]       _sendBuffer;
    private byte[]       _receiveBuffer = new byte[1024];

    public void Start(int _port)
    {
        Console.Clear();
        Console.WriteLine($"\nTotal client : {_clientSockets.Count}");
        OpenTCPListener(_port);
        GetChatString();
    }

    private void GetChatString()
    {
        string _chatString = Console.ReadLine();

        var _cursorTopPos = Console.GetCursorPosition().Top;
        Console.SetCursorPosition(0, _cursorTopPos - 1);
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"Server ({_chatString.Length}) :");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($" {_chatString}");
        Console.ResetColor();

        byte[] _chatData = Encoding.UTF8.GetBytes(_chatString);
        foreach (Socket _socket in _clientSockets)
            _socket.Send(_chatData);
        GetChatString();
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
        Console.WriteLine($"New connection in {_socket.RemoteEndPoint}");
        UpdateClientCount();
        _socket.BeginReceive(_receiveBuffer, 0, 1024, SocketFlags.None, ReceiveCallback, _socket);
        _listenSocket.BeginAccept(AcceptCallback, null);
    }

    private void ReceiveCallback(IAsyncResult _ar)
    {
        try
        {
            Socket _socket   = (Socket)_ar.AsyncState;
            int    _received = _socket.EndReceive(_ar);
            UpdateClientCount();

            if (_received == 0)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"Disconnected from {_socket.RemoteEndPoint}");
                Console.ResetColor();
            }

            string _text = Encoding.UTF8.GetString(_receiveBuffer);
            Console.WriteLine($"{_socket.RemoteEndPoint} ({_received}) : {_text}");
            Array.Clear(_receiveBuffer, 0, _receiveBuffer.Length);
            _socket.BeginReceive(_receiveBuffer, 0, 1024, SocketFlags.None, ReceiveCallback, _socket);
        }
        catch (SocketException _se)
        {
            switch (_se.SocketErrorCode)
            {
                case SocketError.ConnectionReset: //클라이언트가 연결을 끊었을 때
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"ConnectionReset from client {(_ar.AsyncState as Socket).RemoteEndPoint}.");
                    Console.ResetColor();
                    _clientSockets.Remove((_ar.AsyncState as Socket)!);
                    UpdateClientCount();
                    break;
                case SocketError.ConnectionAborted:
                    Console.WriteLine("ConnectionAborted from client.");
                    break;
                case SocketError.SocketError:
                case SocketError.Success:
                case SocketError.OperationAborted:
                case SocketError.IOPending:
                case SocketError.Interrupted:
                case SocketError.AccessDenied:
                case SocketError.Fault:
                case SocketError.InvalidArgument:
                case SocketError.TooManyOpenSockets:
                case SocketError.WouldBlock:
                case SocketError.InProgress:
                case SocketError.AlreadyInProgress:
                case SocketError.NotSocket:
                case SocketError.DestinationAddressRequired:
                case SocketError.MessageSize:
                case SocketError.ProtocolType:
                case SocketError.ProtocolOption:
                case SocketError.ProtocolNotSupported:
                case SocketError.SocketNotSupported:
                case SocketError.OperationNotSupported:
                case SocketError.ProtocolFamilyNotSupported:
                case SocketError.AddressFamilyNotSupported:
                case SocketError.AddressAlreadyInUse:
                case SocketError.AddressNotAvailable:
                case SocketError.NetworkDown:
                case SocketError.NetworkUnreachable:
                case SocketError.NetworkReset:
                case SocketError.NoBufferSpaceAvailable:
                case SocketError.IsConnected:
                case SocketError.NotConnected:
                case SocketError.Shutdown:
                case SocketError.TimedOut:
                case SocketError.ConnectionRefused:
                case SocketError.HostDown:
                case SocketError.HostUnreachable:
                case SocketError.ProcessLimit:
                case SocketError.SystemNotReady:
                case SocketError.VersionNotSupported:
                case SocketError.NotInitialized:
                case SocketError.Disconnecting:
                case SocketError.TypeNotFound:
                case SocketError.HostNotFound:
                case SocketError.TryAgain:
                case SocketError.NoRecovery:
                case SocketError.NoData:
                default:
                    Console.WriteLine(_se);
                    break;
            }
        }
    }

    private void UpdateClientCount()
    {
        (int Left, int Top) _cursorPosition = Console.GetCursorPosition();
        Console.SetCursorPosition(0, 0);
        Console.WriteLine($"{"",-100}");
        Console.WriteLine($"Total client : {_clientSockets.Count}{"",-100}");
        Console.SetCursorPosition(_cursorPosition.Left, _cursorPosition.Top);
    }
}