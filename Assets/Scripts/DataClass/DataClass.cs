using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DataClass
{


    public class DataClass
    {

    }
    /// <summary>
    /// 能源消耗基类
    /// </summary>
    [Serializable]
    public class EnergyConsumptionBase
    {
        public int Id;//id
        public string CreateMember;//创建人
        public string CreateTime;//创建时间
        public string LastModifyTime;//最后修改时间
        public string LastModifier;//最后修改人
        public string Year;//年
        public string Type;//类型
                           //public string Type { get; set; }
                           //public decimal? ActualValue { get; set; }
                           //public decimal? AverageValue { get; set; }
                           //public string Unit { get; set; }
    }
    /// <summary>
    /// 年总能耗
    /// </summary>
    [Serializable]
    public class YearEnergyConsumption : EnergyConsumptionBase
    {
        public string ActualValue;//实际值，使用字符串保留数据库小数精度
        public string AverageValue;//平均值，使用字符串保留数据库小数精度
        public string Unit;//单位
    }
    /// <summary>
    /// 月总能耗
    /// </summary>
    [Serializable]
    public class MonthEnergyConsumption : EnergyConsumptionBase
    {
        public string Month;//月
        public string ActualValue;//实际值
        public string AverageValue;//平均值
        public string Unit;//单位
    }
    /// <summary>
    /// 日总能耗
    /// </summary>
    [Serializable]
    public class DayEnergyConsumption : EnergyConsumptionBase
    {
        public string Month;//月
        public string Day;//天
        public string ActualValue;//实际值
        public string AverageValue;//平均值
        public string Unit;//单位
    }
    /// <summary>
    /// 年部门总能耗
    /// </summary>
    [Serializable]
    public class YearDepartmentEnergyConsumption : EnergyConsumptionBase
    {
        public string Department;//部门
        public string Value;//能耗值
        public string Ratio;//占比
        public string Unit; //单位
    }
    /// <summary>
    /// 月部门总能耗
    /// </summary>
    [Serializable]
    public class MonthDepartmentEnergyConsumption : EnergyConsumptionBase
    {
        public string Month;//月
        public string Department;//部门
        public string Value;//能耗值
        public string Ratio;//占比
        public string Unit;//单位
    }
    /// <summary>
    /// 日部门总能耗
    /// </summary>
    [Serializable]
    public class DayDepartmentEnergyConsumption : EnergyConsumptionBase
    {
        public string Month;//月
        public string Day;//日
        public string Department;//部门
        public string Value;//能耗值
        public string Ratio;//占比
        public string Unit; //单位
    }

    /// <summary>
    /// 欢迎语
    /// </summary>
    [Serializable]
    public class WelcomeMessage
    {
        public string topMessage;
        public string middleMessage;
        public string bottomMessage;
        public string date;

    }

    /// <summary>
    /// 建筑物上方的字体信息
    /// </summary>
    [Serializable]
    public class FloorInfo
    {
        public string A;
        public string B_Ya;
        public string B_Zhong;
        public string C;
        public string D;
        public string E;
        public string F;
        public string G;
        public string H;
        public string Kfh_Jing;
        public string Kfh_Hou;
        public string Kfh_Ya;
        public string Comprehensive_Office;
    }
}
