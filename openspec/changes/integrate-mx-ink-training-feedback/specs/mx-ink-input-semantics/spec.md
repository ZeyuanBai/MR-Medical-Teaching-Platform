## ADDED Requirements

### Requirement: 保留当前 profile 接入检测
系统 MUST 继续使用当前基于 Meta 内部注册 MX Ink 兼容 profile 的接入检测路径判断触控笔是否 active，并且 MUST NOT 要求启用 Logitech 官方 OpenXR MX Ink profile 作为唯一检测前提。

#### Scenario: 通过当前内部 profile 识别触控笔
- **WHEN** 右手交互 profile 表明当前接入设备为 Meta 内部注册的 MX Ink 兼容 profile
- **THEN** 系统 MUST 将 MX Ink 运行时状态标记为 active
- **THEN** 系统 MUST 保持触控笔模型与右手控制器模型的现有自动切换行为

#### Scenario: 不依赖官方 profile 完成接入判断
- **WHEN** Logitech 官方 OpenXR MX Ink profile 未作为当前右手 profile 暴露
- **THEN** 系统 MUST 仍能基于当前内部 profile 判断 MX Ink 是否接入
- **THEN** 系统 MUST NOT 因未匹配官方 profile 而禁用触控笔运行时

### Requirement: 恢复 MX Ink 独立输入语义
系统 MUST 以官方 `MX_Ink.inputactions` 的语义为目标，将 MX Ink 的 tip、front/grab、middle、back/option 恢复为相互独立的输入状态，并输出到统一 `StylusInputs`。

#### Scenario: 读取笔尖压力
- **WHEN** 用户按压 MX Ink 笔尖
- **THEN** 系统 MUST 将压力值写入 `StylusInputs.tip_value`
- **THEN** 系统 MUST NOT 将该输入同时误写为 front、middle 或 back 按键状态

#### Scenario: 读取 front/grab 按键
- **WHEN** 用户按下 MX Ink front/grab 按键
- **THEN** 系统 MUST 将 `StylusInputs.cluster_front_value` 设置为 pressed
- **THEN** 系统 MUST NOT 因该按键触发而修改 `tip_value`、`cluster_middle_value` 或 `cluster_back_value`

#### Scenario: 读取 middle 按键压力
- **WHEN** 用户按压 MX Ink middle 按键
- **THEN** 系统 MUST 将压力值写入 `StylusInputs.cluster_middle_value`
- **THEN** 系统 MUST NOT 将该输入折叠为 front/grab 按键

#### Scenario: 读取 back/option 按键
- **WHEN** 用户按下 MX Ink back/option 按键
- **THEN** 系统 MUST 将 `StylusInputs.cluster_back_value` 设置为 pressed
- **THEN** 系统 MUST NOT 将该输入折叠为 front/grab 按键

### Requirement: 统一触控笔状态输出
系统 MUST 在 MX Ink active 时持续更新 `StylusInputs` 的输入状态、位姿状态和活动状态；在 MX Ink inactive 时 MUST 清空按钮状态并保留安全的 inactive 状态。

#### Scenario: active 时输出完整状态
- **WHEN** MX Ink 被识别为 active
- **THEN** 系统 MUST 将 `StylusInputs.isActive` 设置为 true
- **THEN** 系统 MUST 更新 `StylusInputs.inkingPose` 以反映当前触控笔位姿
- **THEN** 系统 MUST 更新 tip、front、middle、back 的当前输入值

#### Scenario: inactive 时清空输入
- **WHEN** MX Ink 未接入或当前 profile 不再匹配触控笔
- **THEN** 系统 MUST 将 `StylusInputs.isActive` 设置为 false
- **THEN** 系统 MUST 清空 tip、front、middle、back 和 double tap 状态
- **THEN** 系统 MUST NOT 继续向训练桥接层报告有效触控笔操作

### Requirement: 输入读取失败时降级
系统 MUST 在官方 action binding 无法直接读取当前内部 profile 的独立输入值时提供可诊断的 fallback 路径，并且 MUST 保持最终 `StylusInputs` 语义一致。

#### Scenario: 官方 action 未返回有效值
- **WHEN** MX Ink active 但官方 action binding 对某个控制项未返回有效值
- **THEN** 系统 MUST 尝试使用配置的 fallback 输入路径读取该控制项
- **THEN** 系统 MUST 保持输出字段仍对应官方语义名称

#### Scenario: fallback 也不可用
- **WHEN** 官方 action 与 fallback 路径都无法读取某个控制项
- **THEN** 系统 MUST 将该控制项输出为未按下或零压力
- **THEN** 系统 MUST 记录可诊断信息以帮助确认当前 profile 的实际控制路径
- **THEN** 系统 MUST NOT 阻塞 MX Ink 接入检测或模型切换

### Requirement: 仅通过项目内脚本扩展
系统 MUST 仅通过项目内新建或现有自有脚本实现 MX Ink 输入语义恢复，不得修改 Logitech 提供的脚本、预制体或其他资源。

#### Scenario: 语义扩展不改 Logitech 资产
- **WHEN** 需要调整 MX Ink 输入读取或状态输出行为
- **THEN** 系统 MUST 使用项目内脚本或组件引用完成扩展
- **THEN** 系统 MUST NOT 直接修改 Logitech 提供的脚本、prefab 或输入资源文件

### Requirement: 场景接入通过 MCP 完成
系统 MUST 通过 MCP 完成与场景相关的对象绑定和组件接入，不得直接编辑场景文件。

#### Scenario: 场景引用配置
- **WHEN** 需要把输入恢复组件接入当前训练场景
- **THEN** 系统 MUST 通过 MCP 配置场景对象和组件引用
- **THEN** 系统 MUST NOT 直接修改场景序列化文件
