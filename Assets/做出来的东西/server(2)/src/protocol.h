#ifndef PROTOCOL_H
#define PROTOCOL_H

#define MSG_TYPE_MOVE      1
#define MSG_TYPE_RESULT    2
#define MSG_TYPE_GAME_OVER 3

#pragma pack(push, 1)

typedef struct {
    int msg_type;
    int chess_id;
    int to_log_x;
    int to_log_y;
} msg_move_t;

typedef struct {
    int msg_type;
    int success;
    int eated_id;
    int next_turn_is_red;
} msg_result_t;

typedef struct {
    int msg_type;
    int winner_is_red;
} msg_game_over_t;

#pragma pack(pop)

int parse_message(const char *buf, int len, int *msg_type, void *out);
void build_move_msg(char *buf, int *len, int chess_id, int to_log_x, int to_log_y);
void build_result_msg(char *buf, int *len, int success, int eated_id, int next_turn_is_red);
void build_game_over_msg(char *buf, int *len, int winner_is_red);

#endif
