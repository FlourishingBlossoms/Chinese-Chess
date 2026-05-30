//using System.Collections;
//using System.Collections.Generic;
//using TMPro;
//using Unity.VisualScripting;
//using UnityEngine;
//using UnityEngine.UI;

///// <summary>
///// 登录/注册输入框控制器
///// 负责处理用户输入、登录验证、注册新账号，以及登录/注册界面的翻转动画
///// 
///// 使用方式：
/////   1. 将此脚本挂载到 LoginUI 根节点上
/////   2. 在 Inspector 中拖拽绑定以下字段：
/////      - inputAccount / inputPassWord：登录界面的账号/密码输入框
/////      - inputSingAccount / inputSignPassWord：注册界面的账号/密码输入框
/////      - 登入 / 注册 按钮
/////      - 注册界面 / 登录界面 GameObject
/////   3. 数据库路径默认为 Application.dataPath + "/../Save/SaveData.db"
///// </summary>
//public class LoginUIControl : MonoBehaviour
//{
//    // ==================== 存储数据的变量 ====================
//    private string userAccount;
//    private string userPassWord;
//    private string userSingAccount;
//    private string userSignPassWord;

//    // ==================== 输入框绑定 ====================
//    // 使用 SerializeField 保证面板可拖拽，同时保持私有安全
//    [SerializeField] private TMP_InputField inputAccount;
//    [SerializeField] private TMP_InputField inputPassWord;
//    [SerializeField] private TMP_InputField inputSingAccount;
//    [SerializeField] private TMP_InputField inputSignPassWord;

//    // ==================== 按钮绑定 ====================
//    public Button 登入;
//    public Button 注册;
//    public Button 忘记密码;
//    public Button 想起密码;

//    // ==================== 界面翻转设置 ====================
//    [Header("翻转页面设置")]
//    public GameObject 注册界面; // 注册面板
//    public GameObject 登录界面; // 登录面板

//    [Header("动画参数")]
//    [Range(1f, 15f)] public float flipSpeed = 10f;

//    // ==================== 提示信息 UI ====================
//    [Header("提示信息")]
//    public GameObject 提示信息框;    // 提示信息面板（包含文本子对象）
//    public TMP_Text 提示文本;       // 提示信息文本组件
//    public float 提示显示时长 = 2f;  // 提示信息显示时长（秒）

//    // ==================== 登录成功回调 ====================
//    [Header("登录成功回调")]
//    public UnityEngine.Events.UnityEvent OnLoginSuccess; // 登录成功时触发的事件

//    // ==================== 内部状态 ====================
//    private bool isFlipping = false;
//    private float flipProgress = 0f;
//    private GameObject flipOutObj;
//    private GameObject flipInObj;
//    private bool isShowingPage1 = true;

//    // ==================== 数据库相关 ====================
//    private SQLDBCommand db;
//    private string dbPath;

//    // ==================== 当前登录用户 ====================
//    /// <summary>
//    /// 当前已登录的用户 Id，-1 表示未登录
//    /// </summary>
//    public static int CurrentUserId { get; private set; } = -1;

//    /// <summary>
//    /// 当前已登录的用户账号
//    /// </summary>
//    public static string CurrentAccount { get; private set; } = "";

//    // ==================== 生命周期方法 ====================

//    void Start()
//    {
//        // 初始化数据库
//        dbPath = Application.dataPath + "/../Save/SaveData.db";
//        db = new SQLDBCommand(dbPath);

//        // 绑定输入框回调
//        if (inputAccount != null) inputAccount.onEndEdit.AddListener(OnUserInputAccount);
//        if (inputPassWord != null) inputPassWord.onEndEdit.AddListener(OnUserInputPassWord);
//        if (inputSingAccount != null) inputSingAccount.onEndEdit.AddListener(OnSignInputAccount);
//        if (inputSignPassWord != null) inputSignPassWord.onEndEdit.AddListener(OnSignInputPassWord);

//        // 绑定按钮回调
//        if (登入 != null) 登入.onClick.AddListener(OnLoginButtonClick);
//        if (注册 != null) 注册.onClick.AddListener(OnRegisterButtonClick);
//        if (忘记密码 != null) 忘记密码.onClick.AddListener(Flip);
//        if (想起密码 != null) 想起密码.onClick.AddListener(Flip);

//        // 初始显示登录界面
//        if (登录界面 != null) 登录界面.transform.localScale = new Vector3(1, 1, 1);
//        if (注册界面 != null) 注册界面.transform.localScale = new Vector3(0, 1, 1);

