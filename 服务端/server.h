// server.h
#ifndef SERVER_H
#define SERVER_H

#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#include <winsock2.h>
#include <ws2tcpip.h>
#pragma comment(lib, "ws2_32.lib")
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
#include "game.h"

#define PORT 9999
#define BACKLOG 2
#define MAX_CLIENTS 2
#define BUFFER_SIZE 2048
#define MAX_ROOMS 10

typedef struct {
    SOCKET fd;
    int is_red;
    int authenticated;
    char recv_buf[BUFFER_SIZE];
    int recv_len;
    char send_buf[BUFFER_SIZE];
    int send_len;
    int room_index;
} client_t;

typedef struct {
    game_t *game;
    client_t clients[MAX_CLIENTS];
    int client_count;
    int active;
} room_t;

extern room_t rooms[MAX_ROOMS];
extern int room_count;

void server_init(void);
void server_deinit(void);
int event_loop(void (*on_message)(client_t*, const char*, int));

#endif
