#include <iostream>
#include <cstring>
#include <string>
#include <thread>
#include <atomic>

// Windows 网络库
#ifdef _WIN32
#include <winsock2.h>
#include <ws2tcpip.h>
#pragma comment(lib, "ws2_32.lib")
typedef int socklen_t;
#else
#include <sys/socket.h>
#include <netinet/in.h>
#include <unistd.h>
#include <arpa/inet.h>
#define closesocket close
typedef int SOCKET;
#define INVALID_SOCKET -1
#endif

#include "protocol.h"

using namespace std;

#define SERVER_IP "10.21.22.131"
#define SERVER_PORT 9999
#define BUF_SIZE 2048

atomic<bool> is_connected(true); // 连接状态

// 初始化网络库（Windows）
bool initWinSocket() {
#ifdef _WIN32
    WSADATA wsa;
    if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0) {
        cout << "WSA 初始化失败" << endl;
        return false;
    }
#endif
    return true;
}

// 连接服务器
SOCKET connectServer() {
    SOCKET sock = socket(AF_INET, SOCK_STREAM, 0);
    if (sock == INVALID_SOCKET) {
        cout << "创建 socket 失败" << endl;
        return INVALID_SOCKET;
    }

    sockaddr_in addr{};
    addr.sin_family = AF_INET;
    addr.sin_port = htons(SERVER_PORT);
    inet_pton(AF_INET, SERVER_IP, &addr.sin_addr);

    if (connect(sock, (sockaddr*)&addr, sizeof(addr)) < 0) {
        cout << "连接服务器失败！" << endl;
        closesocket(sock);
        return INVALID_SOCKET;
    }

    cout << "连接服务器成功！" << endl;
    return sock;
}

// 发送消息（长度 + 数据）
bool sendMsg(SOCKET sock, const char* data, int len) {
    int net_len = htonl(len);
    if (send(sock, (char*)&net_len, 4, 0) != 4) return false;
    if (send(sock, data, len, 0) != len) return false;
    return true;
}

// 接收消息
bool recvMsg(SOCKET sock, char* buf, int& out_len) {
    int net_len;
    if (recv(sock, (char*)&net_len, 4, 0) <= 0) return false;
    int len = ntohl(net_len);

    if (len <= 0 || len > BUF_SIZE) return false;
    if (recv(sock, buf, len, 0) != len) return false;

    out_len = len;
    return true;
}

// 解析服务端消息
void parseMessage(const char* buf, int len) {
    int type;
    memcpy(&type, buf, 4);

    switch (type) {
    case MSG_TYPE_RESULT: {
        msg_result_t res;
        memcpy(&res, buf, sizeof(res));
        cout << "\n==== 移动结果 ====" << endl;
        cout << "成功：" << (res.success ? "是" : "否") << endl;
        cout << "被吃棋子：" << res.eated_id << endl;
        cout << "下一轮红方：" << (res.next_turn_is_red ? "是" : "否") << endl;
        break;
    }
    case MSG_TYPE_GAME_OVER: {
        msg_game_over_t over;
        memcpy(&over, buf, sizeof(over));
        cout << "\n 游戏结束！胜者：" << (over.winner_is_red ? "红方" : "黑方") << endl;
        is_connected = false;
        break;
    }
    default:
        cout << "未知消息类型：" << type << endl;
    }
}

// 接收线程：持续监听服务器消息
void recvThread(SOCKET sock) {
    char buf[BUF_SIZE];
    int len;
    while (is_connected) {
        if (!recvMsg(sock, buf, len)) {
            cout << "\n 服务器断开连接" << endl;
            is_connected = false;
            break;
        }
        parseMessage(buf, len);
    }
}

// 发送移动指令
void sendMove(SOCKET sock, int id, int x, int y) {
    msg_move_t msg{};
    msg.type = MSG_TYPE_MOVE;
    msg.chess_id = id;
    msg.to_x = x;
    msg.to_y = y;

    if (sendMsg(sock, (char*)&msg, sizeof(msg))) {
        cout << " 已发送：棋子" << id << " → (" << x << "," << y << ")" << endl;
    }
    else {
        cout << "发送失败！" << endl;
    }
}

int main() {
    if (!initWinSocket()) return -1;

    SOCKET sock = connectServer();
    if (sock == INVALID_SOCKET) return -1;

    // 启动接收线程
    thread t(recvThread, sock);
    t.detach();

    // 主线程输入指令
    int id, x, y;
    while (is_connected) {
        cout << "\n 请输入 棋子ID 目标X 目标Y（空格分隔）：";
        cin >> id >> x >> y;

        if (!is_connected) break;
        sendMove(sock, id, x, y);
    }

    closesocket(sock);
#ifdef _WIN32
    WSACleanup();
#endif
    return 0;
}