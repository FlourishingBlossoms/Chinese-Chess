using System;
using UnityEngine;

public class 规则检测 : MonoBehaviour
{

    public static bool 车(int SelectedId, int TargetLogY, int TargetLogX, int DistoryID)
    {
        // 【修正】变量名与实际含义对齐
        // ChangeToLineX 返回列 -> 赋值给 StartLogX
        int StartLogX = ToolManager.ChangeToLineX(ChessManager.ChessArray[SelectedId].Vec_X);
        // ChangeToLineY 返回行 -> 赋值给 StartLogY
        int StartLogY = ToolManager.ChangeToLineY(ChessManager.ChessArray[SelectedId].Vec_Y);

        // 【修正】逻辑判断：行相同 或 列相同
        if (StartLogY == TargetLogY || StartLogX == TargetLogX)
        {
            // 【修正】CountLineChess 参数顺序已修正为
            int pathCount = ToolManager.CountLineChess(StartLogX, StartLogY, TargetLogX, TargetLogY);

            // #region agent log

            if (pathCount == 0)
            {
                 return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }
    public static bool 马(int SelectedId, int TargetLogY, int TargetLogX, int DistoryID)
    {
        int StartLogX = ToolManager.ChangeToLineX(ChessManager.ChessArray[SelectedId].Vec_X);
        int StartLogY = ToolManager.ChangeToLineY(ChessManager.ChessArray[SelectedId].Vec_Y);

        int dy = Math.Abs(StartLogY - TargetLogY);
        int dx = Math.Abs(StartLogX - TargetLogX);

        // 情况1：纵向走2格，横向走1格
        if (dy == 2 && dx == 1)
        {
            // 蹩马腿：检查纵向中点是否有棋子
            int midY = (StartLogY + TargetLogY) / 2;
            if (ToolManager.GetChessId(StartLogX, midY) == -1)
                return true;
        }
        // 情况2：横向走2格，纵向走1格
        else if (dy == 1 && dx == 2)
        {
            // 蹩马腿：检查横向中点是否有棋子
            int midX = (StartLogX + TargetLogX) / 2;
            if (ToolManager.GetChessId(midX, StartLogY) == -1)
                return true;
        }

        return false;
    }
    public static bool 炮(int SelectedId, int TargetLogY, int TargetLogX, int DistoryID)
    {
        int StartLogX = ToolManager.ChangeToLineX(ChessManager.ChessArray[SelectedId].Vec_X);
        int StartLogY = ToolManager.ChangeToLineY(ChessManager.ChessArray[SelectedId].Vec_Y);

        if (DistoryID == -1)
        {
            // 移动：逻辑同车
            return 车(SelectedId, TargetLogY, TargetLogX, DistoryID);
        }
        else
        {
            // 吃子：必须隔一个子
            // 【修正】判断直线
            if (StartLogY == TargetLogY || StartLogX == TargetLogX)
            {
                // 【修正】参数顺序
                int pathCount = ToolManager.CountLineChess(StartLogX, StartLogY, TargetLogX, TargetLogY);

                // #region agent log

                if (pathCount == 1)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public static bool 相(int SelectedId, int TargetLogY, int TargetLogX, int destoryId)
    {
        int StartLogX = ToolManager.ChangeToLineX(ChessManager.ChessArray[SelectedId].Vec_X);
        int StartLogY = ToolManager.ChangeToLineY(ChessManager.ChessArray[SelectedId].Vec_Y);

        // 【修正】参数顺序
        int Side = ToolManager.Relation(StartLogX, StartLogY, TargetLogX, TargetLogY);
        if (Side != 22) return false;

        // 计算象眼位置
        int eyeLogY = (TargetLogY + StartLogY) / 2;
        int eyeLogX = (TargetLogX + StartLogX) / 2;

        // 【修正】GetChessId 参数顺序
        if (ToolManager.GetChessId(eyeLogX, eyeLogY) != -1) return false;

        // 检查过河
        if (ChessManager.ChessArray[SelectedId].Is_Red == false) // 黑方
        {
            // 黑方只能在下方 (LogY 5-9)，不能去上方 (LogY < 5)
            // 注意：原代码写的是 row < 4，根据上下文如果是标准棋盘，黑方一般在 5-9
            // 这里保持原逻辑意图，但变量名修正
            if (TargetLogY < 5) return false;
        }
        else // 红方
        {
            // 红方只能在上方 (LogY 0-4)，不能去下方 (LogY > 4)
            if (TargetLogY > 4) return false;
        }

        return true;
    }

    public static bool 兵(int SelectedId, int TargetLogY, int TargetLogX, int destoryId)
    {
        int StartLogX = ToolManager.ChangeToLineX(ChessManager.ChessArray[SelectedId].Vec_X);
        int StartLogY = ToolManager.ChangeToLineY(ChessManager.ChessArray[SelectedId].Vec_Y);

        int Side = ToolManager.Relation(StartLogX, StartLogY, TargetLogX, TargetLogY);

        if (Side != 1 && Side != 10) return false;

        if (ChessManager.ChessArray[SelectedId].Is_Red == false) // 黑方
        {
            if (TargetLogY > StartLogY) return false;
            if (StartLogY > 4 && StartLogX != TargetLogX) return false;
        }
        else // 红方
        {
            if (TargetLogY < StartLogY) return false;

            if (StartLogY < 5 && StartLogX != TargetLogX) return false;
        }

        return true;
    }
    public static bool 将(int SelectedId, int TargetLogY, int TargetLogX, int destoryId)
    {
        // 将帅对面检测
        if (destoryId != -1 && ChessManager.ChessArray[destoryId].Type == ChessManager.Chess.ChessType.帅)
            return 车(SelectedId, TargetLogY, TargetLogX, destoryId);

        // 九宫格列限制 (LogX: 3-5)
        if (TargetLogX < 3 || TargetLogX > 5) return false;

        if (ChessManager.ChessArray[SelectedId].Is_Red == false) // 黑方
        {
            // 黑方九宫行范围 (LogY 7-9)
            if (TargetLogY < 7 || TargetLogY > 9) return false;
        }
        else // 红方
        {
            // 红方九宫行范围 (LogY 0-2)
            if (TargetLogY < 0 || TargetLogY > 2) return false;
        }

        int StartLogX = ToolManager.ChangeToLineX(ChessManager.ChessArray[SelectedId].Vec_X);
        int StartLogY = ToolManager.ChangeToLineY(ChessManager.ChessArray[SelectedId].Vec_Y);

        // 【修正】参数顺序
        int Side = ToolManager.Relation(StartLogX, StartLogY, TargetLogX, TargetLogY);
        if (Side != 1 && Side != 10) return false;

        return true;
    }

    /// <summary>
    /// 士的走棋规则
    /// </summary>
    public static bool 士(int SelectedId, int TargetLogY, int TargetLogX, int destoryId)
    {
        if (ChessManager.ChessArray[SelectedId].Is_Red == false) // 黑方
        {
            if (TargetLogY < 7 || TargetLogY > 9) return false;
        }
        else // 红方
        {
            if (TargetLogY < 0 || TargetLogY > 2) return false;
        }

        if (TargetLogX < 3 || TargetLogX > 5) return false;

        int StartLogX = ToolManager.ChangeToLineX(ChessManager.ChessArray[SelectedId].Vec_X);
        int StartLogY = ToolManager.ChangeToLineY(ChessManager.ChessArray[SelectedId].Vec_Y);

        // 【修正】参数顺序
        int Side = ToolManager.Relation(StartLogX, StartLogY, TargetLogX, TargetLogY);
        if (Side != 11) return false;

        return true;
    }
}