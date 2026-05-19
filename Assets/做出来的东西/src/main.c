#include "server.h"
#include "game.h"
#include "protocol.h"
#include <stdio.h>
#include <string.h>

// 定义全局变量，供其他文件使用
client_t clients[MAX_CLIENTS];
game_t game;

// 消息处理回调函数
static void on_message(client_t *c, const char *buf, int len) {
    int msg_type = 0;
    char out[128];
    int out_len = 0;

    if (parse_message(buf, len, &msg_type, NULL) != 0) {
        return;
    }

    switch (msg_type) {
        case MSG_TYPE_MOVE: {
            msg_move_t move;
            if (parse_message(buf, len, &msg_type, &move) != 0) {
                return;
            }
            
            // 检查是否轮到该玩家
            if (c->is_red != game.red_turn) {
                build_result_msg(out, &out_len, 0, -1, game.red_turn);
                break;
            }

            int eated_id = -1;
            int ok = game_move(&game, move.chess_id, move.to_log_x, move.to_log_y, &eated_id);
            build_result_msg(out, &out_len, ok == 0, eated_id, game.red_turn);

            // 如果走子成功且游戏结束，广播游戏结束消息
            if (ok == 0 && game.game_over) {
                char over_buf[64];
                int over_len = 0;
                build_game_over_msg(over_buf, &over_len, game.winner_is_red);
                
                // 广播给所有人
                for (int i = 0; i < MAX_CLIENTS; ++i) {
                    if (clients[i].fd != INVALID_SOCKET) {
                        // 简单处理：如果发送缓冲区够大
                        if (clients[i].send_len + over_len < BUFFER_SIZE) {
                            memcpy(clients[i].send_buf + clients[i].send_len, over_buf, over_len);
                            clients[i].send_len += over_len;
                        }
                    }
                }
            }
            break;
        }
        default:
            return;
    }

    // 将结果发回给当前客户端
   if (out_len > 0 && c->send_len + 4 + out_len < BUFFER_SIZE) {
    // 1. 先写入消息长度
    memcpy(c->send_buf + c->send_len, &out_len, 4);
    c->send_len += 4;
    
    // 2. 再写入消息体
    memcpy(c->send_buf + c->send_len, out, out_len);
    c->send_len += out_len;
}

}

int main(void) {
    // 初始化全局客户端数组
    for (int i = 0; i < MAX_CLIENTS; ++i) {
        clients[i].fd = INVALID_SOCKET;
        clients[i].is_red = (i == 0); // 第一个连接的是红方
        clients[i].recv_len = 0;
        clients[i].send_len = 0;
    }

    server_init();
    game_init(&game);

    printf("Server started on port %d\n", PORT);
    printf("Waiting for Red and Black players...\n");

    // 主循环
    while (1) {
        event_loop(on_message);
    }

    server_deinit();
    return 0;
}
