using DataClass;
using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using Unity.VisualScripting;
using UnityEngine;

public class ConnectMySQL : MonoBehaviour
{
    /// <summary>
    /// 六张能耗表完成一轮读取后触发，供 ProductScene 图表统一刷新。
    /// </summary>
    public static event Action DataRefreshed;

    // 数据库连接相关参数
    public MySqlConnection connection;//连接
    public string serverName = "yd.hoyangmt.com";//localhost
    public string dbName = "hoyang";    // 数据库名
    public string userName = "root";    // 登录数据库的用户名
    public string password = "Hoyang123";    // 登录数据库的密码
    public string port = "8125";         // MySQL服务的端口号

    public DataSO dataSO;//数据文件
    public float readDataWaitTimer;
    public static ConnectMySQL instance;
    private bool isRefreshingWelcomeMessages;

    private const string DatabaseConfigDirectoryName = "XML";
    private const string DatabaseConfigFileName = "ConnectMySQL.xml";

    private sealed class WelcomeMessageRefreshResult
    {
        public readonly List<WelcomeMessage> Messages = new List<WelcomeMessage>();
        public string Error;
    }

    private void Awake()
    {
        LoadDatabaseConfig();

        if (dataSO == null)
        {
            dataSO = Resources.Load("MysqlData") as DataSO;
        }
        if (dataSO == null)
        {
            Debug.LogError("未找到 Resources/MysqlData.asset，无法保存数据库图表数据。");
            return;
        }
        dataSO.EnsureInitialized();
        StartCoroutine(ConnectMysqlServer());
        Application.runInBackground = true;
        //单例
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }else
        {
            Destroy(gameObject);
        }

    }

    /// <summary>
    /// 从程序目录/XML/ConnectMySQL.xml 加载数据库连接参数。
    /// 编辑器运行时程序目录为 Unity 项目根目录，打包后为 exe 所在目录。
    /// 配置文件缺失或读取失败时继续使用 Inspector 中的默认值。
    /// </summary>
    private void LoadDatabaseConfig()
    {
        string configPath = GetDatabaseConfigPath();
        if (!File.Exists(configPath))
        {
            Debug.LogWarning($"未找到数据库配置文件：{configPath}，将使用脚本默认连接参数。");
            return;
        }

        try
        {
            XmlDocument configDocument = new XmlDocument
            {
                XmlResolver = null
            };
            configDocument.Load(configPath);

            XmlElement root = configDocument.DocumentElement;
            if (root == null)
            {
                Debug.LogWarning($"数据库配置文件没有有效的根节点：{configPath}，将使用默认参数。");
                return;
            }

            serverName = ReadConfigValue(root, serverName, "ServerName", "Server Name", "Server", "server_name");
            dbName = ReadConfigValue(root, dbName, "DbName", "DatabaseName", "Database Name", "Database", "dabame", "db_name");
            userName = ReadConfigValue(root, userName, "UserName", "User Name", "Username", "User", "ysername", "user_name");
            password = ReadConfigValue(root, password, "Password", "Pwd", "password");
            port = ReadConfigValue(root, port, "Port", "port");

            Debug.Log($"已加载数据库配置：{configPath}（Server={serverName}, Database={dbName}, User={userName}, Port={port}）");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"读取数据库配置文件失败：{configPath}，将使用脚本默认连接参数。原因：{ex.Message}");
        }
    }

    private static string GetDatabaseConfigPath()
    {
        string programDirectory = null;
        if (!string.IsNullOrEmpty(Application.dataPath))
        {
            DirectoryInfo dataDirectory = Directory.GetParent(Application.dataPath);
            if (dataDirectory != null)
            {
                programDirectory = dataDirectory.FullName;
            }
        }

        if (string.IsNullOrEmpty(programDirectory))
        {
            programDirectory = AppContext.BaseDirectory;
        }

        return Path.Combine(programDirectory, DatabaseConfigDirectoryName, DatabaseConfigFileName);
    }

    private static string ReadConfigValue(XmlElement root, string fallback, params string[] elementNames)
    {
        foreach (XmlNode child in root.ChildNodes)
        {
            if (!(child is XmlElement element))
            {
                continue;
            }

            for (int i = 0; i < elementNames.Length; i++)
            {
                if (!string.Equals(element.Name, elementNames[i], StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string value = element.InnerText?.Trim();
                return string.IsNullOrEmpty(value) ? fallback : value;
            }
        }

        return fallback;
    }

    private void OnDestroy()
    {
        CloseConnection();
    }

    public IEnumerator ConnectMysqlServer()
    {
        string connectionString = $"Server={serverName};Database={dbName};Uid={userName};Pwd={password};Port={port};Pooling=true;";
        connection = new MySqlConnection(connectionString);

        bool isConnected = false;
        while (!isConnected)
        {
            // 在新线程中尝试连接数据库
            yield return new WaitUntil(() =>
            {
                try
                {
                    connection.Open();
                    isConnected = true;
                    Debug.Log("数据库连接成功！");
                    //TODO:开始读取数据
                    StartCoroutine(ReadMySQLData());
                }
                catch (Exception ex)
                {
                    isConnected = false;
                    Debug.LogError("数据库连接失败: " + ex.Message);
                }
                return true; // 不管是否连接成功，协程都会返回
            });

        }
    }

    /// <summary>
    /// 关闭数据库连接
    /// </summary>
    public void CloseConnection()
    {
        if (connection != null && connection.State == System.Data.ConnectionState.Open)
        {
            connection.Close();
            Debug.Log("数据库连接已关闭");
        }
    }

    public void RefreshMySQLData()
    {
        ReadData("day_energy_consumption");     
        ReadData("month_energy_consumption");
        ReadData("year_energy_consumption");     
        ReadData("day_department_energy_consumption");      
        ReadData("month_department_energy_consumption");
        ReadData("year_department_energy_consumption");
        ReadData("welcome_message");
        ReadData("floor_info");
        DataRefreshed?.Invoke();
    }

    /// <summary>
    /// 在后台只读取欢迎语表，并把结果更新到本地 DataSO 缓存。
    /// 不占用主线程，也不会触发能耗图表的整批刷新。
    /// </summary>
    public IEnumerator RefreshWelcomeMessagesAsync()
    {
        if (isRefreshingWelcomeMessages)
        {
            yield break;
        }

        isRefreshingWelcomeMessages = true;
        string connectionString = $"Server={serverName};Database={dbName};Uid={userName};Pwd={password};Port={port};CharSet=utf8;Pooling=true;Connection Timeout=5;Default Command Timeout=5;";

        Task<WelcomeMessageRefreshResult> refreshTask = Task.Run(() =>
        {
            WelcomeMessageRefreshResult result = new WelcomeMessageRefreshResult();
            try
            {
                using (MySqlConnection welcomeConnection = new MySqlConnection(connectionString))
                using (MySqlCommand command = new MySqlCommand(
                           "SELECT top_message, middle_message, bottom_message, date FROM welcome_message",
                           welcomeConnection))
                {
                    welcomeConnection.Open();
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Messages.Add(new WelcomeMessage
                            {
                                topMessage = reader.IsDBNull("top_message") ? null : reader.GetString("top_message"),
                                middleMessage = reader.IsDBNull("middle_message") ? null : reader.GetString("middle_message"),
                                bottomMessage = reader.IsDBNull("bottom_message") ? null : reader.GetString("bottom_message"),
                                date = reader.IsDBNull("date") ? null : reader.GetString("date")
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
            }

            return result;
        });

        yield return new WaitUntil(() => refreshTask.IsCompleted);
        WelcomeMessageRefreshResult refreshResult = refreshTask.Result;
        isRefreshingWelcomeMessages = false;

        if (!string.IsNullOrEmpty(refreshResult.Error))
        {
            Debug.LogWarning($"后台刷新欢迎语失败，将继续使用本地缓存：{refreshResult.Error}");
            yield break;
        }

        dataSO.welcomeMessages.Clear();
        dataSO.welcomeMessages.AddRange(refreshResult.Messages);
    }
    
    public IEnumerator ReadMySQLData()
    {
        ReadData("day_energy_consumption");     
        ReadData("month_energy_consumption");
        ReadData("year_energy_consumption");     
        ReadData("day_department_energy_consumption");      
        ReadData("month_department_energy_consumption");
        ReadData("year_department_energy_consumption");
        ReadData("welcome_message");
        ReadData("floor_info");
        DataRefreshed?.Invoke();
        yield return new WaitForSeconds(readDataWaitTimer);
        // while (true)
        // {
        //      ReadData("welcome_message");
        //      ReadData("floor_info");
        //     yield return new WaitForSeconds(readDataWaitTimer);
        // }
    }

    // 查询某年的总能耗数据
    public YearEnergyConsumption QueryYearEnergyConsumptionByYear(string year)
    {
        return dataSO.yearEnergyConsumptions.Find(item => item.Year == year);
    }
    /// <summary>
    /// 读取数据方法
    /// </summary>
    /// <param name="tableName"></param>
    public void ReadData(string tableName)
    {
        // 构建查询语句
        string query = $"SELECT * FROM {tableName}";
        Debug.Log($"正在查询表：{tableName}");

        if (connection != null && connection.State == System.Data.ConnectionState.Open)
        {
            MySqlCommand cmd = new MySqlCommand(query, connection);
            MySqlDataReader reader = null;

            try
            {
                reader = cmd.ExecuteReader();

                // 根据表名将数据解析并添加到对应的 DataSO 列表中
                switch (tableName)
                {
                    case "day_energy_consumption":
                        dataSO.dayEnergyConsumptions.Clear();//可去
                        while (reader.Read())
                        {
                            DayEnergyConsumption dayData = new DayEnergyConsumption
                            {
                                Id = reader.GetInt32("id"),
                                CreateMember = reader.IsDBNull("create_member") ?null:reader.GetString("create_member"),
                                CreateTime = reader.IsDBNull("create_time") ? null : reader.GetDateTime("create_time").ToString(),
                                LastModifyTime = reader.IsDBNull("last_modify_time") ? null : reader.GetDateTime("last_modify_time").ToString(),
                                LastModifier = reader.IsDBNull("last_modifier") ? null : reader.GetString("last_modifier"),
                                Year = reader.IsDBNull("year") ? null :reader.GetString("year"),
                                Month = reader.IsDBNull("month") ? null: reader.GetString("month"),
                                Day = reader.IsDBNull("day") ? null :reader.GetString("day"),
                                ActualValue = reader.IsDBNull("actual_value") ? null : reader.GetDecimal("actual_value").ToString(),
                                AverageValue = reader.IsDBNull("average_value") ? null : reader.GetDecimal("average_value").ToString(),
                                Unit = reader.IsDBNull("unit") ? null : reader.GetString("unit"),
                                Type = reader.IsDBNull("Type") ? null : reader.GetString("Type")
                            };
                            dataSO.dayEnergyConsumptions.Add(dayData);
                        }
                        Debug.Log($"已加载 {dataSO.dayEnergyConsumptions.Count} 条日能耗数据");
                        break;

                    case "month_energy_consumption":
                        dataSO.monthEnergyConsumptions.Clear();
                        while (reader.Read())
                        {
                            MonthEnergyConsumption monthData = new MonthEnergyConsumption
                            {
                                Id = reader.GetInt32("id"),
                                CreateMember = reader.IsDBNull("create_member") ?null:reader.GetString("create_member"),
                                CreateTime = reader.IsDBNull("create_time") ? null : reader.GetString("create_time"),
                                LastModifyTime = reader.IsDBNull("last_modify_time") ? null : reader.GetString("last_modify_time"),
                                LastModifier = reader.IsDBNull("last_modifier") ? null : reader.GetString("last_modifier"),
                                Year = reader.IsDBNull("year") ? null : reader.GetString("year"),
                                Month = reader.IsDBNull("month") ? null : reader.GetString("month"),
                                ActualValue = reader.IsDBNull("actual_value") ? null : reader.GetString("actual_value"),
                                AverageValue = reader.IsDBNull("average_value") ? null : reader.GetString("average_value"),
                                Unit = reader.IsDBNull("unit") ? null : reader.GetString("unit"),
                                Type= reader.IsDBNull("type") ? null : reader.GetString("type")
                            };
                            dataSO.monthEnergyConsumptions.Add(monthData);
                        }
                        Debug.Log($"已加载 {dataSO.monthEnergyConsumptions.Count} 条月能耗数据");
                        break;

                    case "year_energy_consumption":
                        dataSO.yearEnergyConsumptions.Clear();
                        while (reader.Read())
                        {
                            YearEnergyConsumption yearData = new YearEnergyConsumption
                            {
                                Id = reader.GetInt32("id"),
                                CreateMember = reader.IsDBNull("create_member") ? null : reader.GetString("create_member"),
                                CreateTime = reader.IsDBNull("create_time") ? null : reader.GetDateTime("create_time").ToString(),
                                LastModifyTime = reader.IsDBNull("last_modify_time") ? null : reader.GetDateTime("last_modify_time").ToString(),
                                LastModifier = reader.IsDBNull("last_modifier") ? null : reader.GetString("last_modifier"),
                                Year = reader.IsDBNull("year") ? null : reader.GetInt32("year").ToString(),
                                ActualValue = reader.IsDBNull("actual_value") ? null : reader.GetDecimal("actual_value").ToString(CultureInfo.InvariantCulture),
                                AverageValue = reader.IsDBNull("average_value") ? null : reader.GetDecimal("average_value").ToString(CultureInfo.InvariantCulture),
                                Unit = reader.IsDBNull("unit") ? null : reader.GetString("unit"),
                                Type = reader.IsDBNull("type") ? null : reader.GetString("type")
                            };
                            dataSO.yearEnergyConsumptions.Add(yearData);
                        }
                        Debug.Log($"已加载 {dataSO.yearEnergyConsumptions.Count} 条年能耗数据");
                        break;
                    case "day_department_energy_consumption":
                        dataSO.dayDepartmentEnergyConsumptions.Clear();
                        while (reader.Read())
                        {
                            DayDepartmentEnergyConsumption dayDepartmentData = new DayDepartmentEnergyConsumption
                            {
                                Id = reader.GetInt32("id"),
                                CreateMember = reader.IsDBNull("create_member") ? null : reader.GetString("create_member"),
                                CreateTime = reader.IsDBNull("create_time") ? null : reader.GetDateTime("create_time").ToString(),
                                LastModifyTime = reader.IsDBNull("last_modify_time") ? null : reader.GetDateTime("last_modify_time").ToString(),
                                LastModifier = reader.IsDBNull("last_modifier") ? null : reader.GetString("last_modifier"),
                                Year = reader.IsDBNull("year") ? null : reader.GetString("year"),
                                Month = reader.IsDBNull("month") ? null : reader.GetString("month"),
                                Day = reader.IsDBNull("day") ? null : reader.GetString("day"),
                                Unit = reader.IsDBNull("unit") ? null : reader.GetString("unit"),
                                Type = reader.IsDBNull("Type") ? null : reader.GetString("Type"),
                                Department = reader.IsDBNull("department") ? null : reader.GetString("department"),
                                Value=reader.IsDBNull("value")? null : reader.GetString("value"),
                                Ratio= reader.IsDBNull("ratio") ? null : reader.GetString("ratio")

                            };
                            dataSO.dayDepartmentEnergyConsumptions.Add(dayDepartmentData);
                        }
                        Debug.Log($"已加载 {dataSO.yearEnergyConsumptions.Count} 条年能耗数据");
                        break;
                    case "month_department_energy_consumption":
                        dataSO.monthDepartmentEnergyConsumptions.Clear();
                        while (reader.Read())
                        {
                            MonthDepartmentEnergyConsumption monthDepartmentData = new MonthDepartmentEnergyConsumption
                            {
                                Id = reader.GetInt32("id"),
                                CreateMember = reader.IsDBNull("create_member") ? null : reader.GetString("create_member"),
                                CreateTime = reader.IsDBNull("create_time") ? null : reader.GetDateTime("create_time").ToString(),
                                LastModifyTime = reader.IsDBNull("last_modify_time") ? null : reader.GetDateTime("last_modify_time").ToString(),
                                LastModifier = reader.IsDBNull("last_modifier") ? null : reader.GetString("last_modifier"),
                                Year = reader.IsDBNull("year") ? null : reader.GetString("year"),
                                Month = reader.IsDBNull("month") ? null : reader.GetString("month"),
                                Type = reader.IsDBNull("Type") ? null : reader.GetString("Type"),
                                Department = reader.IsDBNull("department") ? null : reader.GetString("department"),
                                Value = reader.IsDBNull("value") ? null : reader.GetString("value"),
                                Ratio = reader.IsDBNull("ratio") ? null : reader.GetString("ratio"),
                                Unit = reader.IsDBNull("unit") ? null : reader.GetString("unit")

                            };
                            dataSO.monthDepartmentEnergyConsumptions.Add(monthDepartmentData);
                        }
                        
                        break;
                    case "year_department_energy_consumption":
                        dataSO.yearDepartmentEnergyConsumptions.Clear();   
                        while(reader.Read())
                        {
                            YearDepartmentEnergyConsumption yearDepartmentData = new YearDepartmentEnergyConsumption
                            {
                                Id = reader.GetInt32("id"),
                                CreateMember = reader.IsDBNull("create_member") ? null : reader.GetString("create_member"),
                                CreateTime = reader.IsDBNull("create_time") ? null : reader.GetDateTime("create_time").ToString(),
                                LastModifyTime = reader.IsDBNull("last_modify_time") ? null : reader.GetDateTime("last_modify_time").ToString(),
                                LastModifier = reader.IsDBNull("last_modifier") ? null : reader.GetString("last_modifier"),
                                Year = reader.IsDBNull("year") ? null : reader.GetString("year"),
                                Type = reader.IsDBNull("Type") ? null : reader.GetString("Type"),
                                Department = reader.IsDBNull("department") ? null : reader.GetString("department"),
                                Value = reader.IsDBNull("value") ? null : reader.GetString("value"),
                                Ratio = reader.IsDBNull("ratio") ? null : reader.GetString("ratio"),
                                Unit = reader.IsDBNull("unit") ? null : reader.GetString("unit")
                            };
                            dataSO.yearDepartmentEnergyConsumptions.Add(yearDepartmentData);
                        }
                        break;
                    case "welcome_message":
                        dataSO.welcomeMessages.Clear();

                        while (reader.Read())
                        {
                            WelcomeMessage welcomeMessage = new WelcomeMessage
                            {
                                topMessage = reader.IsDBNull("top_message") ? null : reader.GetString("top_message"),
                                middleMessage = reader.IsDBNull("middle_message")? null:reader.GetString("middle_message"),
                                bottomMessage = reader.IsDBNull("bottom_message") ? null : reader.GetString("bottom_message"),
                                date=reader.IsDBNull("date")?null:reader.GetString("date")
                            };
                            dataSO.welcomeMessages.Add(welcomeMessage);
                        }
                        
                        break;
                    case "floor_info":
                        while (reader.Read())
                        {
                            FloorInfo floor = new FloorInfo
                            {
                                A = reader.IsDBNull("a") ? null : reader.GetString("a"),
                                B_Ya = reader.IsDBNull("b_ya") ? null : reader.GetString("b_ya"),
                                B_Zhong = reader.IsDBNull("b_zhong") ? null : reader.GetString("b_zhong"),
                                C = reader.IsDBNull("c") ? null : reader.GetString("c"),
                                D = reader.IsDBNull("d") ? null : reader.GetString("d"),
                                E = reader.IsDBNull("e") ? null : reader.GetString("e"),
                                F = reader.IsDBNull("f") ? null : reader.GetString("f"),
                                G = reader.IsDBNull("g") ? null : reader.GetString("g"),
                                H = reader.IsDBNull("h") ? null : reader.GetString("h"),
                                Kfh_Jing = reader.IsDBNull("kfh_jing") ? null : reader.GetString("kfh_jing"),
                                Kfh_Hou = reader.IsDBNull("kfh_hou") ? null : reader.GetString("kfh_hou"),
                                Kfh_Ya = reader.IsDBNull("kfh_ya") ? null : reader.GetString("kfh_ya"),
                                Comprehensive_Office = reader.IsDBNull("comprehensive_office") ? null : reader.GetString("comprehensive_office"),
                                
                            };
                            dataSO.floorInfo=floor;
                        }
                        break;
                    default:
                        Debug.Log("无此表名！");
                        break;
                        // 其他表的处理逻辑...
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"读取表 {tableName} 时发生错误：{ex.Message}");
            }
            finally
            {
                reader?.Close();
            }
        }
        else
        {
            Debug.LogWarning($"数据库未连接，暂时无法读取表 {tableName}");
        }
    }
    
}
