using System;
using System.Collections.Generic;
using DataClass;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 根据能源类型和日/月/年维度，显示当前周期内六个部门的能耗占比。
/// </summary>
public class CirculargraphData : MonoBehaviour
{
    public Image 前处理;
    public Image 涂装;
    public Image 精加工;
    public Image 压铸;
    public Image 后处理;
    public Image 重铸;

    public Text 前处理Ratio;
    public Text 涂装Ratio;
    public Text 精加工Ratio;
    public Text 压铸Ratio;
    public Text 后处理Ratio;
    public Text 重铸Ratio;

    public EnergyType energyType;
    public DateType dateType;
    public Text title;
    public Text date;
    public Text totalUsage;

    private static readonly string[] Departments =
    {
        "前处理", "涂装", "精加工", "压铸", "后处理", "重铸"
    };

    private void OnEnable()
    {
        ConnectMySQL.DataRefreshed += RefreshDisplay;
        RefreshDisplay();
    }

    private void OnDisable()
    {
        ConnectMySQL.DataRefreshed -= RefreshDisplay;
    }

    public void SetTitle()
    {
        string type = GetEnergyType();
        title.text = $"{EnergyDataUtility.DisplayName(type)} <size=30>{GetEnglishTitle(type)}</size>";

        switch (dateType)
        {
            case DateType.日:
                date.text = "<color=#84AAEF>日</color>/月/年";
                break;
            case DateType.月:
                date.text = "日/<color=#84AAEF>月</color>/年";
                break;
            case DateType.年:
                date.text = "日/月/<color=#84AAEF>年</color>";
                break;
        }
    }

    public void UpdatePercent()
    {
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        SetTitle();

        var values = new Dictionary<string, float>();
        foreach (string department in Departments)
        {
            values[department] = 0f;
        }

        if (ConnectMySQL.instance == null || ConnectMySQL.instance.dataSO == null)
        {
            ApplyChart(values, 0f, EnergyDataUtility.DefaultUnit(GetEnergyType()), "数据连接尚未就绪");
            return;
        }

        DateTime now = DateTime.Now;
        string type = GetEnergyType();
        string unit = string.Empty;

        switch (dateType)
        {
            case DateType.日:
                foreach (var item in ConnectMySQL.instance.dataSO.dayDepartmentEnergyConsumptions)
                {
                    if (item == null
                        || !EnergyDataUtility.MatchesEnergyType(item.Type, type)
                        || !EnergyDataUtility.MatchesDay(item.Year, item.Month, item.Day, now))
                    {
                        continue;
                    }

                    AddDepartmentValue(values, item.Department, item.Value);
                    unit = PickUnit(unit, item.Unit);
                }
                break;

            case DateType.月:
                foreach (var item in ConnectMySQL.instance.dataSO.monthDepartmentEnergyConsumptions)
                {
                    if (item == null
                        || !EnergyDataUtility.MatchesEnergyType(item.Type, type)
                        || !EnergyDataUtility.MatchesMonth(item.Year, item.Month, now))
                    {
                        continue;
                    }

                    AddDepartmentValue(values, item.Department, item.Value);
                    unit = PickUnit(unit, item.Unit);
                }
                break;

            case DateType.年:
                foreach (var item in ConnectMySQL.instance.dataSO.yearDepartmentEnergyConsumptions)
                {
                    if (item == null
                        || !EnergyDataUtility.MatchesEnergyType(item.Type, type)
                        || !EnergyDataUtility.MatchesYear(item.Year, now))
                    {
                        continue;
                    }

                    AddDepartmentValue(values, item.Department, item.Value);
                    unit = PickUnit(unit, item.Unit);
                }
                break;
        }

        float total = 0f;
        foreach (float value in values.Values)
        {
            total += value;
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            unit = EnergyDataUtility.DefaultUnit(type);
        }

        ApplyChart(values, total, unit, total > 0f ? null : "当前周期暂无数据");
    }

    private static void AddDepartmentValue(Dictionary<string, float> values, string department, string rawValue)
    {
        string normalizedDepartment = department?.Trim();
        if (!values.ContainsKey(normalizedDepartment ?? string.Empty)
            || !EnergyDataUtility.TryParseValue(rawValue, out float parsedValue))
        {
            return;
        }

        values[normalizedDepartment] += parsedValue;
    }

    private void ApplyChart(Dictionary<string, float> values, float total, string unit, string emptyMessage)
    {
        float p1 = SafeRatio(values["前处理"], total);
        float p2 = SafeRatio(values["涂装"], total);
        float p3 = SafeRatio(values["精加工"], total);
        float p4 = SafeRatio(values["压铸"], total);
        float p5 = SafeRatio(values["后处理"], total);
        float p6 = SafeRatio(values["重铸"], total);

        SetSegment(前处理, p1, 0f);
        SetSegment(涂装, p2, p1);
        SetSegment(精加工, p3, p1 + p2);
        SetSegment(压铸, p4, p1 + p2 + p3);
        SetSegment(后处理, p5, p1 + p2 + p3 + p4);
        SetSegment(重铸, p6, p1 + p2 + p3 + p4 + p5);

        SetRatioText(前处理Ratio, p1);
        SetRatioText(涂装Ratio, p2);
        SetRatioText(精加工Ratio, p3);
        SetRatioText(压铸Ratio, p4);
        SetRatioText(后处理Ratio, p5);
        SetRatioText(重铸Ratio, p6);

        totalUsage.text = string.IsNullOrEmpty(emptyMessage)
            ? $"{total:0.##}<size=30>{unit}</size>"
            : emptyMessage;
    }

    private static void SetSegment(Image image, float ratio, float startRatio)
    {
        if (image == null) return;
        image.fillAmount = ratio;
        image.transform.localEulerAngles = new Vector3(0f, 0f, -360f * startRatio);
    }

    private static void SetRatioText(Text text, float ratio)
    {
        if (text != null)
        {
            text.text = $"{ratio * 100f:0}%";
        }
    }

    private static float SafeRatio(float value, float total)
    {
        return total > 0f ? value / total : 0f;
    }

    private static string PickUnit(string current, string candidate)
    {
        return string.IsNullOrWhiteSpace(current) && !string.IsNullOrWhiteSpace(candidate)
            ? candidate.Trim()
            : current;
    }

    private string GetEnergyType()
    {
        switch (energyType)
        {
            case EnergyType.水能耗: return "水";
            case EnergyType.电能耗: return "电";
            case EnergyType.燃气: return "燃气";
            case EnergyType.碳排放: return "碳排放";
            case EnergyType.压缩气: return "压缩气";
            default: return string.Empty;
        }
    }

    private static string GetEnglishTitle(string type)
    {
        switch (EnergyDataUtility.NormalizeEnergyType(type))
        {
            case "水": return "Water energy";
            case "电": return "Electricity consumption";
            case "燃气": return "Gas consumption";
            case "碳排放": return "Carbon emission";
            case "压缩气": return "Compressed gas consumption";
            default: return string.Empty;
        }
    }

    [SerializeField]
    public enum EnergyType
    {
        水能耗,
        电能耗,
        燃气,
        碳排放,
        压缩气
    }

    [SerializeField]
    public enum DateType
    {
        日,
        月,
        年
    }
}
