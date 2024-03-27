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

            if (_chatString.StartsWith('/'))
                ProcessCommand(_chatString);
            else
            {
                Console.Write($"Server ({_chatString.Length}) :");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($" {_chatString}");
                Console.ResetColor();
                byte[] _chatData = Encoding.UTF8.GetBytes($"Srv::{_chatString}");
                BroadcastMessage(_chatData);
            }
        }
    }

    private void ProcessCommand(string _chatString)
    {
        Console.WriteLine();
        string[]  _split = _chatString.Split(' ');
        switch (_split[0])
        {
            case "/ban":
                if (_split.Length < 2)
                {
                    Console.WriteLine("Usage : /ban [IP]");
                    return;
                }

                if (IPAddress.TryParse(_split[1], out IPAddress _ip) is false)
                {
                    Console.WriteLine("Invalid IP address.");
                    return;
                }

                if (_bannedIP.Contains(_ip))
                {
                    Console.WriteLine("Already banned.");
                    return;
                }

                _bannedIP.Add(_ip);
                File.AppendAllText("BannedIP.txt", $"{_ip}\n");
                Console.WriteLine($"Banned {_ip}");
                
                foreach (Socket _socket in _clientSockets)
                    if ((_socket.RemoteEndPoint as IPEndPoint).Address.Equals(_ip))
                    {
                        var _msg = "Srv::You are banned."u8.ToArray();
                        _socket.Send(_msg);
                        
                        var _broadcastMsg = Encoding.UTF8.GetBytes($"Srv::{_socket.RemoteEndPoint} has been banned.");
                        BroadcastMessage(_broadcastMsg, _socket);
                        _clientSockets.Remove(_socket);
                        UpdateClientCount();
                        _socket.Close();
                        break;
                    }
                break;
            case "/unban":
                if (_split.Length < 2)
                {
                    Console.WriteLine("Usage : /unban [IP]\n" +
                                      "        /unban [Index]");
                    return;
                }
                IPAddress _ip2 = IPAddress.None;
                if (_split[1].Split('.').Length == 4)
                {
                    if (IPAddress.TryParse(_split[1], out _ip2) is false)
                    {
                        Console.WriteLine("Invalid IP address.");
                        return;
                    }
                    
                    if (_bannedIP.Contains(_ip2) is false)
                    {
                        Console.WriteLine("Not banned.");
                        return;
                    }
                }
                else
                {
                    if (int.TryParse(_split[1], out int _index) &&
                        _index >= 1 && _index < _bannedIP.Count + 1)
                        _ip2 = _bannedIP[_index - 1];
                    else
                    {
                        Console.WriteLine("Invalid index.");
                        return;
                    }
                }

                _bannedIP.Remove(_ip2);
                File.WriteAllLines("BannedIP.txt", _bannedIP.Select(_ => _.ToString()));
                Console.WriteLine($"Unbanned {_ip2}");
                break;
            case "/ban-list":
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("Banned IP list :");
                Console.ForegroundColor = ConsoleColor.Yellow;
                for (int _i = 0; _i < _bannedIP.Count; _i++)
                {
                    IPAddress _ip3 = _bannedIP[_i];
                    Console.WriteLine($" {_i + 1 + ".",-3} {_ip3}");
                }

                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("End of list.");
                break;
            default:
                Console.WriteLine("Unknown command.");
                break;
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
            
            var _msg = "Srv::You are banned."u8.ToArray();
            _socket.BeginSend(_msg, 0, _msg.Length, SocketFlags.None, _ => { }, _socket);
            _socket.Close();
        }
        else
        {
            _clientSockets.Add(_socket);
            Console.WriteLine($"New connection in {_socket.RemoteEndPoint}");
            UpdateClientCount();
            _socket.BeginReceive(_receiveBuffer, 0, _receiveBuffer.Length, SocketFlags.None, ReceiveCallback, _socket);
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

            string _text = Encoding.UTF8.GetString(_receiveBuffer).TrimEnd('\0');
            Console.WriteLine($"{_socket.RemoteEndPoint} ({_received}) : {_text}");

            BroadcastMessage(Encoding.UTF8.GetBytes($"{_socket.RemoteEndPoint}::{_text}"), _socket);
            Array.Clear(_receiveBuffer, 0, _receiveBuffer.Length);
            _socket.BeginReceive(_receiveBuffer, 0, _receiveBuffer.Length, SocketFlags.None, ReceiveCallback, _socket);
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
                    _clientSockets.Remove((_ar.AsyncState as Socket)!);
                    UpdateClientCount();
                    break;
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