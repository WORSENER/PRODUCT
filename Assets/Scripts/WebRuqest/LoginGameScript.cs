using System;
using UnityEngine;
using UnityEngine.UI;
using MySql.Data.MySqlClient;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoginGameScript : MonoBehaviour
{
    // 数据库连接相关参数
    //private MySqlConnection connection;
    //public string serverName = "yd.hoyangmt.com"; //localhost
    //public string dbName = "hoyang";    // 数据库名
    //public string userName = "root";    // 登录数据库的用户名
    //public string password = "Hoyang123";    // 登录数据库的密码
    //public string port = "8125";         // MySQL数据库的端口号

    // 登录面板
    [Header("登录面板")]
    [Tooltip("登录页面")]
    public Transform loginPanel;

    [Tooltip("用户名输入框")]
    public InputField inputUsername;

    [Tooltip("密码输入框")]
    public InputField inputPassword;

    [Tooltip("登录按钮")]
    public Button loginBtn;

    [Tooltip("注册页面按钮")]
    public Button goToRegisterPlanelBtn;
    [Tooltip("标题中文")]
    public Text titleCN;
    [Tooltip("标题英文")]
    public Text titleEN;
    [Space]

    [Header("注册面板")]
    [Tooltip("注册面板")]
    public Transform registerPanel;

    [Tooltip("设置用户名")]
    public InputField setUsername;

    [Tooltip("设置密码")]
    public InputField setPassword;

    [Tooltip("确认密码")]
    public InputField setPasswordAgain;

    [Tooltip("注册按钮")]
    public Button registerBtn;

    [Tooltip("返回按钮")]
    public Button goLoginPlanelBtn;

    [Header("加载页面")]
    [Tooltip("加载进度页面")]
    public Transform progressPanel;

    [Tooltip("加载进度条")]
    public Image progressBar; // 加载进度条

    [Tooltip("加载进度百分比显示")]
    public Text progressText; // 加载进度显示

    [Space]
    public Text tips;

    void Awake()
    {
        //// 获取登录页面的各个UI控件
        //loginPanel = transform.Find("LoginPanel");
        //inputUsername = loginPanel.Find("InputUsername").GetComponent<InputField>();
        //inputPassword = loginPanel.Find("InputPassword").GetComponent<InputField>();
        //loginBtn = loginPanel.Find("LoginBtn").GetComponent<Button>();
        //goToRegisterPlanelBtn = loginPanel.Find("GoToRegisterPlanelBtn").GetComponent<Button>();

        // 为登录按钮和跳转到注册页面按钮添加事件
        loginBtn.onClick.AddListener(Login);
        goToRegisterPlanelBtn.onClick.AddListener(goToRegisterPanel);

        //// 获取注册页面的各个UI控件
        //registerPanel = transform.Find("RegisterPanel");
        //setUsername = registerPanel.Find("SetUsername").GetComponent<InputField>();
        //setPassword = registerPanel.Find("SetPassword").GetComponent<InputField>();
        //registerBtn = registerPanel.Find("RegisterBtn").GetComponent<Button>();
        //goLoginPlanelBtn = registerPanel.Find("GoLoginPlanelBtn").GetComponent<Button>();

        // 为注册按钮和返回登录按钮添加事件
        registerBtn.onClick.AddListener(Register);
        goLoginPlanelBtn.onClick.AddListener(goToLoginPanel);
    }

    void Start()
    {
        // 初始化显示登录面板，隐藏注册面板
        loginPanel.gameObject.SetActive(true);
        registerPanel.gameObject.SetActive(false);

        if (ConnectMySQL.instance.connection != null && ConnectMySQL.instance.connection.State == System.Data.ConnectionState.Open)
        {
            tips.text = "数据库连接成功！";
        }
        else
        {
            tips.text = "数据库连接失败！";
        }
    }

    private void OnEnable()
    {
        //Debug.Log("11111111");
        //Debug.Log(ConnectMySQL.instance.ConnectMysqlServer());
    }

    private void Update()
    {
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            tips.text = string.Empty;
        }
    }

    // 注册方法
    void Register()
    {
        string username = setUsername.text;
        string password = setPassword.text;
        string passwordAgain = setPasswordAgain.text;

        // 检查用户名和密码是否为空
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordAgain))
        {
            Debug.Log("用户名或密码不能为空！");
            tips.text = "不能为空！";
            return;
        }
        if (password != passwordAgain)
        {
            Debug.Log("两次输入的密码不一致！");
            tips.text = "两次密码不一致！";
            return;
        }

        // 检查用户名是否已经存在
        string checkQuery = "SELECT COUNT(*) FROM users WHERE username=@username";
        MySqlCommand checkCmd = new MySqlCommand(checkQuery, ConnectMySQL.instance.connection);
        checkCmd.Parameters.AddWithValue("@username", username);
        object checkResult = checkCmd.ExecuteScalar();
        int userCount = Convert.ToInt32(checkResult);

        if (userCount > 0)
        {
            Debug.Log("用户名已存在！");
            tips.text = "用户名已存在！";
            return;
        }

        // 用户名不存在，执行注册
        string query = "INSERT INTO users(username, password) VALUES (@username, @password)";
        MySqlCommand cmd = new MySqlCommand(query, ConnectMySQL.instance.connection);
        cmd.Parameters.AddWithValue("@username", username);
        cmd.Parameters.AddWithValue("@password", password);
        int rowsAffected = cmd.ExecuteNonQuery();

        if (rowsAffected > 0)
        {
            Debug.Log("注册成功！");
            tips.text = "注册成功！";
        }
        else
        {
            tips.text = "注册失败！";
            Debug.Log("注册失败！");
        }
    }

    // 登录方法
    void Login()
    {
        string username = inputUsername.text;
        string password = inputPassword.text;

        // 检查用户名和密码是否为空
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            tips.text = "用户名或密码不能为空！";
            Debug.Log("用户名或密码不能为空！");
            return;
        }
        try
        {
            // 检查用户名和密码是否匹配
            string query = "SELECT COUNT(*) FROM users WHERE username=@username AND password=@password";
            MySqlCommand cmd = new MySqlCommand(query, ConnectMySQL.instance.connection);
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@password", password);

            object result = cmd.ExecuteScalar();
            int count = Convert.ToInt32(result);

            if (count > 0)
            {
                tips.text = "登录成功！";
                Debug.Log("登录成功！");
                loginPanel.gameObject.SetActive(false);
                registerPanel.gameObject.SetActive(false);
                titleCN.gameObject.SetActive(false);
                titleEN.gameObject.SetActive(false);
                progressPanel.gameObject.SetActive(true);

                // 异步加载游戏场景
                StartCoroutine(LoadSceneAsync(1));
            }
            else
            {
                tips.text = "登录失败！";
                Debug.Log("登录失败！");
            }
        }
        catch
        {
            StartCoroutine(ConnectMySQL.instance.ConnectMysqlServer());
        }
    }

    // 异步加载场景协程方法
    IEnumerator LoadSceneAsync(int sceneIndex)
    {
        AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneIndex);
        tips.gameObject.SetActive(false);

        // 在加载过程中，更新进度条
        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            progressBar.fillAmount = progress;
            progressText.text = (progress * 100).ToString("F0") + "%";
            Debug.Log("加载进度: " + (progress * 100) + "%");

            yield return null; // 等待一帧
        }
    }

    // 切换到注册页面
    void goToRegisterPanel()
    {
        loginPanel.gameObject.SetActive(false);
        registerPanel.gameObject.SetActive(true);
        inputUsername.text = "";
        inputPassword.text = "";
    }

    // 切换到登录页面
    void goToLoginPanel()
    {
        loginPanel.gameObject.SetActive(true);
        registerPanel.gameObject.SetActive(false);
        setUsername.text = "";
        setPassword.text = "";
    }
}
