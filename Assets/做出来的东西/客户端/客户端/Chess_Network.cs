//using UnityEngine;
//using System;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq; // 新增：引入 Jobject 和 JToken 替代 dynamic
//using WebSocketSharp;

//public class ChessNetwork : MonoBehaviour
//{
//    // 单例模式 
//    public static ChessNetwork Instance { get; private set; }

//    private WebSocket _ws;
//    private string _serverUrl = "ws://10.21.22.131/chess"; // 服务端地址 

//    // 回调定义（用于通知游戏逻辑） 
//    public Action OnLoginSuccess;
//    public Action<string> OnError;
//    public Action<int, int, int, int> OnMoveChess; // chessId, targetX, targetY, killId 
//    public Action<bool> OnTurnChange;

//    private void Awake()
//    {
//        if (Instance == null)
//        {
//            Instance = this;
//            DontDestroyOnLoad(gameObject);
//        }
//        else
//        {
//            Destroy(gameObject);
//        }
//        Connect();
//    }

//    // 连接服务器 
//    private void Connect()
//    {
//        _ws = new WebSocket(_serverUrl);
//        _ws.OnOpen += (sender, e) =>
//        {
//            Debug.Log("连接服务器成功");
//        };
//        _ws.OnMessage += (sender, e) =>
//        {
//            HandleServerMessage(e.Data);
//        };
//        _ws.OnError += (sender, e) =>
//        {
//            Debug.LogError("网络错误: " + e.Message);
//            OnError?.Invoke(e.Message);
//        };
//        _ws.OnClose += (sender, e) =>
//        {
//            Debug.Log("连接关闭: " + e.Reason);
//            // 重连逻辑（可选） 
//            Invoke(nameof(Connect), 3f);
//        };
//        _ws.Connect();
//    }

//    // 处理服务器消息 
//    private void HandleServerMessage(string data)
//    {
//        try
//        {
//            // dynamic msg = JsonConvert.DeserializeObject(data); 
//            // string type = msg.type; 
//            JObject msg = JObject.Parse(data);
//            string type = msg["type"]?.ToString();

//            switch (type)
//            {
//                case "login":
//                    OnLoginSuccess?.Invoke();
//                    break;
//                case "move":
//                    // int chessId = msg.data.chessId; 
//                    // int targetX = msg.data.targetX; 
//                    // int targetY = msg.data.targetY; 
//                    // int killId = msg.data.killId; 
//                    JToken moveData = msg["data"];
//                    int chessId = moveData["chessId"].Value<int>();
//                    int targetX = moveData["targetX"].Value<int>();
//                    int targetY = moveData["targetY"].Value<int>();
//                    int killId = moveData["killId"].Value<int>();
//                    OnMoveChess?.Invoke(chessId, targetX, targetY, killId);
//                    break;
//                case "turn":
//                    // bool isRedTurn = msg.data.isRedTurn; 
//                    JToken turnData = msg["data"];
//                    bool isRedTurn = turnData["isRedTurn"].Value<bool>();
//                    OnTurnChange?.Invoke(isRedTurn);
//                    break;
//                case "error":
//                    // OnError?.Invoke(msg.msg); 
//                    OnError?.Invoke(msg["msg"]?.ToString());
//                    break;
//            }
//        }
//        catch (Exception ex)
//        {
//            Debug.LogError("解析消息失败: " + ex.Message);
//        }
//    }

//    // 发送登录请求 
//    public void SendLogin(string userId, string roomId)
//    {
//        var msg = new { type = "login", userId = userId, roomId = roomId };
//        SendMessage(JsonConvert.SerializeObject(msg));
//    }

//    // 发送移动棋子请求（改造原 SendMove 方法） 
//    public void SendMove(int chessId, int targetLogX, int targetLogY, int killId = -1)
//    {
//        var msg = new { type = "move", chessId = chessId, targetX = targetLogX, targetY = targetLogY, killId = killId };
//        SendMessage(JsonConvert.SerializeObject(msg));
//    }

//    // 发送回合切换请求 
//    public void SendTurnChange(bool isRedTurn)
//    {
//        var msg = new { type = "turn", isRedTurn = isRedTurn };
//        SendMessage(JsonConvert.SerializeObject(msg));
//    }

//    // 通用发送消息方法 
//    private void SendMessage(string data)
//    {
//        if (_ws != null && _ws.IsAlive)
//        {
//            _ws.Send(data);
//        }
//        else
//        {
//            Debug.LogError("未连接服务器，无法发送消息");
//        }
//    }

//    private void OnDestroy()
//    {
//        _ws?.Close();
//    }
//}
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

