#include "rule.h"
#include <math.h>

int rule_relation(int log_x1, int log_y1, int log_x2, int log_y2) {
    return abs(log_y1 - log_y2) * 10 + abs(log_x1 - log_x2);
}

int rule_count_line_chess(game_t *g, int log_x1, int log_y1, int log_x2, int log_y2) {
    if (log_x1 != log_x2 && log_y1 != log_y2) return -1;

    int count = 0;
    if (log_x1 == log_x2) {
        int dir = (log_y2 > log_y1) ? 1 : -1;
        for (int y = log_y1 + dir; y != log_y2; y += dir) {
            if (rule_get_chess_id(g, log_x1, y) != -1) ++count;
        }
    } else {
        int dir = (log_x2 > log_x1) ? 1 : -1;
        for (int x = log_x1 + dir; x != log_x2; x += dir) {
            if (rule_get_chess_id(g, x, log_y1) != -1) ++count;
        }
    }
    return count;
}

int rule_get_chess_id(game_t *g, int log_x, int log_y) {
    for (int i = 0; i < CHESS_NUM; ++i) {
        if (g->chess[i].is_dead) continue;
        if (g->chess[i].log_x == log_x && g->chess[i].log_y == log_y) return i;
    }
    return -1;
}

int rule_ju(game_t *g, int chess_id, int to_log_y, int to_log_x, int target_id) {
    chess_t *c = &g->chess[chess_id];
    int from_log_x = c->log_x;
    int from_log_y = c->log_y;

    if (from_log_y != to_log_y && from_log_x != to_log_x) return 0;

    int count = rule_count_line_chess(g, from_log_x, from_log_y, to_log_x, to_log_y);
    return count == 0;
}

int rule_ma(game_t *g, int chess_id, int to_log_y, int to_log_x, int target_id) {
    chess_t *c = &g->chess[chess_id];
    int from_log_x = c->log_x;
    int from_log_y = c->log_y;

    int dy = abs(from_log_y - to_log_y);
    int dx = abs(from_log_x - to_log_x);

    if (dy == 2 && dx == 1) {
        int mid_y = (from_log_y + to_log_y) / 2;
        if (rule_get_chess_id(g, from_log_x, mid_y) != -1) return 0;
        return 1;
    }

    if (dy == 1 && dx == 2) {
        int mid_x = (from_log_x + to_log_x) / 2;
        if (rule_get_chess_id(g, mid_x, from_log_y) != -1) return 0;
        return 1;
    }

    return 0;
}

int rule_pao(game_t *g, int chess_id, int to_log_y, int to_log_x, int target_id) {
    chess_t *c = &g->chess[chess_id];
    int from_log_x = c->log_x;
    int from_log_y = c->log_y;

    if (from_log_y != to_log_y && from_log_x != to_log_x) return 0;

    int count = rule_count_line_chess(g, from_log_x, from_log_y, to_log_x, to_log_y);

    if (target_id == -1) {
        return count == 0;
    } else {
        return count == 1;
    }
}

int rule_xiang(game_t *g, int chess_id, int to_log_y, int to_log_x, int target_id) {
    chess_t *c = &g->chess[chess_id];
    int from_log_x = c->log_x;
    int from_log_y = c->log_y;

    int rel = rule_relation(from_log_x, from_log_y, to_log_x, to_log_y);
    if (rel != 22) return 0;

    int eye_log_x = (from_log_x + to_log_x) / 2;
    int eye_log_y = (from_log_y + to_log_y) / 2;
    if (rule_get_chess_id(g, eye_log_x, eye_log_y) != -1) return 0;

    if (!c->is_red) {
        if (to_log_y < 5) return 0;
    } else {
        if (to_log_y > 4) return 0;
    }

    return 1;
}

int rule_shi(game_t *g, int chess_id, int to_log_y, int to_log_x, int target_id) {
    chess_t *c = &g->chess[chess_id];
    int from_log_x = c->log_x;
    int from_log_y = c->log_y;

    if (!c->is_red) {
        if (to_log_y < 7 || to_log_y > 9) return 0;
    } else {
        if (to_log_y < 0 || to_log_y > 2) return 0;
    }

    if (to_log_x < 3 || to_log_x > 5) return 0;

    int rel = rule_relation(from_log_x, from_log_y, to_log_x, to_log_y);
    if (rel != 11) return 0;

    return 1;
}

int rule_shuai(game_t *g, int chess_id, int to_log_y, int to_log_x, int target_id) {
    chess_t *c = &g->chess[chess_id];
    int from_log_x = c->log_x;
    int from_log_y = c->log_y;

    if (target_id != -1 && g->chess[target_id].type == CHESS_TYPE_SHUAI) {
        return rule_ju(g, chess_id, to_log_y, to_log_x, target_id);
    }

    if (to_log_x < 3 || to_log_x > 5) return 0;

    if (!c->is_red) {
        if (to_log_y < 7 || to_log_y > 9) return 0;
    } else {
        if (to_log_y < 0 || to_log_y > 2) return 0;
    }

    int rel = rule_relation(from_log_x, from_log_y, to_log_x, to_log_y);
    if (rel != 1 && rel != 10) return 0;

    return 1;
}

int rule_bing(game_t *g, int chess_id, int to_log_y, int to_log_x, int target_id) {
    chess_t *c = &g->chess[chess_id];
    int from_log_x = c->log_x;
    int from_log_y = c->log_y;

    int rel = rule_relation(from_log_x, from_log_y, to_log_x, to_log_y);
    if (rel != 1 && rel != 10) return 0;

    if (!c->is_red) {
        if (to_log_y > from_log_y) return 0;
        if (from_log_y > 4 && from_log_x != to_log_x) return 0;
    } else {
        if (to_log_y < from_log_y) return 0;
        if (from_log_y < 5 && from_log_x != to_log_x) return 0;
    }

    return 1;
}
