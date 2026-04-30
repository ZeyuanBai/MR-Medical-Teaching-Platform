# 气管切开训练位置偏差扣分曲线

版本：0.2-review  
状态：待临床和产品审核  
范围：气管切开训练报告中“位置偏差”和短期可实现评分标准的建议

## 目的

本文档用于定义气管切开训练四个步骤的位置偏差扣分曲线，供后续软件实现、其他 AI 处理或临床评审使用。

本版相对 `0.1-draft` 的主要调整：

- 将“单一径向偏差”升级为“关键门槛 + 连续扣分曲线”。
- 对气道切开步骤引入左右偏差和头尾方向偏差，头侧、足侧采用非对称扣分。
- 收紧原先过宽的 `hard_cm` 和 `fail_cm`，避免明显偏离目标区域仍然得高分。
- 补充短期内可实现的评分标准，并按优先级排序。

## 外部依据摘要

公开评分表和教材/指南通常不直接给出“偏差多少厘米扣几分”的曲线，而是强调关键解剖位置和关键动作：

- 住培技能评分表将“切口位置的选择”“切开第 2-3 或 3-4 气管环”“插入气管套管，确认套管置于气管内”作为明确评分项。
- 常规开放气管切开术目标多在第 2-3 气管环之间，部分考核也接受第 3-4 气管环；应避免第 1 气管环和过低切开。
- 成人气管横径通常约 1.5-2.0 cm，单个气管环高度约 0.45 cm，环间距约 0.14 cm。纵向错过一个环间隙往往已接近 0.6 cm 量级，因此气道切开位置不宜采用 2 cm 以上仍有较高得分的宽松曲线。

参考来源：

- 广东省住培临床实践能力结业考核方案，耳鼻喉科气管切开术评分表。
  <https://gd.wsglw.net/UpFile/adjunctfile/2024/4/06d0caf7ebe64ec4b89f971c9b0bf53e.pdf>
- StatPearls / NCBI Bookshelf: Tracheostomy.
  <https://www.ncbi.nlm.nih.gov/books/NBK559124/>
- 成人气管形态学研究，含气管横径、前后径、气管环高度等数据。
  <https://link.springer.com/article/10.1186/s13104-018-3123-1>

## 总体评分原则

位置评分不应单独决定全部成绩。建议按以下顺序处理：

1. 先判定关键门槛：是否完成步骤、是否跳步或逆序、是否在允许气管环范围、套管是否确认位于气管内。
2. 门槛通过后，再用连续曲线处理小范围位置偏差。
3. 完成时间、分步耗时、重试次数优先作为观察指标和报告建议，不进入主扣分。
4. 气道切开比皮肤切口和表面标记更接近关键结构，应更严格。
5. 头尾方向偏差应非对称：向头侧靠近环状软骨和第 1 气管环，应更快重扣；向足侧落入第 3-4 气管环可部分接受，但继续向足侧应快速归零。

## 基础径向曲线

对 `step1`、`step2` 和 `step4`，短期可继续使用径向偏差 `d_cm`。输入参数：

- `d_cm`：实测位置偏差，单位为厘米。
- `free_cm`：免扣分半径，偏差不超过该值时给满分。
- `soft_cm`：轻度扣分区间终点。
- `hard_cm`：加速扣分区间终点。
- `fail_cm`：归零偏差点，偏差达到或超过该值时位置分为 0。
- `soft_penalty`：到达 `soft_cm` 时累计扣除的分数比例。
- `hard_score`：到达 `hard_cm` 时保留的位置分。

输出参数：

- `position_score`：位置指标标准化得分，范围为 `0.0` 到 `1.0`。

分段公式：

```text
if d_cm <= free_cm:
    position_score = 1.0

else if d_cm <= soft_cm:
    t = (d_cm - free_cm) / (soft_cm - free_cm)
    position_score = 1.0 - soft_penalty * t^2

else if d_cm <= hard_cm:
    t = (d_cm - soft_cm) / (hard_cm - soft_cm)
    soft_score = 1.0 - soft_penalty
    position_score = soft_score - (soft_score - hard_score) * t^2.5

else if d_cm <= fail_cm:
    t = (d_cm - hard_cm) / (fail_cm - hard_cm)
    position_score = hard_score * (1.0 - t^2)

else:
    position_score = 0.0
```

