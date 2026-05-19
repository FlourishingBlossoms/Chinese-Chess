#include "protocol.h"
#include <string.h>

int parse_message(const char *buf, int len, int *msg_type, void *out) {
    if (len < 4) return -1;
    int type = 0;
    memcpy(&type, buf, 4);
    *msg_type = type;

    switch (type) {
        case MSG_TYPE_MOVE: {
            if (len < sizeof(msg_move_t)) return -1;
            memcpy(out, buf, sizeof(msg_move_t));
            break;
        }
        default:
            return -1;
    }
    return 0;
}

void build_move_msg(char *buf, int *len, int chess_id, int to_log_x, int to_log_y) {
    msg_move_t msg;
    msg.msg_type = MSG_TYPE_MOVE;
    msg.chess_id = chess_id;
    msg.to_log_x = to_log_x;
    msg.to_log_y = to_log_y;
    memcpy(buf, &msg, sizeof(msg));
    *len = sizeof(msg);
}

void build_result_msg(char *buf, int *len, int success, int eated_id, int next_turn_is_red) {
    msg_result_t msg;
    msg.msg_type = MSG_TYPE_RESULT;
    msg.success = success;
    msg.eated_id = eated_id;
    msg.next_turn_is_red = next_turn_is_red;
    memcpy(buf, &msg, sizeof(msg));
    *len = sizeof(msg);
}

void build_game_over_msg(char *buf, int *len, int winner_is_red) {
    msg_game_over_t msg;
    msg.msg_type = MSG_TYPE_GAME_OVER;
    msg.winner_is_red = winner_is_red;
    memcpy(buf, &msg, sizeof(msg));
    *len = sizeof(msg);
}