//        // 隐藏提示信息框
//        if (提示信息框 != null) 提示信息框.SetActive(false);
//    }

//    void Update()
//    {
//        UpdateFlipAnimation();
//    }

//    // ==================== 登录按钮回调 ====================

//    /// <summary>
//    /// 登录按钮点击事件
//    /// 验证账号密码是否正确，成功则记录当前用户并触发 OnLoginSuccess 事件
//    /// 用法：由 登入 按钮自动绑定调用
//    /// </summary>
//    private void OnLoginButtonClick()
//    {
//        // 输入校验
//        if (string.IsNullOrEmpty(userAccount))
//        {
//            ShowMessage("请输入账号！");
//            return;
//        }
//        if (string.IsNullOrEmpty(userPassWord))
//        {
//            ShowMessage("请输入密码！");
//            return;
//        }

//        // 调用数据库登录验证
//        int userId = db.LoginUser(userAccount, userPassWord);
//        if (userId != -1)
//        {
//            // 登录成功
//            CurrentUserId = userId;
//            CurrentAccount = userAccount;
//            Debug.Log($"[LoginUIControl] 登录成功！用户ID: {userId}, 账号: {userAccount}");
//            ShowMessage("登录成功！");

//            // 触发登录成功事件
//            if (OnLoginSuccess != null)
//            {
//                OnLoginSuccess.Invoke();
//            }
//        }
//        else
//        {
//            // 登录失败
//            Debug.LogWarning("[LoginUIControl] 登录失败：账号或密码错误");
//            ShowMessage("账号或密码错误！");
//        }
//    }

//    // ==================== 注册按钮回调 ====================

//    /// <summary>
//    /// 注册按钮点击事件
//    /// 创建新账号，成功后自动切换到登录界面
//    /// 用法：由 注册 按钮自动绑定调用
//    /// </summary>
//    private void OnRegisterButtonClick()
//    {
//        // 输入校验
//        if (string.IsNullOrEmpty(userSingAccount))
//        {
//            ShowMessage("请输入注册账号！");
//            return;
//        }
//        if (string.IsNullOrEmpty(userSignPassWord))
//        {
//            ShowMessage("请输入注册密码！");
//            return;
//        }
//        if (userSingAccount.Length < 3)
//        {
//            ShowMessage("账号长度不能少于3位！");
//            return;
//        }
//        if (userSignPassWord.Length < 4)
//        {
//            ShowMessage("密码长度不能少于4位！");
//            return;
//        }

//        // 调用数据库注册
//        int userId = db.RegisterUser(userSingAccount, userSignPassWord, userSingAccount);
//        if (userId != -1)
//        {
//            // 注册成功
//            Debug.Log($"[LoginUIControl] 注册成功！用户ID: {userId}, 账号: {userSingAccount}");
//            ShowMessage("注册成功！请登录");

//            // 清空注册输入框
//            if (inputSingAccount != null) inputSingAccount.text = "";
//            if (inputSignPassWord != null) inputSignPassWord.text = "";
//            userSingAccount = "";
//            userSignPassWord = "";

//            // 自动切换到登录界面
//            if (isShowingPage1)
//            {
//                Flip();
//            }
//        }
//        else
//        {
//            // 注册失败（账号已存在）
//            Debug.LogWarning("[LoginUIControl] 注册失败：账号可能已存在");
//            ShowMessage("注册失败，账号已存在！");
//        }
//    }

//    // ==================== 提示信息显示 ====================

//    /// <summary>
//    /// 显示提示信息（自动在指定时间后隐藏）
//    /// 用法：ShowMessage("登录成功！");
//    /// </summary>
//    /// <param name="message">提示内容</param>
//    private void ShowMessage(string message)
//    {
//        if (提示文本 != null)
//        {
//            提示文本.text = message;
//        }
//        if (提示信息框 != null)
//        {
//            提示信息框.SetActive(true);
//            StartCoroutine(HideMessageAfterDelay(提示显示时长));
//        }
//        Debug.Log($"[LoginUIControl] 提示: {message}");
//    }

//    /// <summary>
//    /// 延迟隐藏提示信息框的协程
//    /// </summary>
//    private IEnumerator HideMessageAfterDelay(float delay)
//    {
//        yield return new WaitForSecondsRealtime(delay);
//        if (提示信息框 != null)
//        {
//            提示信息框.SetActive(false);
//        }
//    }

//    // ==================== 翻转动画 ====================

//    /// <summary>
//    /// 触发登录/注册界面的翻转切换动画
//    /// 用法：由 忘记密码/想起密码 按钮自动绑定调用
//    /// </summary>
//    public void Flip()
//    {
//        if (isFlipping) return;

