// main.c
#include "server.h"
#include "game.h"
#include "protocol.h"
#include <stdio.h>
#include <string.h>
#include <stdlib.h>

room_t rooms[MAX_ROOMS];
int room_count = 0;

static void on_message(client_t *c, const char *buf, int len) {
    int msg_type = 0;
    char out[128];
    int out_len = 0;
    
    if (parse_message(buf, len, &msg_type, NULL) != 0) {
        return;
    }
    
    room_t *room = &rooms[c->room_index];
    game_t *game = room->game;
    
    switch (msg_type) {
        case MSG_TYPE_MOVE: {
            msg_move_t move;
            if (parse_message(buf, len, &msg_type, &move) != 0) {
                return;
            }
            if (c->is_red != game->red_turn) {
                build_result_msg(out, &out_len, 0, -1, game->red_turn);
                break;
            }
            int eated_id = -1;
            int ok = game_move(game, move.chess_id, move.to_log_x, move.to_log_y, &eated_id);
            build_result_msg(out, &out_len, ok == 0, eated_id, game->red_turn);
            
            if (ok == 0 && game->game_over) {
                char over_buf[64];
                int over_len = 0;
                build_game_over_msg(over_buf, &over_len, game->winner_is_red);
                for (int i = 0; i < MAX_CLIENTS; ++i) {
                    if (room->clients[i].fd != INVALID_SOCKET) {
                        if (room->clients[i].send_len + over_len < BUFFER_SIZE) {
                            memcpy(room->clients[i].send_buf + room->clients[i].send_len, over_buf, over_len);
                            room->clients[i].send_len += over_len;
                        }
                    }
                }
            }
            break;
        }
        default:
            return;
    }
    
    if (out_len > 0 && c->send_len + 4 + out_len < BUFFER_SIZE) {
        memcpy(c->send_buf + c->send_len, &out_len, 4);
        c->send_len += 4;
        memcpy(c->send_buf + c->send_len, out, out_len);
        c->send_len += out_len;
    }
}

int main(void) {
    for (int i = 0; i < MAX_ROOMS; ++i) {
        rooms[i].active = 0;
        rooms[i].client_count = 0;
        rooms[i].game = NULL;
    }
    
    server_init();
    printf("Server started on port %d\n", PORT);
    printf("Maximum concurrent rooms: %d\n", MAX_ROOMS);
    printf("Waiting for players...\n");
    
    while (1) {
        event_loop(on_message);
    }
    
    server_deinit();
    return 0;
}
