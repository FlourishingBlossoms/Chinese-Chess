#ifndef SERVER_H
#define SERVER_H

#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#include <winsock2.h>
#include <ws2tcpip.h>
#pragma comment(lib, "ws2_32.lib") // 自动链接 ws2_32.lib
#else
#include <sys/socket.h>
#include <netinet/in.h>
#include <unistd.h>
#define SOCKET int
#define INVALID_SOCKET (-1)
#define SOCKET_ERROR (-1)
#define closesocket close
#endif

#include <stddef.h>

#define PORT 9999
#define BACKLOG 2
#define MAX_CLIENTS 2
#define BUFFER_SIZE 2048 // 增大缓冲区防溢出

typedef struct {
    SOCKET fd;
    int is_red;      // 是否红方
    int authenticated; // 预留字段
    char recv_buf[BUFFER_SIZE];
    int recv_len;
    char send_buf[BUFFER_SIZE];
    int send_len;
} client_t;

// 声明全局变量（在 main.c 中定义）
extern client_t clients[MAX_CLIENTS];

// 函数声明
void server_init(void);
void server_deinit(void);
int event_loop(void (*on_message)(client_t*, const char*, int));

#endif