最终输出需要截断到合法范围：

```text
position_score = clamp(position_score, 0.0, 1.0)
```

## 气道切开非对称曲线

`step3` 不建议只用一个径向 `d_cm`。建议拆成两个局部坐标轴：

- `lateral_offset_cm`：相对气管中线的左右偏差，取绝对值，左右对称扣分。
- `cranio_caudal_offset_cm`：相对目标环间隙的头尾方向偏差，足侧为正，头侧为负。

组合规则：

```text
lateral_score = radial_curve(abs(lateral_offset_cm), lateral_params)

if cranio_caudal_offset_cm < 0:
    axial_score = radial_curve(abs(cranio_caudal_offset_cm), cranial_params)
else:
    axial_score = radial_curve(cranio_caudal_offset_cm, caudal_params)

position_score = min(lateral_score, axial_score)
```

使用 `min` 的原因是左右偏离和头尾偏离任一项明显错误都可能导致目标环间隙不正确，不宜被另一项优秀表现抵消。

## 推荐位置参数

| 步骤 ID | 步骤名称 | 位置偏差指标 | 免扣分 cm | 轻扣终点 cm | 加速扣分终点 cm | 归零点 cm | 轻扣累计比例 | 加速区终点得分 | 说明 |
|---|---|---|---:|---:|---:|---:|---:|---:|---|
| `step1` | 确定切割位置 / 标记 | 标记中点径向偏移 | 0.7 | 1.4 | 2.4 | 3.2 | 0.12 | 0.35 | 表面标记可最宽容，但偏离 3 cm 以上通常已难以视为同一目标区域。 |
| `step2` | 切开皮肤和组织 | 切口中心径向偏移 | 0.6 | 1.2 | 2.0 | 2.8 | 0.15 | 0.32 | 皮肤切口保留横切、竖切和个体习惯差异，但中心不应明显偏离颈前中线目标区。 |
| `step3_lateral` | 切开气道 | 气道切口左右偏移 | 0.25 | 0.5 | 0.9 | 1.2 | 0.20 | 0.25 | 左右偏差严格控制；约 1 cm 已接近成人气管半径量级。 |
| `step3_cranial` | 切开气道 | 气道切口向头侧偏移 | 0.25 | 0.45 | 0.75 | 1.0 | 0.25 | 0.20 | 头侧偏移更危险，应避免靠近第 1 气管环或环状软骨。 |
| `step3_caudal` | 切开气道 | 气道切口向足侧偏移 | 0.35 | 0.8 | 1.2 | 1.8 | 0.18 | 0.30 | 第 3-4 气管环可部分接受，但继续向足侧应快速重扣。 |
| `step4` | 放置气管套管 | 套管放置径向偏移或插入位置代理指标 | 0.5 | 1.0 | 1.8 | 2.5 | 0.12 | 0.45 | 必须先通过“确认套管位于气管内”门槛，位置只作为辅助评分。 |

## 关键门槛规则

以下规则建议优先实现为门槛，而不是普通扣分项：

| 门槛 ID | 适用步骤 | 判定 | 建议结果 |
|---|---|---|---|
| `required_step_completed` | 全部步骤 | 关键步骤未完成 | 该步骤得分为 0，并在总评中标记未达标。 |
| `sequence_valid` | 全流程 | 跳步、逆序或强制结束 | 总评标记未达标；报告展示具体异常步骤。 |
| `tracheal_ring_window_valid` | `step3` | 气道切开不在第 2-3 或第 3-4 气管环范围 | `step3` 位置分为 0，且标记关键错误。 |
| `tube_in_trachea_confirmed` | `step4` | 套管未确认位于气管内 | `step4` 得分为 0，且训练结果不应判定通过。 |
| `dangerous_contact_exceeded` | 全流程 | 危险接触或危险区域进入超过阈值 | 总评标记未达标；短期没有碰撞数据时可暂缓。 |

