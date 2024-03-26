﻿using System.Net;
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
            }

            string _sender;
            if (_socket.RemoteEndPoint.ToString() == _serverIPP.Item1 + ":" + _serverIPP.Item2)
                _sender = "Server";
            else
                _sender = _socket.RemoteEndPoint.ToString();

            string _text = Encoding.UTF8.GetString(_receiveBuffer);
            Console.WriteLine($"{_sender} ({_received}) : {_text}");
            Array.Clear(_receiveBuffer, 0, _receiveBuffer.Length);
            _socket.BeginReceive(_receiveBuffer, 0, 1024, SocketFlags.None, ReceiveCallback, _socket);
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

    private void SendCallback(IAsyncResult _ar)
    {
        Socket _socket = (Socket)_ar.AsyncState;
        _socket.EndSend(_ar);
        var    _sender = _socket.LocalEndPoint.ToString();
        
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
}