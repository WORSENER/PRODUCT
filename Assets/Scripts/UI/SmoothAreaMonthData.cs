using System;
using System.Linq;
using DataClass;
using UnityEngine;
using XCharts.Runtime;

/// <summary>
/// 显示当前年份内每个月的实际值和平均值趋势。
/// </summary>
public class SmoothAreaMonthData : MonoBehaviour
{
    public ConsumptionType consumptionType;

    private LineChart chart;

    private void OnEnable()
    {
        ConnectMySQL.DataRefreshed += RefreshChart;
        chart = GetComponent<LineChart>();
        RefreshChart();
    }

    private void OnDisable()
    {
        ConnectMySQL.DataRefreshed -= RefreshChart;
    }

    public void RefreshChart()
    {
        if (chart == null)
        {
            chart = GetComponent<LineChart>();
        }

        chart.ClearData();

        string energyType = consumptionType.ToString();
        DateTime today = DateTime.Now;
        var title = chart.EnsureChartComponent<Title>();
        var yAxis = chart.EnsureChartComponent<YAxis>();
        title.text = $"{EnergyDataUtility.DisplayName(energyType)}月耗趋势 {today:yyyy}<size=24> Monthly energy trend</size>";

        if (ConnectMySQL.instance == null || ConnectMySQL.instance.dataSO == null)
        {
            title.subText = "数据连接尚未就绪";
            return;
        }

        var items = ConnectMySQL.instance.dataSO.monthEnergyConsumptions
            .Where(data => data != null
                           && EnergyDataUtility.MatchesEnergyType(data.Type, energyType)
                           && EnergyDataUtility.MatchesDatePart(data.Year, today.Year)
                           && EnergyDataUtility.ParseDatePart(data.Month) != int.MaxValue)
            .GroupBy(data => EnergyDataUtility.ParseDatePart(data.Month))
            .Select(group => group.OrderByDescending(data => data.Id).First())
            .OrderBy(data => EnergyDataUtility.ParseDatePart(data.Month))
            .ToList();

        if (items.Count == 0)
        {
            title.subText = "当年暂无月度数据";
            yAxis.axisName.name = $"单位:{EnergyDataUtility.DefaultUnit(energyType)}";
            return;
        }

        string unit = string.IsNullOrWhiteSpace(items[0].Unit)
            ? EnergyDataUtility.DefaultUnit(energyType)
            : items[0].Unit.Trim();

        title.subText = string.Empty;
        yAxis.axisName.name = $"单位:{unit}";

        foreach (var item in items)
        {
            if (!EnergyDataUtility.TryParseValue(item.ActualValue, out float actualValue)
                || !EnergyDataUtility.TryParseValue(item.AverageValue, out float averageValue))
            {
                continue;
            }

            int month = EnergyDataUtility.ParseDatePart(item.Month);
            chart.AddXAxisData($"{month}月");
            chart.AddData(0, actualValue, "实际值");
            chart.AddData(1, averageValue, "平均值");
        }
    }
}

[SerializeField]
public enum ConsumptionType
{
    水,
    电,
    燃气,
    压缩气,
    碳排放
}
