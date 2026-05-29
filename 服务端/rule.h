#ifndef RULE_H
#define RULE_H

#include "game.h"

int rule_relation(int log_x1, int log_y1, int log_x2, int log_y2);
int rule_count_line_chess(game_t *g, int log_x1, int log_y1, int log_x2, int log_y2);
int rule_get_chess_id(game_t *g, int log_x, int log_y);

int rule_ju(game_t *g, int chess_id, int to_log_y, int to_log_x, int target_id);
int rule_ma(game_t *g, int chess_id, int to_log_y, int to_log_x, int target_id);
int rule_pao(game_t *g, int chess_id, int to_log_y, int to_log_x, int target_id);
int rule_xiang(game_t *g, int chess_id, int to_log_y, int to_log_x, int target_id);
int rule_shi(game_t *g, int chess_id, int to_log_y, int to_log_x, int target_id);
int rule_shuai(game_t *g, int chess_id, int to_log_y, int to_log_x, int target_id);
int rule_bing(game_t *g, int chess_id, int to_log_y, int to_log_x, int target_id);

#endif