## 短期可实现评分标准优先级

### P0：应优先实现

这些标准与当前训练脚本和报告结构最贴近，短期收益高：

| 标准 | 指标 | 建议阈值或规则 | 说明 |
|---|---|---|---|
| 关键步骤完成 | step completion event | 未完成则该步骤 0 分 | 已有步骤生命周期采集基础。 |
| 流程顺序 | step order | 跳步、逆序、强制结束标记未达标 | 可直接从 session event 序列判断。 |
| 方向允许横/竖 | angle error | `step1`、`step2` 允许横向或竖向中误差较小者 | 与现有方向判定逻辑一致。 |
| 气道切开目标环间隙 | local axial position | 第 2-3 或第 3-4 气管环为有效窗口 | 若场景已有目标区域，可先用本地坐标代理实现。 |
| 套管在气管内确认 | success state | 未确认则 `step4` 为 0 | 比位置偏差更关键。 |
| 稳定停留 | stable hold duration | 默认 1.5 s | 已在配置中有对应字段，适合作为 step4 主指标。 |

### P1：短期可补充

这些标准已有部分指标或较容易从采样轨迹计算：

| 标准 | 指标 | 建议阈值或规则 | 说明 |
|---|---|---|---|
| 标记长度 | marker length | 默认 1.5-3.0 cm | 超短或超长都提示定位不清。 |
| 皮肤切口长度 | incision length | 默认 1.8-4.0 cm | 与现有 `Step23Rubric` 接近。 |
| 气道切口长度 | airway incision length | 默认 1.0-2.5 cm | 不应过长，避免扩大损伤。 |
| 路径效率 | path efficiency | step2 默认 0.75，step3 默认 0.8 | 反映切开轨迹是否绕行或反复修正。 |
| 插管角度 | tube angle error | 默认不超过 15 度 | 作为 step4 辅助分。 |
| 手势防抖 | stable window | 默认 6 帧，采样 20-50 Hz | 过滤短时误触和瞬时抖动。 |

### P2：先展示，暂不扣分

这些指标对教学反馈有价值，但不建议短期进入主评分：

| 标准 | 指标 | 建议处理 | 说明 |
|---|---|---|---|
| 总完成时间 | total duration | 报告展示，不扣分 | 熟练度指标，容易误伤初学者。 |
| 分步耗时 | step duration | 报告展示，不扣分 | 用于趋势分析。 |
| 重试次数 | retry count | 报告展示，必要时提示 | 先作为观察指标。 |
| 重复训练趋势 | score trend / metric trend | 报告展示 | 适合教学复盘。 |

## 机器可读参数

说明：

- JSON 字段名保持英文，便于程序直接解析。
- `name` 和 `offsetMetricLabel` 使用中文，便于报告或配置界面直接展示。
- `step3` 使用轴向参数，不再只用一个 `airway_incision_center_offset_cm`。

