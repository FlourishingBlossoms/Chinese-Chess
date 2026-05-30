#ifndef PROTOCOL_H
#define PROTOCOL_H

#define MSG_TYPE_MOVE       1   // 客户端发送：移动棋子
#define MSG_TYPE_RESULT     2   // 服务端返回：移动结果
#define MSG_TYPE_GAME_OVER  3   // 服务端返回：游戏结束

// 移动请求
typedef struct {
    int type;
    int chess_id;
    int to_x;
    int to_y;
} msg_move_t;

// 移动结果
typedef struct {
    int type;
    int success;
    int eated_id;
    int next_turn_is_red;
} msg_result_t;

// 游戏结束
typedef struct {
    int type;
    int winner_is_red;
} msg_game_over_t;

#endif