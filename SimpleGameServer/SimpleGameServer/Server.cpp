#pragma comment(lib, "ws2_32.lib")
#include "Server.h"
#include <iostream>
#include <vector>
#include <winsock2.h>
#include <ws2tcpip.h>

std::vector<SOCKET> clients;

void Server::ErrorDisplay(const char* str)
{
    LPVOID out;
    FormatMessage(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM, nullptr, WSAGetLastError(),
                  MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPSTR)&out, 0, nullptr);
    std::cout << str << " error: " << (LPSTR)out << std::endl;
    LocalFree(out);
}

int Server::ClientConnection(SOCKET socket, SOCKADDR_IN clientAddr, int clientAddrSize)
{
    while (true)
    {
        std::cout << "Waiting for client..." << std::endl;
        SOCKET clientSocket = accept(socket, (SOCKADDR*)&clientAddr, &clientAddrSize);
        if (clientSocket == INVALID_SOCKET)
        {
            ErrorDisplay("accept()");
            return -1;
        }
        std::cout << "Client connected!" << std::endl;
        clients.push_back(clientSocket);
    }
}

int Server::Run()
{
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
    serverAddr.sin_addr.s_addr = INADDR_ANY;

    if (bind(socket, (SOCKADDR*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR)
    {
        ErrorDisplay("bind()");
        return -1;
    }

    if (listen(socket, SOMAXCONN))
    {
        ErrorDisplay("listen()");
        return -1;
    }

    SOCKADDR_IN clientAddr;
    int         clientAddrSize = sizeof(clientAddr);

    while (true)
    {
        std::cout << "Waiting for client..." << std::endl;
        SOCKET clientSocket = accept(socket, (SOCKADDR*)&clientAddr, &clientAddrSize);
        if (clientSocket == INVALID_SOCKET)
        {
            ErrorDisplay("accept()");
            -1;
        }
        char clientIP[INET_ADDRSTRLEN];
        InetNtop(AF_INET, &clientAddr.sin_addr, PSTR(clientIP), INET_ADDRSTRLEN);
        
        std::cout << "Client connected! - IP :" << clientIP << std::endl;
        clients.push_back(clientSocket);

        std::cout << "Connected clients: " << clients.size() << std::endl;
    }

    return 0;
}
