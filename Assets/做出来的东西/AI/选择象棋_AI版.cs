using UnityEngine;
using System.IO;
using System.Collections;

/// <summary>
/// 选择象棋 - 修改版
/// 添加了AI思考协程，用于调用AI系统进行决策
/// 
/// 【修改说明】
/// 1. 添加了AIThinkCoroutine协程方法
/// 2. 集成了AIManager进行AI决策
/// 3. 处理AI走法的执行逻辑
/// </summary>
public class 选择象棋_AI版 : MonoBehaviour
{
    // ==================== 原有变量 ====================
    
    private GameObject 选择棋子;      // 选择的棋子对象
    public GameObject 选择框;        // 选择框预制体
    public int SelectedID = -1;     // 当前选中的棋子ID，-1表示未选中
    public int SelectedID1 = -1;     // 用于移动动画的临时ID
    public bool AllowMove = false;   // 是否允许移动
    private GameObject SelectedChessObj; // 当前选中的棋子游戏对象
    public Vector3 ClickPosition;   // 点击位置
    public Vector3 LocalTargetPos;
    
    // 回合控制变量
    public bool IsRedTurn = true;   // 当前是否是红方回合
    
    public GameObject WorringSign1;   // 警告提示UI
    public GameObject WorringSign2;    // 警告提示UI
    private bool IsIllegal = false;  // 是否为非法移动
    private bool IsIllegal2 = false; // 是否为非法移动
    private float TimeCounter = 0;   // 时间计数器
    private float TimeCounter2 = 0;
    
    public Transform ChessBoardTransform; // 棋盘物体引用
    
    // ==================== 新增变量 ====================
    
    /// <summary>
    /// AI是否正在思考
    /// 防止AI思考时玩家进行操作
    /// </summary>
    private bool isAIThinking = false;
    
    /// <summary>
    /// AI思考延迟时间（秒）
    /// 让玩家有时间看到AI的思考过程
    /// </summary>
    private float aiThinkDelay = 0.5f;
    
    // ==================== 原有方法 ====================
    
    /// <summary>
    /// 判断当前选中棋子是否属于当前回合
    /// </summary>
    public bool WhichTurn(int SelectedID)
    {
        if (ChessManager.ChessArray[SelectedID].Is_Red == IsRedTurn)
        {
            IsIllegal = false;
            return true;
        }
        else
        {
            IsIllegal = true;
            return false;
        }
    }
    
    /// <summary>
    /// 根据棋子类型调用相应的规则检测方法
    /// </summary>
    public bool CheckMoveRules(int selectedId, float VecX, float VecY, int destroyId)
    {
        int targetLogX = ToolManager.ChangeToLineX(VecX);
        int targetLogY = ToolManager.ChangeToLineY(VecY);
        
        ChessManager.Chess chess = ChessManager.ChessArray[selectedId];
        
        switch (chess.Type)
        {
            case ChessManager.Chess.ChessType.车:
                return 规则检测.车(selectedId, targetLogY, targetLogX, destroyId);
            case ChessManager.Chess.ChessType.马:
                return 规则检测.马(selectedId, targetLogY, targetLogX, destroyId);
            case ChessManager.Chess.ChessType.炮:
                return 规则检测.炮(selectedId, targetLogY, targetLogX, destroyId);
            case ChessManager.Chess.ChessType.象:
                return 规则检测.相(selectedId, targetLogY, targetLogX, destroyId);
            case ChessManager.Chess.ChessType.士:
                return 规则检测.士(selectedId, targetLogY, targetLogX, destroyId);
            case ChessManager.Chess.ChessType.帅:
                return 规则检测.将(selectedId, targetLogY, targetLogX, destroyId);
            case ChessManager.Chess.ChessType.卒:
                return 规则检测.兵(selectedId, targetLogY, targetLogX, destroyId);
            default:
                return false;
        }
    }
    
    private void Awake()
    {
        WorringSign1.SetActive(false);
        WorringSign2.SetActive(false);
        选择棋子 = Instantiate(选择框, Vector3.zero, Quaternion.identity);
        选择棋子.SetActive(false);
    }
    
    // ==================== 核心修改：Update方法 ====================
    
