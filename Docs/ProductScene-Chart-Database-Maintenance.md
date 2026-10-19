# ProductScene 图表数据库维护说明

ProductScene 现在始终使用运行设备的本地日期：

- 日表：只显示当天，例如 `2026-07-31`。
- 月表：只显示当月，例如 `2026-07`。
- 年表：只显示当年，例如 `2026`。

数据库中的 `year`、`month`、`day` 可以保存为数字或字符串，`7` 和 `07` 都能识别。仍建议统一使用数字字段，避免错误数据进入数据库。

## 一、统一枚举值

### 能源类型 type

生产数据建议只使用以下五个值：

| type | 建议单位 |
|---|---|
| 水 | m³ |
| 电 | kWh |
| 燃气 | m³ |
| 压缩气 | m³ |
| 碳排放 | kgCO₂e |

代码兼容 `水能耗`、`电能耗`、`气`、`天然气`、`压缩空气`、`碳` 等旧名称，但数据库应逐步清洗成上表中的标准值。

### 部门 department

部门占比环图只识别以下六个值：

1. 前处理
2. 涂装
3. 精加工
4. 压铸
5. 后处理
6. 重铸

不要在名称前后添加空格，也不要混用“糖加工”“精加工课”等名称。

## 二、总能耗表

### day_energy_consumption

用途：ProductScene 的“当天数据”折线图。

每个日期、每种能源只能有一条有效记录。

建议唯一键：

```sql
UNIQUE KEY uk_day_energy (`year`, `month`, `day`, `type`)
```

必须维护的字段：

| 字段 | 内容 | 示例 |
|---|---|---|
| year | 数据所属年 | 2026 |
| month | 数据所属月 | 7 |
| day | 数据所属日 | 31 |
| type | 标准能源类型 | 水 |
| actual_value | 当天实际用量 | 128.50 |
| average_value | 用于对比的日均/计划值 | 120.00 |
| unit | 单位 | m³ |

示例：

```sql
INSERT INTO day_energy_consumption
(`year`, `month`, `day`, `type`, actual_value, average_value, unit)
VALUES
(2026, 7, 31, '水', 128.50, 120.00, 'm³'),
(2026, 7, 31, '电', 2680.00, 2500.00, 'kWh'),
(2026, 7, 31, '燃气', 86.30, 90.00, 'm³'),
(2026, 7, 31, '压缩气', 310.00, 300.00, 'm³'),
(2026, 7, 31, '碳排放', 960.00, 900.00, 'kgCO₂e');
```

当天没有记录时，图表显示“当天暂无数据”，不会回退到历史日期。

### month_energy_consumption

用途：ProductScene 的“当月数据”折线图。

每个月、每种能源只能有一条有效记录。

建议唯一键：

```sql
UNIQUE KEY uk_month_energy (`year`, `month`, `type`)
```

必须维护：`year`、`month`、`type`、`actual_value`、`average_value`、`unit`。

示例：

```sql
INSERT INTO month_energy_consumption
(`year`, `month`, `type`, actual_value, average_value, unit)
VALUES
(2026, 7, '水', 3860.00, 3720.00, 'm³'),
(2026, 7, '电', 80600.00, 77500.00, 'kWh');
```

`actual_value` 应是当月累计值；`average_value` 的业务含义必须统一，可以是月计划值、历史同期值或月均基准，但不能在不同月份混用不同口径。

### year_energy_consumption

用途：顶部年度总览数字。

每年、每种能源只能有一条有效记录。

建议唯一键：

```sql
UNIQUE KEY uk_year_energy (`year`, `type`)
```

示例：

```sql
INSERT INTO year_energy_consumption
(`year`, `type`, actual_value, average_value, unit)
VALUES
(2026, '水', 26800.50, 26000.00, 'm³'),
(2026, '电', 568000.00, 550000.00, 'kWh'),
(2026, '燃气', 18200.00, 18000.00, 'm³'),
(2026, '碳排放', 196000.00, 190000.00, 'kgCO₂e');
```

