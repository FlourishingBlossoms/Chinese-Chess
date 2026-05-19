#include "game.h"
#include "rule.h"
#include <string.h>

static const int init_log_x[CHESS_NUM] = {
    0, 1, 2, 3, 4, 5, 6, 7, 8,
    1, 7,
    0, 2, 4, 6, 8
};

static const int init_log_y[CHESS_NUM] = {
    0, 0, 0, 0, 0, 0, 0, 0, 0,
    2, 2,
    3, 3, 3, 3, 3
};

static const chess_type_t init_type[CHESS_NUM] = {
    CHESS_TYPE_JU, CHESS_TYPE_MA, CHESS_TYPE_XIANG, CHESS_TYPE_SHI, CHESS_TYPE_SHUAI,
    CHESS_TYPE_SHI, CHESS_TYPE_XIANG, CHESS_TYPE_MA, CHESS_TYPE_JU,
    CHESS_TYPE_PAO, CHESS_TYPE_PAO,
    CHESS_TYPE_BING, CHESS_TYPE_BING, CHESS_TYPE_BING, CHESS_TYPE_BING, CHESS_TYPE_BING
};

void game_init(game_t *g) {
    memset(g, 0, sizeof(*g));
    g->red_turn = 1;

    for (int i = 0; i < 16; ++i) {
        chess_t *c = &g->chess[i];
        c->id = i;
        c->is_red = 1;
        c->log_x = init_log_x[i];
        c->log_y = init_log_y[i];
        c->type = init_type[i];
        c->is_dead = 0;
    }

    for (int i = 16; i < CHESS_NUM; ++i) {
        chess_t *c = &g->chess[i];
        c->id = i;
        c->is_red = 0;
        c->log_x = init_log_x[i - 16];
        c->log_y = 9 - init_log_y[i - 16];
        c->type = init_type[i - 16];
        c->is_dead = 0;
    }
}

static int find_chess_at(game_t *g, int log_x, int log_y) {
    for (int i = 0; i < CHESS_NUM; ++i) {
        if (g->chess[i].is_dead) continue;
        if (g->chess[i].log_x == log_x && g->chess[i].log_y == log_y) {
            return i;
        }
    }
    return -1;
}

int game_move(game_t *g, int chess_id, int to_log_x, int to_log_y, int *eated_id) {
    if (chess_id < 0 || chess_id >= CHESS_NUM) return -1;
    if (g->game_over) return -1;

    int legal = game_is_legal_move(g, chess_id, to_log_x, to_log_y, eated_id);
    if (!legal) return -1;

    chess_t *c = &g->chess[chess_id];
    if (c->is_dead) return -1;

    int eated = find_chess_at(g, to_log_x, to_log_y);
    if (eated >= 0) {
        g->chess[eated].is_dead = 1;
        if (eated_id) *eated_id = eated;
        if (g->chess[eated].type == CHESS_TYPE_SHUAI) {
            g->game_over = 1;
            g->winner_is_red = !g->chess[eated].is_red;
        }
    } else {
        if (eated_id) *eated_id = -1;
    }

    c->log_x = to_log_x;
    c->log_y = to_log_y;

    g->red_turn = !g->red_turn;

    return 0;
}

int game_is_legal_move(game_t *g, int chess_id, int to_log_x, int to_log_y, int *eated_id) {
    if (chess_id < 0 || chess_id >= CHESS_NUM) return 0;
    chess_t *c = &g->chess[chess_id];
    if (c->is_dead) return 0;

    int from_log_x = c->log_x;
    int from_log_y = c->log_y;

    if (from_log_x == to_log_x && from_log_y == to_log_y) return 0;

    int target_id = find_chess_at(g, to_log_x, to_log_y);
    if (target_id >= 0 && g->chess[target_id].is_red == c->is_red) return 0;

    int relation = rule_relation(from_log_x, from_log_y, to_log_x, to_log_y);

    switch (c->type) {
        case CHESS_TYPE_JU:
            return rule_ju(g, chess_id, to_log_y, to_log_x, target_id);
        case CHESS_TYPE_MA:
            return rule_ma(g, chess_id, to_log_y, to_log_x, target_id);
        case CHESS_TYPE_PAO:
            return rule_pao(g, chess_id, to_log_y, to_log_x, target_id);
        case CHESS_TYPE_XIANG:
            return rule_xiang(g, chess_id, to_log_y, to_log_x, target_id);
        case CHESS_TYPE_SHI:
            return rule_shi(g, chess_id, to_log_y, to_log_x, target_id);
        case CHESS_TYPE_SHUAI:
            return rule_shuai(g, chess_id, to_log_y, to_log_x, target_id);
        case CHESS_TYPE_BING:
            return rule_bing(g, chess_id, to_log_y, to_log_x, target_id);
        default:
            return 0;
    }
}
