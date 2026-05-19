#ifndef GAME_H
#define GAME_H

#define BOARD_COLS 9
#define BOARD_ROWS 10
#define CHESS_NUM  32

typedef enum {
    CHESS_TYPE_JU = 0,
    CHESS_TYPE_MA,
    CHESS_TYPE_SHI,
    CHESS_TYPE_XIANG,
    CHESS_TYPE_PAO,
    CHESS_TYPE_SHUAI,
    CHESS_TYPE_BING
} chess_type_t;

typedef struct {
    int id;
    int is_red;
    int log_x;
    int log_y;
    chess_type_t type;
    int is_dead;
} chess_t;

typedef struct {
    chess_t chess[CHESS_NUM];
    int red_turn;
    int game_over;
    int winner_is_red;
} game_t;

void game_init(game_t *g);
int game_move(game_t *g, int chess_id, int to_log_x, int to_log_y, int *eated_id);
int game_is_legal_move(game_t *g, int chess_id, int to_log_x, int to_log_y, int *eated_id);

#endif
