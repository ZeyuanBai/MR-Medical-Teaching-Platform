# 训练报告指标说明

## 1. 适用范围

本文档说明气管切开训练报告中各指标的计算公式、单位、阈值和判定规则。

## 2. 坐标系约定

- 步骤一、步骤二、步骤三的几何指标，统一在目标区域的局部坐标系下计算。
- 这样可以消除模型整体平移、台面高度、父物体旋转等因素对评分的干扰。
- 内部长度单位为米，报告展示为厘米时使用 `cm = m * 100` 转换。

## 3. 总评规则

- 总分公式：`overall_score = 所有步骤 normalizedScore 的平均值`
- 报告显示分：`display_score = round(overall_score * 100)`
- 通过门槛：`overall_score >= passScore`
- 但只要出现以下任一情况，总评仍判为未通过：
  - 任一步骤未完成
  - 任一关键阈值失败

## 4. 方向误差公式

- 垂直参考轴：

```text
vertical_axis = normalize(StandardB_local - StandardA_local)
```

- 水平参考轴：

```text
horizontal_axis = normalize(cross(local_up, vertical_axis))
```

- 操作方向：

```text
operation_axis = normalize(EndPoint_local - StartPoint_local)
```

- 垂直误差：

```text
vertical_error_deg = angle(operation_axis, vertical_axis)
```

- 水平误差：

```text
horizontal_error_deg = angle(operation_axis, horizontal_axis)
```

- `AllowedAxes` 判定规则：
  - `Vertical`：`angle_error_deg = vertical_error_deg`
  - `Horizontal`：`angle_error_deg = horizontal_error_deg`
  - `Both`：`angle_error_deg = min(vertical_error_deg, horizontal_error_deg)`

## 5. 中点/中心偏移公式

- 参考中心优先取标准线两端中点：

```text
reference_center = midpoint(StandardA_local, StandardB_local)
```

- 若标准线两端不可用，则退化为：

```text
reference_center = StandardLine_local_position
```

- 操作中心：

```text
operation_center = midpoint(StartPoint_local, EndPoint_local)
```

- 偏移：

```text
offset_m = distance(reference_center, operation_center)
offset_cm = offset_m * 100
```

## 6. 长度公式

```text
length_m = distance(StartPoint_local, EndPoint_local)
length_cm = length_m * 100
```

## 7. 路径效率公式

- 理想长度：

```text
ideal_length_m = (length_min_m + length_max_m) / 2
```

- 路径效率：

```text
path_efficiency = min(actual_length_m, ideal_length_m) / max(actual_length_m, ideal_length_m)
```

- 取值范围：`0 ~ 1`
- 含义：
  - 越接近 `1`，说明实际路径长度越接近理想长度
  - 越小，说明切口明显过短或过长

## 8. 各步骤指标与判定标准

### 步骤一：确定切割位置

- `s1_angle_error_deg`
  - 含义：定位方向误差
  - 判定：越小越好
  - 阈值：`<= 10 deg`
- `s1_midpoint_offset_cm`
  - 含义：定位中点偏移
  - 判定：越小越好
  - 阈值：`<= 1.0 cm`
- `s1_mark_length_cm`
  - 含义：标记长度
  - 判定：区间内最好
  - 阈值范围：`1.5 cm ~ 3.0 cm`
- `s1_duration_seconds`
  - 仅观察，不参与主评分

### 步骤二：切开皮肤和组织

- `s2_angle_error_deg`
  - 含义：切口方向误差
  - 判定：越小越好
  - 阈值：`<= 12 deg`
- `s2_incision_length_cm`
  - 含义：切口长度
  - 判定：区间内最好
  - 阈值范围：`1.8 cm ~ 4.0 cm`
- `s2_center_offset_cm`
  - 含义：切口中心偏移
  - 判定：越小越好
  - 阈值：`<= 0.8 cm`
- `s2_path_efficiency`
  - 含义：路径效率
  - 判定：越大越好
  - 阈值：`>= 0.75`

### 步骤三：切开气管

- `s3_angle_error_deg`
  - 含义：气管切口方向误差
  - 判定：越小越好
  - 阈值：`<= 8 deg`
- `s3_incision_length_cm`
  - 含义：气管切口长度
  - 判定：区间内最好
  - 阈值范围：`1.0 cm ~ 2.5 cm`
- `s3_center_offset_cm`
  - 含义：气管切口中心偏移
  - 判定：越小越好
  - 阈值：`<= 0.6 cm`
- `s3_path_efficiency`
  - 含义：路径效率
  - 判定：越大越好
  - 阈值：`>= 0.8`

### 步骤四：插入气管套管

- `s4_success_state`
  - 公式：插入位置有效记为 `1`，否则记为 `0`
  - 阈值：要求成功时必须等于 `1`
- `s4_stable_hold_seconds`
  - 公式：有效插入后连续稳定停留的时间
  - 阈值：`>= 1.5 s`
- `s4_angle_error_deg`
  - 当前实现说明：
    - 插管成功时记为 `0 deg`
    - 插管失败时使用阈值角作为失败占位值
  - 阈值：`<= 15 deg`
  - 备注：这一项当前仍是代理值，不是基于真实插管向量的几何角度

## 9. 指标标准化规则

### 越小越好

```text
normalized = 1 - clamp(raw / max_reference)
```

### 越大越好

```text
normalized = clamp(raw / target_reference)
```

### 区间内最好

- 落在阈值区间内时记为 `1`
- 超出区间时，按偏离目标中心的距离进行衰减

## 10. 阈值判定规则

- 一个阈值通过，必须同时满足：
  - 最小值条件
  - 最大值条件
  - 方向轴匹配条件
- 任何关键阈值失败，步骤直接判为 `Failed`
- 时间、重试次数等观察指标会显示和导出，但不参与主几何评分

## 11. 2026-04-19 异常值说明

- `167.63 cm` 这种步骤二中心偏移，对当前训练场景来说明显不合理。
- 根因是旧逻辑混用了不同参照系：
  - 切口点部分使用了压平后的坐标
  - 参考点却直接使用世界坐标
  - 模型离地高度被误算进了偏移量
- 当前修复后，步骤一到三统一在区域局部坐标系下计算，不再把这类整体位姿差异记成中心偏移。
## 12. 2026-04-26 Scale-independent measurement
- Step 1-3 geometry now uses an unscaled local measurement frame: `local = inverse(rotation) * (world - origin)`.
- This avoids using `InverseTransformPoint` as the distance basis, because that API includes parent scale and can make centimeter metrics drift when `SkillTrainingModelTable` or visual meshes are scaled.
- Incision representative endpoints, path length, center offset, angle error, and report recomputation now share the same measurement convention.
- Scene cleanup recommendation: keep `SkillTrainingModelTable` at `{1,1,1}`, keep colliders and step regions under the measurement root, and move only visual meshes into a child object when visual scaling is needed.