    private void Update()
    {
        // ========== AI回合处理 ==========
        // 如果是黑方回合（AI回合），启动AI思考协程
        if (!IsRedTurn && !AllowMove && !isAIThinking)
        {
            StartCoroutine(AIThinkCoroutine());
            return; // 必须return，阻止后续的鼠标点击代码执行
        }
        
        // ========== 玩家操作处理 ==========
        if (Input.GetMouseButtonDown(0))
        {
            // 移动动画或AI思考期间禁止操作
            Vector2 MousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(MousePosition, Vector2.zero);
            
            if (hit.collider != null)
            {
                // 检查回合权限
                if (SelectedID != -1)
                {
                    if (WhichTurn(SelectedID) == false)
                    {
                        IsIllegal2 = true;
                        WorringSign2.SetActive(true);
                        SelectedID = -1;
                        选择棋子.SetActive(false);
                        return;
                    }
                }
                
                // === 情况1：当前没有选中棋子 ===
                if (SelectedID == -1)
                {
                    if (hit.transform.tag == "红棋" || hit.transform.tag == "黑棋")
                    {
                        SelectedID = ToolManager.GetChessId(hit.transform.gameObject);
                        SelectedChessObj = hit.transform.gameObject;
                        选择棋子.transform.position = hit.transform.position;
                        选择棋子.SetActive(true);
                        
                        // 选中阶段限制只能选当前回合阵营
                        if (SelectedID != -1 && ChessManager.ChessArray[SelectedID] != null &&
                            ChessManager.ChessArray[SelectedID].Is_Red != IsRedTurn)
                        {
                            IsIllegal2 = true;
                            WorringSign2.SetActive(true);
                            SelectedID = -1;
                            SelectedChessObj = null;
                            选择棋子.SetActive(false);
                            return;
                        }
                    }
                }
                // === 情况2：当前已经选中了棋子 ===
                else
                {
                    选择棋子.SetActive(false);
                    
                    // --- 点击了棋盘（移动） ---
                    if (hit.transform.tag == "棋盘")
                    {
                        Vector3 localTargetPos = ChessBoardTransform.InverseTransformPoint(hit.point);
                        if (CheckMoveRules(SelectedID, localTargetPos.x, localTargetPos.y, -1))
                        {
                            AllowMove = true;
                            
                            int targetLogY = ToolManager.ChangeToLineY(localTargetPos.y);
                            int targetLogX = ToolManager.ChangeToLineX(localTargetPos.x);
                            
                            float centeredX = ToolManager.ChangeBackX(targetLogX);
                            float centeredY = ToolManager.ChangeBackY(targetLogY);
                            
                            LocalTargetPos = new Vector3(centeredX, centeredY, 0);
                            ClickPosition = ChessBoardTransform.TransformPoint(LocalTargetPos);
                            
                            SelectedID1 = SelectedID;
                            SelectedID = -1;
                            IsRedTurn = !IsRedTurn;  // 切换回合
                        }
                        else
                        {
                            IsIllegal = true;
                            WorringSign1.SetActive(true);
                            SelectedID = -1;
                        }
                    }
                    // --- 点击了其他棋子（吃子） ---
                    else if (hit.transform.tag == "红棋" || hit.transform.tag == "黑棋")
                    {
                        int targetChessId = ToolManager.GetChessId(hit.transform.gameObject);
                        
                        // 点击己方棋子，切换选择
                        if (ChessManager.ChessArray[SelectedID].Is_Red == ChessManager.ChessArray[targetChessId].Is_Red)
                        {
                            SelectedID = targetChessId;
                            SelectedChessObj = hit.transform.gameObject;
                            选择棋子.transform.position = hit.transform.position;
                            选择棋子.SetActive(true);
                            return;
                        }
                        
                        Vector3 localTargetPos = ChessBoardTransform.InverseTransformPoint(hit.point);
                        
                        if (CheckMoveRules(SelectedID, localTargetPos.x, localTargetPos.y, targetChessId))
                        {
                            // 吃子
                            ChessManager.ChessArray[targetChessId].Is_Dead = true;
                            Destroy(hit.transform.gameObject);
                            
                            AllowMove = true;
                            
                            int targetLogY = ToolManager.ChangeToLineY(localTargetPos.y);
                            int targetLogX = ToolManager.ChangeToLineX(localTargetPos.x);
                            float centeredX = ToolManager.ChangeBackX(targetLogX);
                            float centeredY = ToolManager.ChangeBackY(targetLogY);
                            LocalTargetPos = new Vector3(centeredX, centeredY, 0);
                            ClickPosition = ChessBoardTransform.TransformPoint(LocalTargetPos);
                            
                            SelectedID1 = SelectedID;
                            SelectedID = -1;
                            IsRedTurn = !IsRedTurn;  // 切换回合
                        }
                        else
                        {
                            IsIllegal = true;
                            WorringSign1.SetActive(true);
                            SelectedID = -1;
                        }
                    }
                    else
                    {
                        SelectedID = -1;
                    }
                }
            }
        }
        
        // ========== 执行移动动画 ==========
        if (AllowMove)
        {
            SelectedChessObj.transform.position = Vector3.MoveTowards(
                SelectedChessObj.transform.position,
                ClickPosition,
                Time.deltaTime * 10
            );
            
            if (Vector3.Distance(SelectedChessObj.transform.position, ClickPosition) < 0.1f)
            {
                SelectedChessObj.transform.position = ClickPosition;
                ChessManager.ChessArray[SelectedID1].Vec_X = LocalTargetPos.x;
                ChessManager.ChessArray[SelectedID1].Vec_Y = LocalTargetPos.y;
                
                AllowMove = false;
                SelectedID = -1;
                SelectedID1 = -1;
                SelectedChessObj = null;
            }
        }
        
        // ========== UI计时器 ==========
        if (IsIllegal)
        {
            TimeCounter += Time.deltaTime;
            if (TimeCounter >= 1)
            {
                WorringSign1.SetActive(false);
                TimeCounter = 0;
                IsIllegal = false;
            }
        }
        else if (IsIllegal2)
        {
            TimeCounter2 += Time.deltaTime;
            if (TimeCounter2 >= 1)
            {
                WorringSign2.SetActive(false);
                TimeCounter2 = 0;
                IsIllegal2 = false;
            }
        }
    }
    
