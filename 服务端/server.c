// server.c
#include "server.h"
#include <stdio.h>
#include <string.h>
#include <stdlib.h>

static SOCKET server_socket = INVALID_SOCKET;

static int init_winsock(void) {
#ifdef _WIN32
    WSADATA wsa;
    if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0) {
        return -1;
    }
#endif
    return 0;
}

static SOCKET create_server_socket(void) {
    SOCKET s;
    struct sockaddr_in addr;
    s = socket(AF_INET, SOCK_STREAM, 0);
    if (s == INVALID_SOCKET) {
        return INVALID_SOCKET;
    }
    int reuse = 1;
    setsockopt(s, SOL_SOCKET, SO_REUSEADDR, (char*)&reuse, sizeof(reuse));
    memset(&addr, 0, sizeof(addr));
    addr.sin_family = AF_INET;
    addr.sin_addr.s_addr = htonl(INADDR_ANY);
    addr.sin_port = htons(PORT);
    if (bind(s, (struct sockaddr*)&addr, sizeof(addr)) == SOCKET_ERROR) {
        closesocket(s);
        return INVALID_SOCKET;
    }
    if (listen(s, BACKLOG) == SOCKET_ERROR) {
        closesocket(s);
        return INVALID_SOCKET;
    }
    return s;
}

static int set_nonblock(SOCKET s) {
#ifdef _WIN32
    u_long mode = 1;
    return ioctlsocket(s, FIONBIO, &mode);
#else
    int flags = fcntl(s, F_GETFL, 0);
    return fcntl(s, F_SETFL, flags | O_NONBLOCK);
#endif
}

void server_init(void) {
    if (init_winsock() != 0) {
        fprintf(stderr, "Failed to init winsock\n");
        exit(1);
    }
    server_socket = create_server_socket();
    if (server_socket == INVALID_SOCKET) {
        fprintf(stderr, "Failed to create server socket\n");
        exit(1);
    }
    if (set_nonblock(server_socket) != 0) {
        fprintf(stderr, "Failed to set nonblock\n");
        exit(1);
    }
}

void server_deinit(void) {
    if (server_socket != INVALID_SOCKET) {
        closesocket(server_socket);
        server_socket = INVALID_SOCKET;
    }
#ifdef _WIN32
    WSACleanup();
#endif
}

static int find_available_room() {
    for (int i = 0; i < room_count; ++i) {
        if (!rooms[i].active) {
            rooms[i].active = 1;
            rooms[i].client_count = 0;
            game_init(rooms[i].game);
            for (int j = 0; j < MAX_CLIENTS; ++j) {
                rooms[i].clients[j].fd = INVALID_SOCKET;
                rooms[i].clients[j].room_index = i;
                rooms[i].clients[j].is_red = (j == 0);
                rooms[i].clients[j].recv_len = 0;
                rooms[i].clients[j].send_len = 0;
            }
            return i;
        }
        if (rooms[i].client_count < MAX_CLIENTS && rooms[i].active) {
            return i;
        }
    }
    if (room_count < MAX_ROOMS) {
        int new_room = room_count++;
        rooms[new_room].active = 1;
        rooms[new_room].client_count = 0;
        rooms[new_room].game = (game_t*)malloc(sizeof(game_t));
        game_init(rooms[new_room].game);
        for (int j = 0; j < MAX_CLIENTS; ++j) {
            rooms[new_room].clients[j].fd = INVALID_SOCKET;
            rooms[new_room].clients[j].room_index = new_room;
            rooms[new_room].clients[j].is_red = (j == 0);
            rooms[new_room].clients[j].recv_len = 0;
            rooms[new_room].clients[j].send_len = 0;
        }
        return new_room;
    }
    return -1;
}

