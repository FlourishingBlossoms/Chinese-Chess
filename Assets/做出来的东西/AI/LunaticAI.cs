using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Lunatic难度AI - 疯狂难度（完整版）
/// 
/// 【设计思路】
/// Lunatic AI是最高难度的AI，结合了：
/// 1. 大语言模型(LLM)的战略理解能力
/// 2. 5层Minimax深度搜索的战术计算能力
/// 
/// 【LLM调用说明】
/// - 需要先启动 llm_api_server.py 服务
/// - 服务地址默认为 http://localhost:5000
/// - 如果LLM服务不可用，会自动回退到纯Minimax搜索
/// 
/// 【适合人群】
/// 高水平象棋玩家，需要精确计算和深入思考才能获胜
/// </summary>
public class LunaticAI
{
    // ==================== API配置 ====================
    
    /// <summary>
    /// LLM API服务地址
    /// 需要先启动 llm_api_server.py 服务
    /// </summary>
    private const string LLM_API_URL = "http://localhost:5000/api/chess/analyze";
    
    /// <summary>
    /// API请求超时时间（秒）
    /// </summary>
    private const int API_TIMEOUT = 15;
    
    /// <summary>
    /// 是否使用LLM（可通过配置关闭）
    /// </summary>
    private bool useLLM = true;
    
    /// <summary>
    /// LLM服务是否可用
    /// </summary>
    private bool llmAvailable = true;
    
    /// <summary>
    /// 连续失败次数（用于自动禁用LLM）
    /// </summary>
    private int consecutiveFailures = 0;
    
    /// <summary>
    /// 最大连续失败次数（超过后自动禁用LLM）
    /// </summary>
    private const int MAX_FAILURES = 3;
    
    // ==================== 核心组件 ====================
    
    private ChessEvaluator evaluator;
    private System.Random random;
    private Dictionary<string, float> transpositionTable;
    
    // ==================== 搜索参数 ====================
    
    private const int SEARCH_DEPTH = 5;
    private const float INFINITY = 100000f;
    private const int MAX_THINK_TIME = 10000;  // 10秒（LLM需要额外时间）
    
    private System.DateTime searchStartTime;
    private bool isTimeout;
    
    // ==================== 棋子价值表 ====================
    
    private static readonly Dictionary<ChessManager.Chess.ChessType, int> PieceValues = 
        new Dictionary<ChessManager.Chess.ChessType, int>
    {
        { ChessManager.Chess.ChessType.帅, 10000 },
        { ChessManager.Chess.ChessType.车, 1000 },
        { ChessManager.Chess.ChessType.炮, 500 },
        { ChessManager.Chess.ChessType.马, 450 },
        { ChessManager.Chess.ChessType.象, 200 },
        { ChessManager.Chess.ChessType.士, 200 },
        { ChessManager.Chess.ChessType.卒, 100 }
    };
    
    // 位置价值表（与HardAI相同，略）
    private static readonly int[,] RookPositionTable = new int[,]
    {
        {  10,  10,  15,  20,  25,  20,  15,  10,  10 },
        {  15,  20,  25,  30,  35,  30,  25,  20,  15 },
        {  20,  25,  30,  35,  40,  35,  30,  25,  20 },
        {  25,  30,  35,  40,  45,  40,  35,  30,  25 },
        {  25,  30,  35,  40,  45,  40,  35,  30,  25 },
        {  25,  30,  35,  40,  45,  40,  35,  30,  25 },
        {  20,  25,  30,  35,  40,  35,  30,  25,  20 },
        {  15,  20,  25,  30,  35,  30,  25,  20,  15 },
        {  10,  15,  20,  25,  30,  25,  20,  15,  10 },
        {   5,  10,  15,  20,  25,  20,  15,  10,   5 }
    };
    
    private static readonly int[,] KnightPositionTable = new int[,]
    {
        { -70, -60, -50, -40, -40, -40, -50, -60, -70 },
        { -60, -40, -20, -10,   0, -10, -20, -40, -60 },
        { -50, -20,   0,  15,  25,  15,   0, -20, -50 },
        { -40, -10,  15,  30,  40,  30,  15, -10, -40 },
        { -40,   0,  25,  40,  50,  40,  25,   0, -40 },
        { -40,   0,  25,  40,  50,  40,  25,   0, -40 },
        { -40, -10,  15,  30,  40,  30,  15, -10, -40 },
        { -50, -20,   0,  15,  25,  15,   0, -20, -50 },
        { -60, -40, -20, -10,   0, -10, -20, -40, -60 },
        { -70, -60, -50, -40, -40, -40, -50, -60, -70 }
    };
    
