using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 开始界面UI控制器
/// 负责管理游戏主菜单的界面切换、难度选择、登录流程以及场景加载
/// 
/// 界面流程：
///   1. 启动游戏 → 显示初始界面（含"开始"和"退出"按钮）
///   2. 点击"开始" → 进入登录界面（由 LoginUIControl 处理登录/注册）
///   3. 登录成功 → 显示模式选择（单人/双人/EX）
///   4. 选择模式后 → 选择难度（Easy/Normal/Hard/Lunatic）
///   5. 选择难度后 → 加载游戏场景
/// 
/// 使用方式：
///   1. 将此脚本挂载到开始界面的根 Canvas 上
///   2. 在 Inspector 中拖拽绑定所有 UI 面板和按钮
///   3. 确保 LoginUIControl 脚本已挂载到登录界面节点上
/// </summary>
public class 开始界面UI : MonoBehaviour
{
    // ==================== UI 面板 ====================
    public GameObject UI;           // 根UI节点（注意：如果脚本挂载在这个UI上，不要SetActive(false)它）
    public GameObject 初始界面;      // 初始主菜单面板
    public GameObject 单人;          // 单人模式面板
    public GameObject 双人;          // 双人模式面板
    public GameObject EX;           // EX模式面板
    public GameObject 登录界面;      // 登录/注册面板

    // ==================== 加载动画 ====================
    public GameObject 少女分形;      // Loading动画预制体
    public GameObject 位置1;         // 动画生成位置

    // ==================== 初始界面按钮 ====================
    public Button StartButten;      // 开始按钮
    public Button Quit;             // 退出按钮

    // ==================== 模式选择按钮 ====================
    public Button EXtra;            // EX模式按钮
    public Button Practice;         // 单人模式按钮

    // ==================== 难度选择按钮 ====================
    public Button EasyMode;         // 简单难度
    public Button NormalMode;       // 普通难度
    public Button HardMode;         // 困难难度
    public Button LunaticMode;      // 地狱难度

    // ==================== 登录相关 ====================
    public LoginUIControl loginControl;  // 登录控制器引用

    // ==================== 内部状态 ====================
    private bool isLoading = false;

    private void Start()
    {
        // 绑定初始界面按钮
        if (StartButten != null) StartButten.onClick.AddListener(OnStartButtonClick);
        if (Quit != null) Quit.onClick.AddListener(QuitGame);

        // 绑定模式选择按钮
        if (EXtra != null) EXtra.onClick.AddListener(EXtraGame);
        if (Practice != null) Practice.onClick.AddListener(PracticeGame);

        // 绑定难度选择按钮
        if (EasyMode != null) EasyMode.onClick.AddListener(Easy);
        if (NormalMode != null) NormalMode.onClick.AddListener(Normal);
        if (HardMode != null) HardMode.onClick.AddListener(Hard);
        if (LunaticMode != null) LunaticMode.onClick.AddListener(Lunatic);

        // 绑定登录成功回调
        if (loginControl != null)
        {
            loginControl.OnLoginSuccess.AddListener(OnLoginSuccess);
        }

        // 初始显示：初始界面可见，其他隐藏
        ShowOnlyPanel(初始界面);
    }

    // ==================== 初始界面按钮回调 ====================

    /// <summary>
    /// "开始"按钮点击事件 → 显示登录界面
    /// 用法：由 StartButten 按钮自动绑定调用
    /// </summary>
    private void OnStartButtonClick()
    {
        Debug.Log("[开始界面UI] 点击开始，显示登录界面");
        ShowOnlyPanel(登录界面);
    }

