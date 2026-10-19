using System;
using System.Linq;
using DataClass;
using UnityEngine;
using XCharts.Runtime;

/// <summary>
/// 显示当前月份内每天的实际值和平均值趋势。
/// </summary>
public class SmoothAreaDayData : MonoBehaviour
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
        title.text = $"{EnergyDataUtility.DisplayName(energyType)}日耗趋势 {today:yyyy-MM}<size=24> Daily energy trend</size>";

        if (ConnectMySQL.instance == null || ConnectMySQL.instance.dataSO == null)
        {
            title.subText = "数据连接尚未就绪";
            return;
        }

        var items = ConnectMySQL.instance.dataSO.dayEnergyConsumptions
            .Where(data => data != null
                           && EnergyDataUtility.MatchesEnergyType(data.Type, energyType)
                           && EnergyDataUtility.MatchesDatePart(data.Year, today.Year)
                           && EnergyDataUtility.MatchesDatePart(data.Month, today.Month)
                           && EnergyDataUtility.ParseDatePart(data.Day) != int.MaxValue)
            .GroupBy(data => EnergyDataUtility.ParseDatePart(data.Day))
            .Select(group => group.OrderByDescending(data => data.Id).First())
            .OrderBy(data => EnergyDataUtility.ParseDatePart(data.Day))
            .ToList();

        if (items.Count == 0)
        {
            title.subText = "当月暂无每日数据";
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

            int day = EnergyDataUtility.ParseDatePart(item.Day);
            chart.AddXAxisData($"{day}日");
            chart.AddData(0, actualValue, "实际值");
            chart.AddData(1, averageValue, "平均值");
        }
    }
}