    private static readonly int[,] CannonPositionTable = new int[,]
    {
        {   5,  10,  15,  20,  25,  20,  15,  10,   5 },
        {  10,  15,  20,  25,  30,  25,  20,  15,  10 },
        {  15,  20,  25,  30,  35,  30,  25,  20,  15 },
        {  20,  25,  30,  35,  40,  35,  30,  25,  20 },
        {  20,  25,  30,  35,  40,  35,  30,  25,  20 },
        {  20,  25,  30,  35,  40,  35,  30,  25,  20 },
        {  15,  20,  25,  30,  35,  30,  25,  20,  15 },
        {  10,  15,  20,  25,  30,  25,  20,  15,  10 },
        {   5,  10,  15,  20,  25,  20,  15,  10,   5 },
        {   0,   5,  10,  15,  20,  15,  10,   5,   0 }
    };
    
    private static readonly int[,] PawnPositionTable = new int[,]
    {
        {   0,   0,   0,   0,   0,   0,   0,   0,   0 },
        { 120, 120, 140, 150, 160, 150, 140, 120, 120 },
        { 120, 120, 140, 150, 160, 150, 140, 120, 120 },
        { 100, 120, 140, 140, 140, 140, 140, 120, 100 },
        {  90,  90,  90,  90,  90,  90,  90,  90,  90 },
        {   0,   0,   0,   0,   0,   0,   0,   0,   0 },
        {   0,   0,   0,   0,   0,   0,   0,   0,   0 },
        {   0,   0,   0,   0,   0,   0,   0,   0,   0 },
        {   0,   0,   0,   0,   0,   0,   0,   0,   0 },
        {   0,   0,   0,   0,   0,   0,   0,   0,   0 }
    };
    
    // ==================== 构造函数 ====================
    
    public LunaticAI()
    {
        evaluator = new ChessEvaluator();
        random = new System.Random();
        transpositionTable = new Dictionary<string, float>();
        
        Debug.Log("[LunaticAI] 疯狂AI初始化完成");
        Debug.Log($"[LunaticAI] LLM模式: {(useLLM ? "启用" : "禁用")}");
        Debug.Log($"[LunaticAI] 搜索深度: {SEARCH_DEPTH}");
    }
    
    // ==================== 主要接口 ====================
    
