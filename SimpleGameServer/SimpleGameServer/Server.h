#pragma once
#include <winsock2.h>

class Server
{
public:
    static void ErrorDisplay(const char* str);
    int         ClientConnection(SOCKET socket, SOCKADDR_IN clientAddr, int clientAddrSize);
    int         Run();
};
