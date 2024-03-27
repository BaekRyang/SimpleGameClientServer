using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SimpleGameClientCS;

public class Client
{
    private (IPAddress, int) _serverIPP;
    private Socket           _socket;
    private byte[]           _sendBuffer;
    private byte[]           _receiveBuffer = new byte[1024];

    public void Start(IPAddress _ip, int _port)
    {
        _serverIPP = (_ip, _port);
        _socket    = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _socket.BeginConnect(_ip, _port, ConnectCallback, _socket);
        GetChatString();
    }

    private void GetChatString()
    {
        string _chatString = Console.ReadLine();
        _sendBuffer = Encoding.UTF8.GetBytes(_chatString);
        _socket.BeginSend(_sendBuffer, 0, _sendBuffer.Length, SocketFlags.None, SendCallback, _socket);
        GetChatString();
    }

    private void SendCallback(IAsyncResult _ar)
    {
        Socket _socket = (Socket)_ar.AsyncState;
        _socket.EndSend(_ar);
        var _sender = _socket.LocalEndPoint.ToString();
        
        var _cursorTopPos = Console.GetCursorPosition().Top;
        Console.SetCursorPosition(0, _cursorTopPos - 1);
        
        if (_sendBuffer.Length == 0) 
            return;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"{_sender} ({_sendBuffer.Length}) :");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($" {Encoding.UTF8.GetString(_sendBuffer)}");
        Console.ResetColor();
        
        Array.Clear(_sendBuffer, 0, _sendBuffer.Length);
    }

    private void ConnectCallback(IAsyncResult _ar)
    {
        Socket _socket = (Socket)_ar.AsyncState;
        _socket.EndConnect(_ar);
        Console.WriteLine("Connected to the server.");
        _sendBuffer = "Hello server!"u8.ToArray();
        _socket.BeginReceive(_receiveBuffer, 0, _receiveBuffer.Length, SocketFlags.None, ReceiveCallback, _socket);
    }

    private void ReceiveCallback(IAsyncResult _ar)
    {
        try
        {
            Socket _socket   = (Socket)_ar.AsyncState;
            int    _received = _socket.EndReceive(_ar);

            if (_received == 0)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"Disconnected from {_socket.RemoteEndPoint}");
                Console.ResetColor();
                Program.Main(null);
                return;
            }
            
            byte[] _receiveData = new byte[_received];
            Array.Copy(_receiveBuffer, _receiveData, _received);
            ParseData(_receiveData);
            
            Array.Clear(_receiveBuffer, 0, _receiveBuffer.Length);
            _socket.BeginReceive(_receiveBuffer, 0, _receiveBuffer.Length, SocketFlags.None, ReceiveCallback, _socket);
        }
        catch (SocketException _se)
        {
            switch (_se.SocketErrorCode)
            {
                
                case SocketError.ConnectionReset:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"ConnectionReset from client {(_ar.AsyncState as Socket).RemoteEndPoint}.");
                    Console.ResetColor();
                    break;
                default:
                    Console.WriteLine(_se);
                    break;
            }
        }
    }

    private void ParseData(byte[] _bytes)
    {
        string _data = Encoding.UTF8.GetString(_bytes);

        string[] _split       = _data.Split("::");
        int      _chatSize = _bytes.Length - _split[0].Length - 2;
        if (_data.StartsWith("Srv"))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Server");
            Console.ResetColor();
            Console.WriteLine($" ({_chatSize}) : {_split[1]}");
            return;
        }

        string[] _ipPort = _split[0].Split(":");

        try
        {
            (IPAddress, int, string) _result = (IPAddress.Parse(_ipPort[0]), int.Parse(_ipPort[1]), _split[1]);
            Console.Write($"{_result.Item1}:{_result.Item2}");
            Console.WriteLine($" ({_chatSize}) : {_result.Item3}");
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error : {_socket.RemoteEndPoint}");
            Console.ResetColor();
        }
        
    }
}