    // ==================== 新增：AI思考协程 ====================
    
    /// <summary>
    /// AI思考协程
    /// 这是AI决策的核心流程
    /// 
    /// 【执行流程】
    /// 1. 设置思考状态标志
    /// 2. 等待一小段时间（让玩家看到局面）
    /// 3. 调用AIManager获取最佳走法
    /// 4. 执行AI走法
    /// 5. 切换回合
    /// </summary>
    private IEnumerator AIThinkCoroutine()
    {
        // 第一步：设置思考状态
        isAIThinking = true;
        
        Debug.Log("[AI] 开始思考...");
        
        // 第二步：等待一小段时间
        // 这让玩家有时间看到AI"正在思考"
        yield return new WaitForSeconds(aiThinkDelay);
        
        // 第三步：获取AI的最佳走法
        // AIManager会根据当前难度选择对应的AI策略
        AIMove bestMove = AIManager.Instance.GetBestMove();
        
        // 第四步：检查走法是否有效
        if (!bestMove.IsValid)
        {
            Debug.Log("[AI] 没有有效走法，游戏可能结束");
            isAIThinking = false;
            yield break;
        }
        
        // 第五步：执行AI走法
        yield return ExecuteAIMove(bestMove);
        
        // 第六步：重置思考状态
        isAIThinking = false;
        
        Debug.Log("[AI] 思考完成，回合切换");
    }
    
    /// <summary>
    /// 执行AI走法
    /// </summary>
    private IEnumerator ExecuteAIMove(AIMove move)
    {
        // 获取移动的棋子对象
        ChessManager.Chess chess = ChessManager.ChessArray[move.FromChessId];
        if (chess == null || chess.Is_Dead)
        {
            Debug.LogError("[AI] 无效的棋子ID");
            yield break;
        }
        
        SelectedChessObj = chess.Obj;
        
        // 处理吃子
        if (move.CaptureId >= 0 && move.CaptureId < 32)
        {
            ChessManager.Chess captured = ChessManager.ChessArray[move.CaptureId];
            if (captured != null && !captured.Is_Dead)
            {
                captured.Is_Dead = true;
                if (captured.Obj != null)
                {
                    Destroy(captured.Obj);
                }
                Debug.Log($"[AI] 吃掉了 {captured.Type}");
            }
        }
        
        // 计算目标位置
        LocalTargetPos = new Vector3(
            ToolManager.ChangeBackX(move.ToLogX),
            ToolManager.ChangeBackY(move.ToLogY),
            0
        );
        ClickPosition = ChessBoardTransform.TransformPoint(LocalTargetPos);
        
        // 更新棋子数据
        chess.Vec_X = LocalTargetPos.x;
        chess.Vec_Y = LocalTargetPos.y;
        
        // 执行移动动画
        AllowMove = true;
        SelectedID1 = move.FromChessId;
        
        // 等待移动动画完成
        while (AllowMove)
        {
            SelectedChessObj.transform.position = Vector3.MoveTowards(
                SelectedChessObj.transform.position,
                ClickPosition,
                Time.deltaTime * 10
            );
            
            if (Vector3.Distance(SelectedChessObj.transform.position, ClickPosition) < 0.1f)
            {
                SelectedChessObj.transform.position = ClickPosition;
                AllowMove = false;
            }
            
            yield return null;
        }
        
        // 重置状态
        SelectedID1 = -1;
        SelectedChessObj = null;
        
        // 切换回合
        IsRedTurn = true;
        
        Debug.Log($"[AI] 移动完成：{chess.Type} 到 ({move.ToLogX}, {move.ToLogY})");
    }
}