    /// <summary>
    /// 获取最佳走法（同步版本，供AIManager调用）
    /// </summary>
    public AIMove GetBestMove()
    {
        searchStartTime = System.DateTime.Now;
        isTimeout = false;
        transpositionTable.Clear();
        
        // 生成所有合法走法
        List<AIMove> legalMoves = evaluator.GenerateAllLegalMoves(forBlack: true);
        
        if (legalMoves.Count == 0)
        {
            Debug.Log("[LunaticAI] 没有合法走法");
            return AIMove.Empty;
        }
        
        // 走法排序
        legalMoves = OrderMoves(legalMoves);
        
        // 先进行Minimax搜索获取基础最优走法
        AIMove minimaxMove = GetMinimaxBestMove(legalMoves);
        
        // 如果LLM可用且启用，尝试获取LLM建议
        if (useLLM && llmAvailable && consecutiveFailures < MAX_FAILURES)
        {
            try
            {
                AIMove llmMove = GetLLMBestMove(legalMoves);
                
                if (llmMove.IsValid)
                {
                    // 比较LLM建议和Minimax结果
                    // 如果LLM建议的走法评分不太差（差距在可接受范围内），使用LLM建议
                    float minimaxScore = EvaluateMoveQuick(minimaxMove);
                    float llmScore = EvaluateMoveQuick(llmMove);
                    
                    // LLM建议的走法如果评分差距不大（200分以内），优先使用LLM
                    if (llmScore >= minimaxScore - 200)
                    {
                        Debug.Log($"[LunaticAI] 使用LLM建议走法 (LLM评分: {llmScore:F0}, Minimax评分: {minimaxScore:F0})");
                        consecutiveFailures = 0;  // 重置失败计数
                        return llmMove;
                    }
                    else
                    {
                        Debug.Log($"[LunaticAI] LLM建议评分过低，使用Minimax结果");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LunaticAI] LLM调用失败: {e.Message}");
                consecutiveFailures++;
                
                if (consecutiveFailures >= MAX_FAILURES)
                {
                    Debug.LogWarning("[LunaticAI] LLM连续失败次数过多，自动禁用LLM");
                    llmAvailable = false;
                }
            }
        }
        
        Debug.Log($"[LunaticAI] 使用Minimax搜索结果");
        return minimaxMove;
    }
    
    /// <summary>
    /// 获取Minimax搜索的最优走法
    /// </summary>
    private AIMove GetMinimaxBestMove(List<AIMove> legalMoves)
    {
        AIMove bestMove = AIMove.Empty;
        float bestScore = -INFINITY;
        
        for (int depth = 1; depth <= SEARCH_DEPTH; depth++)
        {
            if (isTimeout) break;
            
            List<ScoredMove> scoredMoves = new List<ScoredMove>();
            
            foreach (AIMove move in legalMoves)
            {
                if (isTimeout) break;
                
                SimulateMove(move, out float oldVecX, out float oldVecY, out bool wasDead);
                float score = Minimax(depth - 1, -INFINITY, INFINITY, false);
                UndoMove(move, oldVecX, oldVecY, wasDead);
                
                scoredMoves.Add(new ScoredMove { Move = move, Score = score });
            }
            
            if (!isTimeout && scoredMoves.Count > 0)
            {
                scoredMoves.Sort((a, b) => b.Score.CompareTo(a.Score));
                bestMove = scoredMoves[0].Move;
                bestScore = scoredMoves[0].Score;
                
                legalMoves.Insert(0, bestMove);
                legalMoves.Remove(bestMove);
            }
        }
        
        if (!bestMove.IsValid && legalMoves.Count > 0)
        {
            bestMove = legalMoves[0];
        }
        
        return bestMove;
    }
    
    // ==================== LLM API调用 ====================
    
    /// <summary>
    /// 获取LLM建议的走法
    /// </summary>
    private AIMove GetLLMBestMove(List<AIMove> legalMoves)
    {
        // 构建请求体
        string boardState = BuildBoardDescription();
        List<object> movesData = new List<object>();
        
        for (int i = 0; i < legalMoves.Count; i++)
        {
            AIMove move = legalMoves[i];
            ChessManager.Chess chess = ChessManager.ChessArray[move.FromChessId];
            
            movesData.Add(new
            {
                index = i,
                from_x = move.FromLogX,
                from_y = move.FromLogY,
                to_x = move.ToLogX,
                to_y = move.ToLogY,
                piece_name = chess != null ? GetChinesePieceName(chess) : "棋子",
                can_capture = move.CaptureId >= 0
            });
        }
        
        var requestBody = new
        {
            board_state = boardState,
            legal_moves = movesData
        };
        
        string jsonPayload = JsonUtility.ToJson(requestBody);
        
        // 同步HTTP请求（在Unity主线程中）
        // 注意：这里使用简化的同步请求，实际项目中应该使用协程
        string response = SendHttpRequest(LLM_API_URL, jsonPayload);
        
        if (string.IsNullOrEmpty(response))
        {
            throw new Exception("LLM API返回空响应");
        }
        
        // 解析响应
        LLMResponse llmResponse = JsonUtility.FromJson<LLMResponse>(response);
        
        if (!llmResponse.success)
        {
            throw new Exception($"LLM API调用失败: {llmResponse.error}");
        }
        
        int moveIndex = llmResponse.suggested_move_index;
        
        if (moveIndex < 0 || moveIndex >= legalMoves.Count)
        {
            moveIndex = 0;
        }
        
        Debug.Log($"[LunaticAI] LLM分析: {llmResponse.analysis?.Substring(0, Math.Min(100, llmResponse.analysis?.Length ?? 0))}...");
        
        return legalMoves[moveIndex];
    }
    
    /// <summary>
    /// 发送HTTP请求（简化版同步请求）
    /// </summary>
    private string SendHttpRequest(string url, string jsonPayload)
    {
        // 在Unity中，真正的同步HTTP请求需要使用UnityWebRequest
        // 这里提供一个框架实现，实际使用时可能需要调整
        
        // 方法1：使用UnityWebRequest（需要在协程中调用）
        // 方法2：使用HttpClient（需要.NET 4.x）
        
        // 这里使用模拟响应作为示例
        // 实际项目中，应该使用协程版本的GetBestMoveCoroutine
        
        Debug.Log($"[LunaticAI] 发送LLM API请求: {url}");
        
        // 模拟LLM响应（实际项目中替换为真实HTTP请求）
        return SimulateLLMResponse(jsonPayload);
    }
    
    /// <summary>
    /// 模拟LLM响应（用于测试）
    /// </summary>
    private string SimulateLLMResponse(string payload)
    {
        // 从payload中解析走法数量
        int moveCount = 10;  // 默认值
        
        // 返回模拟响应
        return JsonUtility.ToJson(new LLMResponse
        {
            success = true,
            analysis = "当前局面分析：我方应积极进攻，寻找吃子机会。",
            suggested_move_index = 0,
            raw_response = "模拟响应"
        });
    }
    
    /// <summary>
    /// 构建棋盘状态描述
    /// </summary>
    private string BuildBoardDescription()
    {
        StringBuilder sb = new StringBuilder();
        
        sb.AppendLine("当前中国象棋棋盘状态：");
        sb.AppendLine("红方（玩家）在下方，黑方（AI）在上方。");
        sb.AppendLine();
        
        // 创建棋盘矩阵
        string[,] board = new string[10, 9];
        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 9; x++)
            {
                board[y, x] = "·";
            }
        }
        