顶部目前显示 `actual_value`。如果同一年同一类型存在重复记录，代码会取 id 最大的一条，但数据库仍必须通过唯一键避免重复。

## 三、部门能耗表

部门表用于计算六个部门在当前周期内的占比。图表不再使用数据库中的 `ratio`，而是按以下公式实时计算：

```text
部门占比 = 当前部门 value / 六个部门 value 总和
```

因此 `ratio` 可以不维护，或者仅作为报表冗余字段使用，不能把它作为 ProductScene 图表的数据源。

### day_department_energy_consumption

用途：当天部门占比环图。

建议唯一键：

```sql
UNIQUE KEY uk_day_department
(`year`, `month`, `day`, `type`, `department`)
```

同一天、同一种能源，应维护六个部门各一条数据：

```sql
INSERT INTO day_department_energy_consumption
(`year`, `month`, `day`, `type`, department, `value`, unit)
VALUES
(2026, 7, 31, '水', '前处理', 20.00, 'm³'),
(2026, 7, 31, '水', '涂装',   30.00, 'm³'),
(2026, 7, 31, '水', '精加工', 18.00, 'm³'),
(2026, 7, 31, '水', '压铸',   25.00, 'm³'),
(2026, 7, 31, '水', '后处理', 15.00, 'm³'),
(2026, 7, 31, '水', '重铸',   12.00, 'm³');
```

如果某部门当天没有消耗，仍建议写入 `value = 0`，这样能区分“确实为零”和“数据漏维护”。

### month_department_energy_consumption

用途：当月部门占比环图。

建议唯一键：

```sql
UNIQUE KEY uk_month_department
(`year`, `month`, `type`, `department`)
```

`value` 应保存该部门当月累计量。可以由日部门表汇总生成：

```sql
SELECT `year`, `month`, `type`, department,
       SUM(`value`) AS `value`, MAX(unit) AS unit
FROM day_department_energy_consumption
WHERE `year` = 2026 AND `month` = 7
GROUP BY `year`, `month`, `type`, department;
```

### year_department_energy_consumption

用途：当年部门占比环图。

建议唯一键：

```sql
UNIQUE KEY uk_year_department
(`year`, `type`, `department`)
```

`value` 应保存该部门当年累计量，可由月部门表汇总生成。

## 四、推荐的数据更新顺序

每天完成数据采集后，按以下顺序维护：

1. 写入或更新 `day_energy_consumption`。
2. 写入或更新 `day_department_energy_consumption` 六个部门的数据。
3. 汇总更新 `month_energy_consumption`。
4. 汇总更新 `month_department_energy_consumption`。
5. 汇总更新 `year_energy_consumption`。
6. 汇总更新 `year_department_energy_consumption`。

建议使用 `INSERT ... ON DUPLICATE KEY UPDATE`，不要通过重复插入新行来修改同一周期的数据。

示例：

```sql
INSERT INTO day_energy_consumption
(`year`, `month`, `day`, `type`, actual_value, average_value, unit)
VALUES (2026, 7, 31, '水', 128.50, 120.00, 'm³')
ON DUPLICATE KEY UPDATE
actual_value = VALUES(actual_value),
average_value = VALUES(average_value),
unit = VALUES(unit),
last_modify_time = NOW();
```

## 五、数据校验规则

- `year`、`month`、`day` 必须能转换为整数。
- `actual_value`、`average_value`、`value` 必须是数值，不能带单位文本。
- 数值缺失时使用 `NULL`，不要写 `-`、`无`、`暂无`。
- 同一条记录中的 `unit` 必须与 `type` 对应。
- 六个部门名称必须使用规定值。
- 总能耗和部门合计应尽量一致：

```text
day_energy_consumption.actual_value
≈ 当天六个部门 value 合计
```

- 禁止用多条重复记录表示修订版本，应更新原记录并维护 `last_modify_time`。