//        isFlipping = true;
//        flipProgress = 0f;

//        if (isShowingPage1)
//        {
//            // 当前显示登录界面 → 翻转到注册界面
//            flipOutObj = 登录界面;
//            flipInObj = 注册界面;
//        }
//        else
//        {
//            // 当前显示注册界面 → 翻转到登录界面
//            flipOutObj = 注册界面;
//            flipInObj = 登录界面;
//        }

//        isShowingPage1 = !isShowingPage1;
//    }

//    /// <summary>
//    /// 更新翻转动画（每帧调用）
//    /// 动画分两阶段：
//    ///   阶段1 (0~0.5)：当前界面X轴从1缩放到0（压扁消失）
//    ///   阶段2 (0.5~1)：新界面X轴从0缩放到1（展开出现）
//    /// </summary>
//    private void UpdateFlipAnimation()
//    {
//        if (!isFlipping) return;

//        flipProgress += Time.deltaTime * flipSpeed;

//        if (flipProgress <= 0.5f)
//        {
//            // 阶段1：当前界面压扁消失
//            float scaleX = 1f - flipProgress * 2f;
//            if (flipOutObj != null)
//                flipOutObj.transform.localScale = new Vector3(scaleX, 1, 1);
//        }
//        else
//        {
//            // 阶段1结束时，隐藏旧界面
//            if (flipOutObj != null)
//                flipOutObj.transform.localScale = new Vector3(0, 1, 1);

//            // 阶段2：新界面展开出现
//            float scaleX = (flipProgress - 0.5f) * 2f;
//            if (flipInObj != null)
//                flipInObj.transform.localScale = new Vector3(scaleX, 1, 1);
//        }

//        // 动画完成
//        if (flipProgress >= 1f)
//        {
//            isFlipping = false;
//            if (flipOutObj != null)
//                flipOutObj.transform.localScale = new Vector3(0, 1, 1);
//            if (flipInObj != null)
//                flipInObj.transform.localScale = new Vector3(1, 1, 1);
//        }
//    }

//    // ==================== 输入框回调 ====================

//    /// <summary>
//    /// 登录账号输入回调
//    /// </summary>
//    private void OnUserInputAccount(string account)
//    {
//        userAccount = account;
//    }

//    /// <summary>
//    /// 登录密码输入回调
//    /// </summary>
//    private void OnUserInputPassWord(string pwd)
//    {
//        userPassWord = pwd;
//    }

//    /// <summary>
//    /// 注册账号输入回调
//    /// </summary>
//    private void OnSignInputAccount(string account)
//    {
//        userSingAccount = account;
//    }

//    /// <summary>
//    /// 注册密码输入回调
//    /// </summary>
//    private void OnSignInputPassWord(string pwd)
//    {
//        userSignPassWord = pwd;
//    }

//    // ==================== 清理工作 ====================

//    /// <summary>
//    /// 销毁时移除监听，防止内存泄漏，并释放数据库连接
//    /// </summary>
//    void OnDestroy()
//    {
//        // 登录面板解绑
//        if (inputAccount != null) inputAccount.onEndEdit.RemoveListener(OnUserInputAccount);
//        if (inputPassWord != null) inputPassWord.onEndEdit.RemoveListener(OnUserInputPassWord);

//        // 注册面板解绑
//        if (inputSingAccount != null) inputSingAccount.onEndEdit.RemoveListener(OnSignInputAccount);
//        if (inputSignPassWord != null) inputSignPassWord.onEndEdit.RemoveListener(OnSignInputPassWord);

