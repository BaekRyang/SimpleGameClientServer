#pragma comment(lib, "ws2_32.lib")
#include "Client.h"
#include <iostream>
#include <winsock2.h>
#include <ws2tcpip.h>

void Client::ErrorDisplay(const char* str)
{
    LPVOID out;
    FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM, nullptr, WSAGetLastError(),
                  MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPSTR)&out, 0, nullptr);
    std::cout << str << " error: " << (LPSTR)out << std::endl;
}
int Client::Run()
{
    std::cout << "Insert server IP: ";
    char serverIP[16];
    std::cin >> serverIP;

    std::cout << "Connecting to server " << serverIP << std::endl;
    
    WSADATA wsa;

    if (WSAStartup(MAKEWORD(2, 2), &wsa))
    {
        ErrorDisplay("WSAStartup()");
        return -1;
    }

    SOCKET socket = ::socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    if (socket == INVALID_SOCKET)
    {
        ErrorDisplay("socket()");
        return -1;
    }

    SOCKADDR_IN serverAddr;
    serverAddr.sin_family      = AF_INET;
    serverAddr.sin_port        = htons(25565);
    InetPton(AF_INET, (PCSTR)serverIP, &serverAddr.sin_addr);

    if (connect(socket, (SOCKADDR*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR)
    {
        ErrorDisplay("connect()");
        return -1;
    }

    std::cout << "Connected to server!" << std::endl;
}