static int do_accept() {
    struct sockaddr_in addr;
    int addr_len = sizeof(addr);
    SOCKET client_socket = accept(server_socket, (struct sockaddr*)&addr, &addr_len);
    if (client_socket == INVALID_SOCKET) {
        return -1;
    }
    if (set_nonblock(client_socket) != 0) {
        closesocket(client_socket);
        return -1;
    }
    
    int room_idx = find_available_room();
    if (room_idx == -1) {
        printf("Server full, rejecting connection.\n");
        closesocket(client_socket);
        return -1;
    }
    
    room_t *room = &rooms[room_idx];
    for (int i = 0; i < MAX_CLIENTS; ++i) {
        if (room->clients[i].fd == INVALID_SOCKET) {
            room->clients[i].fd = client_socket;
            room->clients[i].recv_len = 0;
            room->clients[i].send_len = 0;
            room->client_count++;
            printf("Client %d connected to room %d (IsRed: %d)\n", i, room_idx, room->clients[i].is_red);
            return 0;
        }
    }
    closesocket(client_socket);
    return -1;
}

static int do_recv(client_t *c, void (*on_message)(client_t*, const char*, int)) {
    char buf[BUFFER_SIZE];
    int n = recv(c->fd, buf, BUFFER_SIZE, 0);
    if (n <= 0) {
        return -1;
    }
    if (c->recv_len + n > BUFFER_SIZE) {
        return -1;
    }
    memcpy(c->recv_buf + c->recv_len, buf, n);
    c->recv_len += n;
    char *start = c->recv_buf;
    int remain = c->recv_len;
    
    while (remain >= 4) {
        int len = 0;
        memcpy(&len, start, 4);
        if (len <= 0 || len > BUFFER_SIZE) {
            return -1;
        }
        if (remain < 4 + len) {
            break;
        }
        if (on_message) {
            on_message(c, start + 4, len);
        }
        start += 4 + len;
        remain -= 4 + len;
    }
    if (remain > 0 && start != c->recv_buf) {
        memmove(c->recv_buf, start, remain);
    }
    c->recv_len = remain;
    return 0;
}

static int do_send(client_t *c) {
    if (c->send_len <= 0) {
        return 0;
    }
    int n = send(c->fd, c->send_buf, c->send_len, 0);
    if (n <= 0) {
        return -1;
    }
    if (n < c->send_len) {
        memmove(c->send_buf, c->send_buf + n, c->send_len - n);
    }
    c->send_len -= n;
    return 0;
}

int event_loop(void (*on_message)(client_t*, const char*, int)) {
    fd_set read_fds, write_fds;
    int max_fd = -1;
    FD_ZERO(&read_fds);
    FD_ZERO(&write_fds);
    FD_SET(server_socket, &read_fds);
    if (server_socket > max_fd) max_fd = server_socket;
    
    for (int i = 0; i < room_count; ++i) {
        room_t *room = &rooms[i];
        if (!room->active) continue;
        
        for (int j = 0; j < MAX_CLIENTS; ++j) {
            if (room->clients[j].fd != INVALID_SOCKET) {
                FD_SET(room->clients[j].fd, &read_fds);
                if (room->clients[j].send_len > 0) {
                    FD_SET(room->clients[j].fd, &write_fds);
                }
                if (room->clients[j].fd > max_fd) max_fd = room->clients[j].fd;
            }
        }
    }
    
    struct timeval tv;
    tv.tv_sec = 0;
    tv.tv_usec = 200000;
    int n = select(max_fd + 1, &read_fds, &write_fds, NULL, &tv);
    if (n < 0) {
        return -1;
    }
    
    if (FD_ISSET(server_socket, &read_fds)) {
        do_accept();
    }
    
    for (int i = 0; i < room_count; ++i) {
        room_t *room = &rooms[i];
        if (!room->active) continue;
        
        for (int j = 0; j < MAX_CLIENTS; ++j) {
            client_t *client = &room->clients[j];
            if (client->fd == INVALID_SOCKET) continue;
            
            if (FD_ISSET(client->fd, &read_fds)) {
                if (do_recv(client, on_message) != 0) {
                    printf("Client %d in room %d disconnected.\n", j, i);
                    closesocket(client->fd);
                    client->fd = INVALID_SOCKET;
                    room->client_count--;
                    if (room->client_count == 0) {
                        room->active = 0;
                    }
                    continue;
                }
            }
            
            if (FD_ISSET(client->fd, &write_fds)) {
                if (do_send(client) != 0) {
                    printf("Client %d in room %d send error.\n", j, i);
                    closesocket(client->fd);
                    client->fd = INVALID_SOCKET;
                    room->client_count--;
                    if (room->client_count == 0) {
                        room->active = 0;
                    }
                }
            }
        }
    }
    return 0;
}