```json
{
  "version": "0.2-review",
  "unit": "cm",
  "curve": "gated_piecewise_radial_with_asymmetric_airway_axis",
  "description": "气管切开训练位置偏差扣分曲线审核版",
  "steps": {
    "step1": {
      "name": "确定切割位置 / 标记",
      "offsetMetric": "marker_midpoint_offset_cm",
      "offsetMetricLabel": "标记中点径向偏移",
      "freeCm": 0.7,
      "softCm": 1.4,
      "hardCm": 2.4,
      "failCm": 3.2,
      "softPenalty": 0.12,
      "hardScore": 0.35
    },
    "step2": {
      "name": "切开皮肤和组织",
      "offsetMetric": "incision_center_offset_cm",
      "offsetMetricLabel": "切口中心径向偏移",
      "freeCm": 0.6,
      "softCm": 1.2,
      "hardCm": 2.0,
      "failCm": 2.8,
      "softPenalty": 0.15,
      "hardScore": 0.32
    },
    "step3": {
      "name": "切开气道",
      "offsetMetric": "airway_incision_axis_offsets_cm",
      "offsetMetricLabel": "气道切口局部轴向偏移",
      "combine": "min(lateralScore, cranioCaudalScore)",
      "signedAxisConvention": "cranioCaudalOffsetCm > 0 means caudal; < 0 means cranial",
      "lateral": {
        "freeCm": 0.25,
        "softCm": 0.5,
        "hardCm": 0.9,
        "failCm": 1.2,
        "softPenalty": 0.20,
        "hardScore": 0.25
      },
      "cranial": {
        "freeCm": 0.25,
        "softCm": 0.45,
        "hardCm": 0.75,
        "failCm": 1.0,
        "softPenalty": 0.25,
        "hardScore": 0.20
      },
      "caudal": {
        "freeCm": 0.35,
        "softCm": 0.8,
        "hardCm": 1.2,
        "failCm": 1.8,
        "softPenalty": 0.18,
        "hardScore": 0.30
      }
    },
    "step4": {
      "name": "放置气管套管",
      "offsetMetric": "tube_placement_offset_cm",
      "offsetMetricLabel": "套管放置径向偏移",
      "requiredGate": "tube_in_trachea_confirmed",
      "freeCm": 0.5,
      "softCm": 1.0,
      "hardCm": 1.8,
      "failCm": 2.5,
      "softPenalty": 0.12,
      "hardScore": 0.45
    }
  },
  "gates": {
    "requiredStepCompleted": true,
    "sequenceValid": true,
    "trachealRingWindowValid": {
      "step": "step3",
      "acceptedWindows": ["2-3", "3-4"]
    },
    "tubeInTracheaConfirmed": {
      "step": "step4",
      "requiredForPass": true
    }
  }
}
```

## 方向判定规则

步骤一和步骤二的方向评分应允许横向和竖向两种标记 / 切口方向。

推荐规则：

```text
horizontal_error = observed_direction 与 horizontal_reference 的夹角
vertical_error = observed_direction 与 vertical_reference 的夹角
angle_error = min(horizontal_error, vertical_error)
```

也就是说，步骤一应和步骤二一样，只要横向或竖向任一方向满足要求，就不应被判定为方向错误。

步骤三的方向应由教学方案决定：

- 如果当前场景教学目标是纵向切开，则使用纵向参考方向。
- 如果当前场景教学目标是横向窗口或瓣式切开，则使用横向参考方向。
- 无论采用哪种方向，均不得替代“切开位于第 2-3 或第 3-4 气管环范围”的门槛判定。

## 实现建议

- 本曲线只应用于位置偏差指标，不应用于完成时间、分步耗时或重试次数。
- `step3` 应优先使用局部坐标拆轴评分；如果短期只能实现径向偏差，可暂用 `min(step3_lateral, step3_cranial/caudal)` 的等效近似，不建议继续使用旧版 2.5 cm 归零点。
- `step4` 应以插管成功状态和稳定停留时长为主要指标，位置偏差曲线只作为辅助扣分。
- 上述参数仍是审核草案，应通过基准回放或等效验证用例校准。

## 建议验证用例

后续实现前，建议至少覆盖以下情况：

- 步骤一横向标记通过。
- 步骤一竖向标记通过。
- 步骤二横切通过。
- 步骤二竖切通过。
- 轻微位置偏移，只产生小幅扣分。
- 明显位置偏移，产生加速扣分。
- 气道切开位于第 2-3 气管环，通过。
- 气道切开位于第 3-4 气管环，允许但可产生轻微扣分。
- 气道切开偏向头侧接近第 1 气管环，快速重扣或归零。
- 气道切开明显偏离中线，快速重扣或归零。
- 插管成功但位置略有偏差，保留主要得分。
- 套管未确认位于气管内，步骤四归零且训练不通过。
- 关键步骤未完成。