//        // 释放数据库连接
//        if (db != null)
//        {
//            db.Dispose();
//            db = null;
//        }
//    }
//}

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LoginUIControl : MonoBehaviour
{
    // ==================== 存储数据的变量 ====================
    private string userAccount;
    private string userPassWord;
    private string userSingAccount;
    private string userSignPassWord;

    // ==================== 输入框绑定 ====================
    [SerializeField] private TMP_InputField inputAccount;
    [SerializeField] private TMP_InputField inputPassWord;
    [SerializeField] private TMP_InputField inputSingAccount;
    [SerializeField] private TMP_InputField inputSignPassWord;

    // ==================== 按钮绑定 ====================
    public Button 登入;
    public Button 注册;
    public Button 忘记密码;
    public Button 想起密码;

    // ==================== 界面翻转设置 ====================
    [Header("翻转页面设置")]
    public GameObject 注册界面;
    public GameObject 登录界面;

    [Header("动画参数")]
    [Range(1f, 15f)]
    public float flipSpeed = 10f;

    // ==================== 提示信息 UI ====================
    [Header("提示信息")]
    public GameObject 提示信息框;
    public TMP_Text 提示文本;
    public float 提示显示时长 = 2f;

    // ==================== 登录成功回调 ====================
    [Header("登录成功回调")]
    public UnityEngine.Events.UnityEvent OnLoginSuccess;

    // ==================== 内部状态 ====================
    private bool isFlipping = false;
    private float flipProgress = 0f;
    private GameObject flipOutObj;
    private GameObject flipInObj;
    private bool isShowingPage1 = true;

    // ==================== 数据库相关 ====================
    private SQLDBCommand db;
    private string dbPath;

    // ==================== 当前登录用户 ====================
    public static int CurrentUserId { get; private set; } = -1;
    public static string CurrentAccount { get; private set; } = "";

    // ==================== 生命周期方法 ====================
    void Start()
    {
        // 初始化数据库
        dbPath = Application.dataPath + "/../Save/SaveData.db";
        db = new SQLDBCommand(dbPath);

        // 绑定输入框回调
        if (inputAccount != null) inputAccount.onEndEdit.AddListener(OnUserInputAccount);
        if (inputPassWord != null) inputPassWord.onEndEdit.AddListener(OnUserInputPassWord);
        if (inputSingAccount != null) inputSingAccount.onEndEdit.AddListener(OnSignInputAccount);
        if (inputSignPassWord != null) inputSignPassWord.onEndEdit.AddListener(OnSignInputPassWord);

        // 绑定按钮回调
        if (登入 != null) 登入.onClick.AddListener(OnLoginButtonClick);
        if (注册 != null) 注册.onClick.AddListener(OnRegisterButtonClick);
        if (忘记密码 != null) 忘记密码.onClick.AddListener(Flip);
        if (想起密码 != null) 想起密码.onClick.AddListener(Flip);

        // 初始显示登录界面
        if (登录界面 != null)
        {
            登录界面.SetActive(true);
            登录界面.transform.localScale = new Vector3(1, 1, 1);
        }
        if (注册界面 != null)
        {
            注册界面.SetActive(false);
            注册界面.transform.localScale = new Vector3(0, 1, 1);
        }

        // 隐藏提示信息框
        if (提示信息框 != null) 提示信息框.SetActive(false);
    }

    void Update()
    {
        UpdateFlipAnimation();
    }

    // ==================== 登录按钮回调 ====================
    private void OnLoginButtonClick()
    {
        // 输入校验
        if (string.IsNullOrEmpty(userAccount))
        {
            ShowMessage("请输入账号！");
            return;
        }
        if (string.IsNullOrEmpty(userPassWord))
        {
            ShowMessage("请输入密码！");
            return;
        }

        // 调用数据库登录验证
        int userId = db.LoginUser(userAccount, userPassWord);
        if (userId > 0)
        {
            // 登录成功
            CurrentUserId = userId;
            CurrentAccount = userAccount;
            Debug.Log($"[LoginUIControl] 登录成功！用户ID: {userId}, 账号: {userAccount}");
            ShowMessage("登录成功！");

            // 触发登录成功事件
            if (OnLoginSuccess != null)
            {
                OnLoginSuccess.Invoke();
            }
        }
        else
        {
            // 登录失败
            if (userId == -2)
            {
                Debug.LogWarning("[LoginUIControl] 登录失败：账号不存在");
                ShowMessage("账号不存在！");
            }
            else if (userId == -3)
            {
                Debug.LogWarning("[LoginUIControl] 登录失败：密码错误");
                ShowMessage("密码错误！");
            }
            else
            {
                Debug.LogWarning("[LoginUIControl] 登录失败：未知错误");
                ShowMessage("登录失败，请重试！");
            }
        }
    }

    // ==================== 注册按钮回调 ====================
    private void OnRegisterButtonClick()
    {
        // 输入校验
        if (string.IsNullOrEmpty(userSingAccount))
        {
            ShowMessage("请输入注册账号！");
            return;
        }
        if (string.IsNullOrEmpty(userSignPassWord))
        {
            ShowMessage("请输入注册密码！");
            return;
        }
        if (userSingAccount.Length < 3)
        {
            ShowMessage("账号长度不能少于3位！");
            return;
        }
        if (userSignPassWord.Length < 4)
        {
            ShowMessage("密码长度不能少于4位！");
            return;
        }

        // 调用数据库注册
        int userId = db.RegisterUser(userSingAccount, userSignPassWord, userSingAccount);
        if (userId > 0)
        {
            // 注册成功
            Debug.Log($"[LoginUIControl] 注册成功！用户ID: {userId}, 账号: {userSingAccount}");
            ShowMessage("注册成功！请登录");

            // 清空注册输入框
            if (inputSingAccount != null) inputSingAccount.text = "";
            if (inputSignPassWord != null) inputSignPassWord.text = "";
            userSingAccount = "";
            userSignPassWord = "";

            // 自动切换到登录界面
            if (isShowingPage1)
            {
                Flip();
            }
        }
        else
        {
            // 注册失败
            if (userId == -2)
            {
                Debug.LogWarning("[LoginUIControl] 注册失败：账号已存在");
                ShowMessage("账号已存在，请更换！");
            }
            else
            {
                Debug.LogWarning("[LoginUIControl] 注册失败：未知错误");
                ShowMessage("注册失败，请重试！");
            }
        }
    }

    // ==================== 提示信息显示 ====================
    private void ShowMessage(string message)
    {
        if (提示文本 != null)
        {
            提示文本.text = message;
        }
        if (提示信息框 != null)
        {
            提示信息框.SetActive(true);
            StartCoroutine(HideMessageAfterDelay(提示显示时长));
        }
        Debug.Log($"[LoginUIControl] 提示: {message}");
    }

    private IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (提示信息框 != null)
        {
            提示信息框.SetActive(false);
        }
    }

    // ==================== 翻转动画 ====================
    public void Flip()
    {
        if (isFlipping) return;
        isFlipping = true;
        flipProgress = 0f;

        if (isShowingPage1)
        {
            // 当前显示登录界面 → 翻转到注册界面
            flipOutObj = 登录界面;
            flipInObj = 注册界面;
        }
        else
        {
            // 当前显示注册界面 → 翻转到登录界面
            flipOutObj = 注册界面;
            flipInObj = 登录界面;
        }

        // 确保翻转前的界面可见，翻转后的界面隐藏但准备好缩放
        if (flipOutObj != null) flipOutObj.SetActive(true);
        if (flipInObj != null)
        {
            flipInObj.SetActive(true);
            flipInObj.transform.localScale = new Vector3(0, 1, 1);
        }

        isShowingPage1 = !isShowingPage1;
    }

    private void UpdateFlipAnimation()
    {
        if (!isFlipping) return;

        flipProgress += Time.deltaTime * flipSpeed;

        if (flipProgress <= 0.5f)
        {
            // 阶段1：当前界面压扁消失
            float scaleX = 1f - flipProgress * 2f;
            if (flipOutObj != null)
            {
                flipOutObj.transform.localScale = new Vector3(Mathf.Max(0, scaleX), 1, 1);
            }
        }
        else
        {
            // 阶段1结束时，完全隐藏旧界面
            if (flipOutObj != null && flipProgress >= 0.5f && flipOutObj.activeSelf)
            {
                flipOutObj.transform.localScale = new Vector3(0, 1, 1);
            }

            // 阶段2：新界面展开出现
            float scaleX = (flipProgress - 0.5f) * 2f;
            if (flipInObj != null)
            {
                flipInObj.transform.localScale = new Vector3(Mathf.Min(1, scaleX), 1, 1);
            }
        }

        // 动画完成
        if (flipProgress >= 1f)
        {
            isFlipping = false;
            if (flipOutObj != null) flipOutObj.transform.localScale = new Vector3(0, 1, 1);
            if (flipInObj != null) flipInObj.transform.localScale = new Vector3(1, 1, 1);
        }
    }

    // ==================== 输入框回调 ====================
    private void OnUserInputAccount(string account) { userAccount = account; }
    private void OnUserInputPassWord(string pwd) { userPassWord = pwd; }
    private void OnSignInputAccount(string account) { userSingAccount = account; }
    private void OnSignInputPassWord(string pwd) { userSignPassWord = pwd; }

    // ==================== 清理工作 ====================
    void OnDestroy()
    {
        // 登录面板解绑
        if (inputAccount != null) inputAccount.onEndEdit.RemoveListener(OnUserInputAccount);
        if (inputPassWord != null) inputPassWord.onEndEdit.RemoveListener(OnUserInputPassWord);

        // 注册面板解绑
        if (inputSingAccount != null) inputSingAccount.onEndEdit.RemoveListener(OnSignInputAccount);
        if (inputSignPassWord != null) inputSignPassWord.onEndEdit.RemoveListener(OnSignInputPassWord);

        // 释放数据库连接
        if (db != null)
        {
            db.Dispose();
            db = null;
        }
    }
}
