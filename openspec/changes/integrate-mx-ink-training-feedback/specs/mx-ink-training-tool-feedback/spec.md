## ADDED Requirements

### Requirement: Step 1-3 工具代理接管
系统 MUST 在 MX Ink active 且技能培训处于第 1-3 步时，允许 MX Ink 作为当前步骤工具的物理代理，驱动对应工具对象和尖端碰撞体的位姿。

#### Scenario: Step 1 驱动 Marker
- **WHEN** 当前训练步骤为 Step 1 定位
- **AND** MX Ink 处于 active 状态
- **THEN** 系统 MUST 使用 MX Ink 位姿驱动 Marker 或 MarkerTip 对应的训练代理
- **THEN** `PositionDetermination` MUST 能继续通过 MarkerTip collider 采样定位路径

#### Scenario: Step 2 驱动 Scalpel
- **WHEN** 当前训练步骤为 Step 2 切开皮肤和组织
- **AND** MX Ink 处于 active 状态
- **THEN** 系统 MUST 使用 MX Ink 位姿驱动 Scalpel 或 ScalpelTip 对应的训练代理
- **THEN** `CutSkin` MUST 能继续通过 ScalpelTip collider 采样切开路径

#### Scenario: Step 3 驱动 Scalpel
- **WHEN** 当前训练步骤为 Step 3 切开气管
- **AND** MX Ink 处于 active 状态
- **THEN** 系统 MUST 使用 MX Ink 位姿驱动 Scalpel 或 ScalpelTip 对应的训练代理
- **THEN** `CutAirway` MUST 能继续通过 ScalpelTip collider 采样切开路径

### Requirement: 非目标步骤不接管工具
系统 MUST 只在训练 Step 1-3 且 MX Ink active 时接管 Marker/Scalpel 位姿，并在其他步骤或触控笔 inactive 时释放接管。

#### Scenario: Step 4 不接管
- **WHEN** 当前训练步骤为 Step 4 插管
- **THEN** 系统 MUST NOT 使用 MX Ink 桥接层接管 Tracheal 或 TrachealTip
- **THEN** Step 4 MUST 保持现有插管流程

#### Scenario: 触控笔断开时释放接管
- **WHEN** MX Ink 从 active 变为 inactive
- **THEN** 系统 MUST 停止使用 MX Ink 位姿驱动 Marker 或 Scalpel
- **THEN** 系统 MUST 保留现有 UI 与手柄训练路径可用

### Requirement: 触控笔流程命令作为增强输入
系统 MUST 支持将 MX Ink 按钮映射为 Step 1-3 的确认、重试或取消等增强流程命令，但 MUST 保留现有 UI 按钮路径作为回退。

#### Scenario: Step 1 触控笔确认定位
- **WHEN** 当前训练步骤为 Step 1
- **AND** 用户触发配置为确认的 MX Ink 输入
- **THEN** 系统 MUST 能将当前定位路径标记为已确认
- **THEN** 该行为 MUST 等价于现有 Step 1 UI 确认路径对训练状态的影响

#### Scenario: Step 2-3 触控笔确认切开完成
- **WHEN** 当前训练步骤为 Step 2 或 Step 3
- **AND** 用户触发配置为确认的 MX Ink 输入
- **THEN** 系统 MUST 能将当前切开步骤标记为完成
- **THEN** 该行为 MUST 等价于对应 UI 完成按钮对训练状态的影响

#### Scenario: UI 回退仍可用
- **WHEN** MX Ink 未接入或触控笔按钮命令不可用
- **THEN** 用户 MUST 仍能通过现有 UI 按钮完成 Step 1-3 的确认、重试和流程推进

### Requirement: 触觉与视觉反馈
系统 MUST 为 MX Ink 训练代理提供可降级的反馈机制，用于提示有效接触、压力输入、步骤确认成功和无效操作。

#### Scenario: 有效接触反馈
- **WHEN** MX Ink 代理的工具尖端进入当前步骤的有效训练区域
- **THEN** 系统 MUST 触发轻量反馈
- **THEN** 如果 haptic 不可用，系统 MUST 使用视觉状态或日志提示作为降级反馈

#### Scenario: 确认成功反馈
- **WHEN** 用户通过 MX Ink 输入成功确认 Step 1、Step 2 或 Step 3
- **THEN** 系统 MUST 触发明确的成功反馈
- **THEN** 反馈失败 MUST NOT 阻止步骤状态更新

#### Scenario: 无效操作反馈
- **WHEN** 用户在非当前步骤区域操作 MX Ink 代理工具
- **THEN** 系统 MUST 不记录该操作为当前步骤有效证据
- **THEN** 系统 SHOULD 触发可区分的无效操作反馈

### Requirement: 不改变现有测量与评分
系统 MUST 复用现有 Step 1-3 的路径采样、测量和评分逻辑；MX Ink 桥接层 MUST NOT 直接计算或覆盖训练评价指标。

#### Scenario: 复用定位测量
- **WHEN** Step 1 使用 MX Ink 代理完成定位路径
- **THEN** 系统 MUST 仍由 `PositionDetermination` 计算定位方向、长度和位置有效性

#### Scenario: 复用切开测量
- **WHEN** Step 2 或 Step 3 使用 MX Ink 代理完成切开路径
- **THEN** 系统 MUST 仍由 `CutSkin` 或 `CutAirway` 计算切口方向、长度和位置有效性

#### Scenario: 报告评分保持兼容
- **WHEN** 使用 MX Ink 完成 Step 1-3 后生成训练报告
- **THEN** 报告系统 MUST 使用现有训练评价数据结构和评分规则生成结果

### Requirement: 仅通过项目内脚本实现训练接入
系统 MUST 仅通过项目内新建或现有自有脚本实现 MX Ink 训练桥接，不得修改 Logitech 提供的脚本、预制体或其他资源。

#### Scenario: 训练接入不改 Logitech 资源
- **WHEN** 需要把 MX Ink 接入 Step 1-3 的工具代理和反馈链路
- **THEN** 系统 MUST 使用项目内桥接脚本和现有训练脚本完成接入
- **THEN** 系统 MUST NOT 修改 Logitech 提供的脚本、prefab 或输入资源

### Requirement: 场景脚本优先预挂载
系统 MUST 优先使用已预挂载在场景中的脚本完成训练接入，不得在初始化阶段再动态挂载本可以提前配置的脚本。

#### Scenario: 预挂载脚本优先
- **WHEN** 训练桥接组件可以在场景中提前配置
- **THEN** 系统 MUST 直接使用预挂载组件完成工作
- **THEN** 系统 MUST NOT 在初始化时再动态创建并挂载该组件