public class ChessNetwork : MonoBehaviour
{
    public static ChessNetwork Instance { get; private set; }

    // 服务器地址和端口，必须与 C 服务器一致
    private const string SERVER_IP = "127.0.0.1"; // 如果服务器在本机，用 127.0.0.1；如果是另一台电脑，用其局域网IP
    private const int SERVER_PORT = 9999;

    private TcpClient _tcpClient;
    private NetworkStream _stream;
    private Thread _receiveThread;
    private bool _isConnected = false;
    private Queue<Action> _mainThreadQueue = new Queue<Action>(); // 用于将网络线程的事件调度到主线程

    // 回调事件
    public Action<int, int, int> OnMoveResult; // success, eated_id, next_turn_is_red
    public Action<int> OnGameOver; // winner_is_red

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ConnectToServer();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // 在主线程中处理队列里的回调
        lock (_mainThreadQueue)
        {
            while (_mainThreadQueue.Count > 0)
            {
                _mainThreadQueue.Dequeue()?.Invoke();
            }
        }
    }

    private void ConnectToServer()
    {
        try
        {
            _tcpClient = new TcpClient(SERVER_IP, SERVER_PORT);
            _stream = _tcpClient.GetStream();
            _isConnected = true;
            Debug.Log("已连接到服务器");

            _receiveThread = new Thread(ReceiveLoop);
            _receiveThread.IsBackground = true;
            _receiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.LogError("连接服务器失败: " + e.Message);
        }
    }

    // 发送移动请求 (对应 C 服务端的 MSG_TYPE_MOVE)
    public void SendMove(int chessId, int toLogX, int toLogY)
    {
        if (!_isConnected) return;

        // 构建 msg_move_t 二进制包
        // 协议定义: int msg_type, int chess_id, int to_log_x, int to_log_y
        byte[] data = new byte[16];
        BitConverter.GetBytes(1).CopyTo(data, 0);      // MSG_TYPE_MOVE = 1
        BitConverter.GetBytes(chessId).CopyTo(data, 4);
        BitConverter.GetBytes(toLogX).CopyTo(data, 8);
        BitConverter.GetBytes(toLogY).CopyTo(data, 12);

        // 发送：长度头(4字节) + 数据体
        byte[] lengthPrefix = BitConverter.GetBytes(data.Length);
        _stream.Write(lengthPrefix, 0, 4);
        _stream.Write(data, 0, data.Length);
        Debug.Log($"发送移动请求: ID={chessId}, ({toLogX},{toLogY})");
    }

    // 接收线程：持续从服务器读取数据
    private void ReceiveLoop()
    {
        byte[] lengthBuffer = new byte[4];
        while (_isConnected && _tcpClient != null && _tcpClient.Connected)
        {
            try
            {
                // 1. 读取消息长度
                if (_stream.Read(lengthBuffer, 0, 4) != 4) break;
                int msgLen = BitConverter.ToInt32(lengthBuffer, 0);

                // 2. 读取消息体
                byte[] msgBuffer = new byte[msgLen];
                int bytesRead = 0;
                while (bytesRead < msgLen)
                {
                    int read = _stream.Read(msgBuffer, bytesRead, msgLen - bytesRead);
                    if (read == 0) break;
                    bytesRead += read;
                }

                // 3. 解析消息 (根据 protocol.h)
                int msgType = BitConverter.ToInt32(msgBuffer, 0);

                // 将解析结果放入队列，在主线程的 Update 中调用
                if (msgType == 2) // MSG_TYPE_RESULT
                {
                    int success = BitConverter.ToInt32(msgBuffer, 4);
                    int eatedId = BitConverter.ToInt32(msgBuffer, 8);
                    int nextTurnIsRed = BitConverter.ToInt32(msgBuffer, 12);

                    lock (_mainThreadQueue)
                    {
                        _mainThreadQueue.Enqueue(() => OnMoveResult?.Invoke(success, eatedId, nextTurnIsRed));
                    }
                }
                else if (msgType == 3) // MSG_TYPE_GAME_OVER
                {
                    int winnerIsRed = BitConverter.ToInt32(msgBuffer, 4);
                    lock (_mainThreadQueue)
                    {
                        _mainThreadQueue.Enqueue(() => OnGameOver?.Invoke(winnerIsRed));
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("接收数据错误: " + e.Message);
                break;
            }
        }
        _isConnected = false;
    }

    private void OnDestroy()
    {
        _isConnected = false;
        _receiveThread?.Join();
        _stream?.Close();
        _tcpClient?.Close();
    }
}