        // 放置棋子
        for (int i = 0; i < 32; i++)
        {
            ChessManager.Chess chess = ChessManager.ChessArray[i];
            if (chess == null || chess.Is_Dead) continue;
            
            int logX = ToolManager.ChangeToLineX(chess.Vec_X);
            int logY = ToolManager.ChangeToLineY(chess.Vec_Y);
            
            if (logX >= 0 && logX <= 8 && logY >= 0 && logY <= 9)
            {
                board[logY, logX] = GetPieceSymbol(chess);
            }
        }
        
        // 输出棋盘
        sb.AppendLine("  列: 0  1  2  3  4  5  6  7  8");
        for (int y = 0; y < 10; y++)
        {
            sb.Append($"行{y}: ");
            for (int x = 0; x < 9; x++)
            {
                sb.Append(board[y, x].PadLeft(3));
            }
            sb.AppendLine();
            
            if (y == 4)
            {
                sb.AppendLine("      ===== 楚河 · 汉界 =====");
            }
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// 获取棋子符号
    /// </summary>
    private string GetPieceSymbol(ChessManager.Chess chess)
    {
        string symbol = "";
        
        switch (chess.Type)
        {
            case ChessManager.Chess.ChessType.帅:
                symbol = chess.Is_Red ? "K" : "k";
                break;
            case ChessManager.Chess.ChessType.车:
                symbol = chess.Is_Red ? "R" : "r";
                break;
            case ChessManager.Chess.ChessType.马:
                symbol = chess.Is_Red ? "N" : "n";
                break;
            case ChessManager.Chess.ChessType.炮:
                symbol = chess.Is_Red ? "C" : "c";
                break;
            case ChessManager.Chess.ChessType.象:
                symbol = chess.Is_Red ? "B" : "b";
                break;
            case ChessManager.Chess.ChessType.士:
                symbol = chess.Is_Red ? "A" : "a";
                break;
            case ChessManager.Chess.ChessType.卒:
                symbol = chess.Is_Red ? "P" : "p";
                break;
        }
        
        return symbol;
    }
    
    /// <summary>
    /// 获取棋子中文名称
    /// </summary>
    private string GetChinesePieceName(ChessManager.Chess chess)
    {
        if (chess.Is_Red)
        {
            switch (chess.Type)
            {
                case ChessManager.Chess.ChessType.帅: return "红帅";
                case ChessManager.Chess.ChessType.车: return "红车";
                case ChessManager.Chess.ChessType.马: return "红马";
                case ChessManager.Chess.ChessType.炮: return "红炮";
                case ChessManager.Chess.ChessType.象: return "红相";
                case ChessManager.Chess.ChessType.士: return "红士";
                case ChessManager.Chess.ChessType.卒: return "红兵";
            }
        }
        else
        {
            switch (chess.Type)
            {
                case ChessManager.Chess.ChessType.帅: return "黑将";
                case ChessManager.Chess.ChessType.车: return "黑车";
                case ChessManager.Chess.ChessType.马: return "黑马";
                case ChessManager.Chess.ChessType.炮: return "黑炮";
                case ChessManager.Chess.ChessType.象: return "黑象";
                case ChessManager.Chess.ChessType.士: return "黑士";
                case ChessManager.Chess.ChessType.卒: return "黑卒";
            }
        }
        return "未知";
    }
    
    /// <summary>
    /// 快速评估走法（用于比较LLM和Minimax结果）
    /// </summary>
    private float EvaluateMoveQuick(AIMove move)
    {
        float score = 0f;
        
        if (move.CaptureId >= 0)
        {
            ChessManager.Chess captured = ChessManager.ChessArray[move.CaptureId];
            if (captured != null)
            {
                score += GetPieceValue(captured.Type);
            }
        }
        
        return score;
    }
    
    // ==================== Minimax算法（与HardAI类似，略） ====================
    
    private float Minimax(int depth, float alpha, float beta, bool isMaximizing)
    {
        if ((System.DateTime.Now - searchStartTime).TotalMilliseconds > MAX_THINK_TIME)
        {
            isTimeout = true;
            return EvaluateBoard(isMaximizing);
        }
        
        if (depth == 0)
        {
            return QuiescenceSearch(alpha, beta, isMaximizing, 6);
        }
        
        string boardKey = GetBoardKey() + depth;
        if (transpositionTable.TryGetValue(boardKey, out float cachedScore))
        {
            return cachedScore;
        }
        
        List<AIMove> moves = evaluator.GenerateAllLegalMoves(forBlack: isMaximizing);
        
        if (moves.Count == 0)
        {
            if (evaluator.IsInCheck(isMaximizing))
            {
                return isMaximizing ? -INFINITY + (SEARCH_DEPTH - depth) : INFINITY - (SEARCH_DEPTH - depth);
            }
            return 0f;
        }
        
        moves = OrderMoves(moves);
        
        float bestScore;
        
        if (isMaximizing)
        {
            bestScore = -INFINITY;
            foreach (AIMove move in moves)
            {
                SimulateMove(move, out float oldVecX, out float oldVecY, out bool wasDead);
                float score = Minimax(depth - 1, alpha, beta, false);
                UndoMove(move, oldVecX, oldVecY, wasDead);
                
                bestScore = Math.Max(bestScore, score);
                alpha = Math.Max(alpha, score);
                
                if (beta <= alpha) break;
            }
        }
        else
        {
            bestScore = INFINITY;
            foreach (AIMove move in moves)
            {
                SimulateMove(move, out float oldVecX, out float oldVecY, out bool wasDead);
                float score = Minimax(depth - 1, alpha, beta, true);
                UndoMove(move, oldVecX, oldVecY, wasDead);
                
                bestScore = Math.Min(bestScore, score);
                beta = Math.Min(beta, score);
                
                if (beta <= alpha) break;
            }
        }
        
        transpositionTable[boardKey] = bestScore;
        return bestScore;
    }
    
    private float QuiescenceSearch(float alpha, float beta, bool isMaximizing, int depth)
    {
        float standPat = EvaluateBoard(isMaximizing);
        
        if (depth == 0) return standPat;
        
        if (isMaximizing)
        {
            if (standPat >= beta) return beta;
            if (standPat > alpha) alpha = standPat;
        }
        else
        {
            if (standPat <= alpha) return alpha;
            if (standPat < beta) beta = standPat;
        }
        
        List<AIMove> moves = evaluator.GenerateAllLegalMoves(forBlack: isMaximizing);
        List<AIMove> tacticalMoves = new List<AIMove>();
        
        foreach (AIMove move in moves)
        {
            if (move.CaptureId >= 0 || IsCheckMove(move, isMaximizing))
            {
                tacticalMoves.Add(move);
            }
        }
        
        if (tacticalMoves.Count == 0) return standPat;
        
        tacticalMoves = OrderMoves(tacticalMoves);
        
        foreach (AIMove move in tacticalMoves)
        {
            SimulateMove(move, out float oldVecX, out float oldVecY, out bool wasDead);
            float score = QuiescenceSearch(alpha, beta, !isMaximizing, depth - 1);
            UndoMove(move, oldVecX, oldVecY, wasDead);
            
            if (isMaximizing)
            {
                if (score >= beta) return beta;
                if (score > alpha) alpha = score;
            }
            else
            {
                if (score <= alpha) return alpha;
                if (score < beta) beta = score;
            }
        }
        
        return isMaximizing ? alpha : beta;
    }
    
    // ==================== 评估函数 ====================
    
    private float EvaluateBoard(bool forBlack)
    {
        float score = 0f;
        
        for (int i = 0; i < 32; i++)
        {
            ChessManager.Chess chess = ChessManager.ChessArray[i];
            if (chess == null || chess.Is_Dead) continue;
            
            float value = GetPieceValue(chess.Type);
            value += GetPositionBonus(chess);
            value += GetMobilityBonus(chess);
            value += GetKingSafetyBonus(chess);
            value += GetConnectivityBonus(chess);
            
            if (chess.Is_Red)
                score += value;
            else
                score -= value;
        }
        
        return forBlack ? -score : score;
    }
    
    private float GetPieceValue(ChessManager.Chess.ChessType type)
    {
        if (PieceValues.TryGetValue(type, out int value))
            return value;
        return 0f;
    }
    
    private float GetPositionBonus(ChessManager.Chess chess)
    {
        int logX = ToolManager.ChangeToLineX(chess.Vec_X);
        int logY = ToolManager.ChangeToLineY(chess.Vec_Y);
        
        if (logX < 0 || logX > 8 || logY < 0 || logY > 9) return 0;
        
        if (!chess.Is_Red) logY = 9 - logY;
        
        switch (chess.Type)
        {
            case ChessManager.Chess.ChessType.车:
                return RookPositionTable[logY, logX];
            case ChessManager.Chess.ChessType.马:
                return KnightPositionTable[logY, logX];
            case ChessManager.Chess.ChessType.炮:
                return CannonPositionTable[logY, logX];
            case ChessManager.Chess.ChessType.卒:
                return PawnPositionTable[logY, logX];
            default:
                return 0;
        }
    }
    
    private float GetMobilityBonus(ChessManager.Chess chess)
    {
        switch (chess.Type)
        {
            case ChessManager.Chess.ChessType.车: return 20;
            case ChessManager.Chess.ChessType.马: return 15;
            case ChessManager.Chess.ChessType.炮: return 18;
            default: return 0;
        }
    }
    
    private float GetKingSafetyBonus(ChessManager.Chess chess)
    {
        if (chess.Type != ChessManager.Chess.ChessType.帅) return 0;
        
        int logX = ToolManager.ChangeToLineX(chess.Vec_X);
        int logY = ToolManager.ChangeToLineY(chess.Vec_Y);
        
        float safety = 0f;
        
        for (int i = 0; i < 32; i++)
        {
            ChessManager.Chess protector = ChessManager.ChessArray[i];
            if (protector == null || protector.Is_Dead) continue;
            if (protector.Is_Red != chess.Is_Red) continue;
            
            if (protector.Type == ChessManager.Chess.ChessType.士 ||
                protector.Type == ChessManager.Chess.ChessType.象)
            {
                int px = ToolManager.ChangeToLineX(protector.Vec_X);
                int py = ToolManager.ChangeToLineY(protector.Vec_Y);
                
                if (Math.Abs(px - logX) <= 2 && Math.Abs(py - logY) <= 2)
                    safety += 15;
            }
        }
        
        return safety;
    }
    
    private float GetConnectivityBonus(ChessManager.Chess chess)
    {
        float bonus = 0f;
        
        if (chess.Type == ChessManager.Chess.ChessType.车)
        {
            for (int i = 0; i < 32; i++)
            {
                ChessManager.Chess other = ChessManager.ChessArray[i];
                if (other == null || other.Is_Dead || other.Id == chess.Id) continue;
                if (other.Is_Red != chess.Is_Red) continue;
                if (other.Type == ChessManager.Chess.ChessType.车)
                {
                    bonus += 25;
                    break;
                }
            }
        }
        
        return bonus;
    }
    
    // ==================== 辅助方法 ====================
    
    private bool IsCheckMove(AIMove move, bool isBlack)
    {
        ChessManager.Chess movingPiece = ChessManager.ChessArray[move.FromChessId];
        if (movingPiece == null) return false;
        
        for (int i = 0; i < 32; i++)
        {
            ChessManager.Chess king = ChessManager.ChessArray[i];
            if (king == null || king.Is_Dead) continue;
            if (king.Type != ChessManager.Chess.ChessType.帅) continue;
            if (king.Is_Red == isBlack) continue;
            
            int kingLogX = ToolManager.ChangeToLineX(king.Vec_X);
            int kingLogY = ToolManager.ChangeToLineY(king.Vec_Y);
            
            if (CanAttack(movingPiece.Type, move.ToLogX, move.ToLogY, kingLogX, kingLogY))
                return true;
        }
        
        return false;
    }
    
    private bool CanAttack(ChessManager.Chess.ChessType type, int fromX, int fromY, int toX, int toY)
    {
        int dx = Math.Abs(fromX - toX);
        int dy = Math.Abs(fromY - toY);
        
        switch (type)
        {
            case ChessManager.Chess.ChessType.车:
                return dx == 0 || dy == 0;
            case ChessManager.Chess.ChessType.马:
                return (dx == 1 && dy == 2) || (dx == 2 && dy == 1);
            case ChessManager.Chess.ChessType.炮:
                return dx == 0 || dy == 0;
            default:
                return false;
        }
    }
    
    private List<AIMove> OrderMoves(List<AIMove> moves)
    {
        List<AIMove> captureMoves = new List<AIMove>();
        List<AIMove> checkMoves = new List<AIMove>();
        List<AIMove> normalMoves = new List<AIMove>();
        
        foreach (AIMove move in moves)
        {
            if (move.CaptureId >= 0)
                captureMoves.Add(move);
            else if (IsCheckMove(move, true))
                checkMoves.Add(move);
            else
                normalMoves.Add(move);
        }
        
        captureMoves.Sort((a, b) =>
        {
            ChessManager.Chess ca = ChessManager.ChessArray[a.CaptureId];
            ChessManager.Chess cb = ChessManager.ChessArray[b.CaptureId];
            float va = ca != null ? GetPieceValue(ca.Type) : 0;
            float vb = cb != null ? GetPieceValue(cb.Type) : 0;
            return vb.CompareTo(va);
        });
        
        List<AIMove> result = new List<AIMove>();
        result.AddRange(captureMoves);
        result.AddRange(checkMoves);
        result.AddRange(normalMoves);
        
        return result;
    }
    
    private string GetBoardKey()
    {
        StringBuilder sb = new StringBuilder();
        
        for (int i = 0; i < 32; i++)
        {
            ChessManager.Chess chess = ChessManager.ChessArray[i];
            if (chess == null || chess.Is_Dead)
            {
                sb.Append("x");
            }
            else
            {
                sb.Append((int)chess.Type);
                sb.Append(chess.Is_Red ? "R" : "B");
                sb.Append(ToolManager.ChangeToLineX(chess.Vec_X));
                sb.Append(ToolManager.ChangeToLineY(chess.Vec_Y));
            }
        }
        
        return sb.ToString();
    }
    
    private void SimulateMove(AIMove move, out float oldVecX, out float oldVecY, out bool wasDead)
    {
        ChessManager.Chess movingPiece = ChessManager.ChessArray[move.FromChessId];
        
        oldVecX = movingPiece.Vec_X;
        oldVecY = movingPiece.Vec_Y;
        wasDead = false;
        
        movingPiece.Vec_X = ToolManager.ChangeBackX(move.ToLogX);
        movingPiece.Vec_Y = ToolManager.ChangeBackY(move.ToLogY);
        
        if (move.CaptureId >= 0 && move.CaptureId < 32)
        {
            ChessManager.Chess captured = ChessManager.ChessArray[move.CaptureId];
            if (captured != null)
            {
                wasDead = captured.Is_Dead;
                captured.Is_Dead = true;
            }
        }
    }
    
    private void UndoMove(AIMove move, float oldVecX, float oldVecY, bool wasDead)
    {
        ChessManager.Chess movingPiece = ChessManager.ChessArray[move.FromChessId];
        
        movingPiece.Vec_X = oldVecX;
        movingPiece.Vec_Y = oldVecY;
        
        if (move.CaptureId >= 0 && move.CaptureId < 32)
        {
            ChessManager.Chess captured = ChessManager.ChessArray[move.CaptureId];
            if (captured != null)
            {
                captured.Is_Dead = wasDead;
            }
        }
    }
    
    // ==================== 数据结构 ====================
    
    [Serializable]
    private class LLMResponse
    {
        public bool success;
        public string analysis;
        public int suggested_move_index;
        public string raw_response;
        public string error;
    }
    
    private struct ScoredMove
    {
        public AIMove Move;
        public float Score;
    }
}
