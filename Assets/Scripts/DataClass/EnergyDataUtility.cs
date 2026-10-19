using System;
using System.Globalization;

namespace DataClass
{
    /// <summary>
    /// 图表数据的统一日期、能源类型和数值转换规则。
    /// 数据库中的年/月/日可以保存为 7 或 07，本类都会按数字比较。
    /// </summary>
    public static class EnergyDataUtility
    {
        public static bool MatchesDatePart(string value, int expected)
        {
            return int.TryParse(value?.Trim(), out int parsed) && parsed == expected;
        }

        public static int ParseDatePart(string value)
        {
            return int.TryParse(value?.Trim(), out int parsed) ? parsed : int.MaxValue;
        }

        public static bool MatchesDay(string year, string month, string day, DateTime target)
        {
            return MatchesDatePart(year, target.Year)
                   && MatchesDatePart(month, target.Month)
                   && MatchesDatePart(day, target.Day);
        }

        public static bool MatchesMonth(string year, string month, DateTime target)
        {
            return MatchesDatePart(year, target.Year)
                   && MatchesDatePart(month, target.Month);
        }

        public static bool MatchesYear(string year, DateTime target)
        {
            return MatchesDatePart(year, target.Year);
        }

        public static string NormalizeEnergyType(string value)
        {
            string normalized = value?.Trim();
            switch (normalized)
            {
                case "水能耗": return "水";
                case "电能耗": return "电";
                case "气":
                case "天然气": return "燃气";
                case "压缩空气": return "压缩气";
                case "碳": return "碳排放";
                default: return normalized ?? string.Empty;
            }
        }

        public static bool MatchesEnergyType(string value, string expected)
        {
            return string.Equals(
                NormalizeEnergyType(value),
                NormalizeEnergyType(expected),
                StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryParseValue(string value, out float result)
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
            {
                return true;
            }

            return float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result);
        }

        public static string DefaultUnit(string energyType)
        {
            switch (NormalizeEnergyType(energyType))
            {
                case "水": return "m³";
                case "电": return "kWh";
                case "燃气": return "m³";
                case "压缩气": return "m³";
                case "碳排放": return "kgCO₂e";
                default: return string.Empty;
            }
        }

        public static string DisplayName(string energyType)
        {
            switch (NormalizeEnergyType(energyType))
            {
                case "水": return "水能耗";
                case "电": return "电能耗";
                case "燃气": return "燃气能耗";
                case "压缩气": return "压缩气能耗";
                case "碳排放": return "碳排放";
                default: return energyType;
            }
        }
    }
}
