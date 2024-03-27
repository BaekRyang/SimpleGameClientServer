using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SimpleGameServerCS;

public class Server
{
    private Socket          _listenSocket;
    private List<Socket>    _clientSockets = new();
    private byte[]          _sendBuffer;
    private byte[]          _receiveBuffer = new byte[1024];
    private List<IPAddress> _bannedIP      = new();

    public async void Start(int _port)
    {
        GetBannedIP();
        await Task.Delay(1000);
        Console.Clear();
        Console.WriteLine($"Banned client : {_bannedIP.Count}" +
                          $"\nTotal client : {_clientSockets.Count}");
        OpenTCPListener(_port);
        GetChatString();
    }

    private void GetBannedIP()
    {
        if (File.Exists("BannedIP.txt") is false)
            File.Create("BannedIP.txt").Close();
        
        string[] _bannedIPs = File.ReadAllLines("BannedIP.txt");
        foreach (string _IP in _bannedIPs)
        {
            Console.WriteLine($"Banned IP : {_IP}");
            _bannedIP.Add(IPAddress.Parse(_IP));
        }
    }

    private void GetChatString()
    {
        while (true)
        {
            string _chatString = Console.ReadLine();

            var _cursorTopPos = Console.GetCursorPosition().Top;
            Console.SetCursorPosition(0, _cursorTopPos - 1);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"Server ({_chatString.Length}) :");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($" {_chatString}");
            Console.ResetColor();

            byte[] _chatData = Encoding.UTF8.GetBytes($"Srv::{_chatString}");
            BroadcastMessage(_chatData);
        }
    }

    private void BroadcastMessage(byte[] _chatData, params Socket[] _excludeSockets)
    {
        foreach (Socket _socket in _clientSockets)
            if (_excludeSockets.Contains(_socket) is false)
                _socket.BeginSend(_chatData, 0, _chatData.Length, SocketFlags.None, _ => { }, _socket);
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

        if (_bannedIP.Contains((_socket.RemoteEndPoint as IPEndPoint).Address))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Banned client tried to connect from {_socket.RemoteEndPoint}");
            Console.ResetColor();
            _socket.Close();
        }
        else
        {
            _clientSockets.Add(_socket);
            Console.WriteLine($"New connection in {_socket.RemoteEndPoint}");
            UpdateClientCount();
            _socket.BeginReceive(_receiveBuffer, 0, 1024, SocketFlags.None, ReceiveCallback, _socket);
        }
        

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
            
            BroadcastMessage(Encoding.UTF8.GetBytes($"{_socket.RemoteEndPoint}::{_text}"), _socket);
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