    /// <summary>
    /// "退出"按钮点击事件 → 退出游戏
    /// 用法：由 Quit 按钮自动绑定调用
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[开始界面UI] 退出游戏");
        Application.Quit();
    }

    // ==================== 模式选择回调 ====================

    /// <summary>
    /// 单人模式 → 显示难度选择面板
    /// 用法：由 Practice 按钮自动绑定调用
    /// </summary>
    public void PracticeGame()
    {
        Debug.Log("[开始界面UI] 选择单人模式");
        PlayerPrefs.SetString("GameMode", "Single");
        ShowOnlyPanel(单人);
    }

    /// <summary>
    /// EX模式 → 显示难度选择面板
    /// 用法：由 EXtra 按钮自动绑定调用
    /// </summary>
    public void EXtraGame()
    {
        Debug.Log("[开始界面UI] 选择EX模式");
        PlayerPrefs.SetString("GameMode", "EX");
        ShowOnlyPanel(EX);
    }

    // ==================== 难度选择回调 ====================

    /// <summary>
    /// 简单难度 → 保存难度设置并加载游戏场景
    /// 用法：由 EasyMode 按钮自动绑定调用
    /// </summary>
    public void Easy()
    {
        PlayerPrefs.SetString("Difficulty", "Easy");
        StartCoroutine(Loading("游戏界面"));
    }

    /// <summary>
    /// 普通难度 → 保存难度设置并加载游戏场景
    /// 用法：由 NormalMode 按钮自动绑定调用
    /// </summary>
    public void Normal()
    {
        PlayerPrefs.SetString("Difficulty", "Normal");
        StartCoroutine(Loading("游戏界面"));
    }

    /// <summary>
    /// 困难难度 → 保存难度设置并加载游戏场景
    /// 用法：由 HardMode 按钮自动绑定调用
    /// </summary>
    public void Hard()
    {
        PlayerPrefs.SetString("Difficulty", "Hard");
        StartCoroutine(Loading("游戏界面"));
    }

    /// <summary>
    /// 地狱难度 → 保存难度设置并加载游戏场景
    /// 用法：由 LunaticMode 按钮自动绑定调用
    /// </summary>
    public void Lunatic()
    {
        PlayerPrefs.SetString("Difficulty", "Lunatic");
        StartCoroutine(Loading("游戏界面"));
    }

    // ==================== 登录成功回调 ====================

    /// <summary>
    /// 登录成功回调 → 保存用户信息到 PlayerPrefs，显示模式选择界面
    /// 用法：由 LoginUIControl.OnLoginSuccess 事件自动触发
    /// </summary>
    private void OnLoginSuccess()
    {
        Debug.Log($"[开始界面UI] 登录成功回调，用户ID: {LoginUIControl.CurrentUserId}");

        // 保存当前登录用户信息到 PlayerPrefs，供游戏场景读取
        PlayerPrefs.SetInt("CurrentUserId", LoginUIControl.CurrentUserId);
        PlayerPrefs.SetString("CurrentAccount", LoginUIControl.CurrentAccount);
        PlayerPrefs.Save();

        // 显示模式选择界面（回到初始界面，但隐藏登录按钮，显示模式选择）
        ShowOnlyPanel(初始界面);

        // 登录成功后，将"开始"按钮文字改为"继续"或直接进入模式选择
        // 这里选择直接显示模式选择区域
        if (初始界面 != null) 初始界面.SetActive(false);
        if (单人 != null) 单人.SetActive(true);
    }

    // ==================== 场景加载 ====================

    /// <summary>
    /// 加载游戏场景（带Loading动画）
    /// 用法：StartCoroutine(Loading("游戏界面"));
    /// 流程：
    ///   1. 隐藏所有UI面板
    ///   2. 生成Loading动画（少女分形）
    ///   3. 等待2秒
    ///   4. 加载目标场景
    /// </summary>
    /// <param name="sceneName">目标场景名称</param>
    public IEnumerator Loading(string sceneName)
    {
        Debug.Log("开始加载流程...");

        if (isLoading) yield break;
        isLoading = true;

        // 隐藏所有面板
        if (初始界面 != null) 初始界面.SetActive(false);
        if (单人 != null) 单人.SetActive(false);
        if (双人 != null) 双人.SetActive(false);
        if (EX != null) EX.SetActive(false);
        if (登录界面 != null) 登录界面.SetActive(false);

        CanvasGroup canvas = GetComponent<CanvasGroup>();
        if (canvas != null) canvas.blocksRaycasts = false;

        Debug.Log("UI已隐藏，生成Loading动画...");

        // 生成Loading动画
        if (少女分形 != null && 位置1 != null)
        {
            Instantiate(少女分形, 位置1.transform.position, Quaternion.identity);
        }

        Debug.Log("等待2秒...");
        yield return new WaitForSecondsRealtime(2.0f);

        Debug.Log("开始加载场景: " + sceneName);

        SceneManager.LoadScene(sceneName);

        isLoading = false;
    }

    // ==================== 面板切换工具方法 ====================

    /// <summary>
    /// 只显示指定面板，隐藏其他所有面板
    /// 用法：ShowOnlyPanel(登录界面);
    /// </summary>
    /// <param name="panelToShow">要显示的面板 GameObject</param>
    private void ShowOnlyPanel(GameObject panelToShow)
    {
        // 隐藏所有面板
        if (初始界面 != null) 初始界面.SetActive(false);
        if (单人 != null) 单人.SetActive(false);
        if (双人 != null) 双人.SetActive(false);
        if (EX != null) EX.SetActive(false);
        if (登录界面 != null) 登录界面.SetActive(false);

        // 显示目标面板
        if (panelToShow != null) panelToShow.SetActive(true);
    }
}
