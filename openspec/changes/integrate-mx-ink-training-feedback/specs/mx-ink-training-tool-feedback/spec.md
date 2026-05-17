## ADDED Requirements

### Requirement: 接管 Step 1-3 训练工具代理
系统 MUST 在 stylus input active 时允许 MX Ink / stylus 作为当前 Step 1-3 训练工具的物理代理。

#### Scenario: Step 1 驱动 Marker
- **WHEN** 当前训练步骤为 Step 1 position determination
- **AND** stylus input active
- **THEN** 系统 MUST 使用 stylus pose 驱动 Marker 或 MarkerTip 训练代理
- **AND** `PositionDetermination` MUST 继续通过 MarkerTip collider 采样定位路径

#### Scenario: Step 2 驱动 Scalpel
- **WHEN** 当前训练步骤为 Step 2 skin and tissue cutting
- **AND** stylus input active
- **THEN** 系统 MUST 使用 stylus pose 驱动 Scalpel 或 ScalpelTip 训练代理
- **AND** `CutSkin` MUST 继续通过 ScalpelTip collider 采样切开路径

#### Scenario: Step 3 驱动 Scalpel
- **WHEN** 当前训练步骤为 Step 3 airway cutting
- **AND** stylus input active
- **THEN** 系统 MUST 使用 stylus pose 驱动 Scalpel 或 ScalpelTip 训练代理
- **AND** `CutAirway` MUST 继续通过 ScalpelTip collider 采样切开路径

### Requirement: 非目标步骤不接管工具
系统 MUST 仅在 Step 1-3 且 stylus input active 时接管 Marker / Scalpel 位姿，并在其他步骤或 stylus inactive 时释放接管。

#### Scenario: Step 4 不接管
- **WHEN** 当前训练步骤为 Step 4 intubation
- **THEN** 系统 MUST NOT 使用 stylus training bridge 接管 Tracheal 或 TrachealTip
- **AND** Step 4 MUST 继续使用现有插管流程

#### Scenario: stylus 变为 inactive
- **WHEN** stylus input 从 active 转为 inactive
- **THEN** 系统 MUST 停止用 stylus pose 驱动 Marker 或 Scalpel
- **AND** 现有 UI 与 controller 训练路径 MUST 保持可用

### Requirement: 支持 stylus command 作为增强流程输入
系统 MUST 支持将 stylus 按钮映射为 Step 1-3 的 confirm、retry 或 cancel 等流程命令，同时保留现有 UI 按钮作为回退路径。

#### Scenario: Step 1 confirm command
- **WHEN** 当前训练步骤为 Step 1
- **AND** 用户触发配置为 confirm 的 stylus 输入
- **THEN** 系统 MUST 能够确认当前定位路径
- **AND** 该结果 MUST 等价于现有 Step 1 UI confirmation path

#### Scenario: Step 2-3 completion command
- **WHEN** 当前训练步骤为 Step 2 或 Step 3
- **AND** 用户触发配置为 confirm 的 stylus 输入
- **THEN** 系统 MUST 能够将当前切开步骤标记为完成
- **AND** 该结果 MUST 等价于对应 UI completion button

#### Scenario: UI fallback remains available
- **WHEN** stylus input 不可用或 stylus button commands 未配置
- **THEN** 用户 MUST 仍可通过现有 UI buttons 完成 Step 1-3 的确认、重试和流程推进

### Requirement: 提供可降级训练反馈
系统 MUST 为 stylus 驱动的训练交互提供可降级反馈，包括 valid contact、successful confirmation 和 invalid operation。

#### Scenario: valid contact feedback
- **WHEN** stylus 驱动的 tool tip 进入当前步骤的有效训练区域
- **THEN** 系统 MUST 触发轻量反馈
- **AND** 如果 haptic 不可用，系统 MUST 使用视觉状态或日志作为 fallback feedback

#### Scenario: successful confirmation feedback
- **WHEN** 用户通过 stylus input 成功确认 Step 1、Step 2 或 Step 3
- **THEN** 系统 MUST 触发可区分的成功反馈
- **AND** feedback failure MUST NOT 阻塞训练步骤状态更新

#### Scenario: invalid operation feedback
- **WHEN** 用户在当前步骤有效区域之外操作 stylus-driven tool
- **THEN** 系统 MUST NOT 将该操作记录为当前步骤的有效证据
- **AND** 系统 SHOULD 触发可区分的 invalid-operation feedback

### Requirement: 保持现有测量和评分
系统 MUST 复用现有 Step 1-3 path sampling、measurement 和 scoring 逻辑。stylus training bridge MUST NOT 直接计算或覆盖训练评价指标。

#### Scenario: 复用定位测量
- **WHEN** Step 1 通过 stylus-driven tool proxy 完成
- **THEN** 系统 MUST 仍由 `PositionDetermination` 计算定位方向、长度和位置有效性

#### Scenario: 复用切开测量
- **WHEN** Step 2 或 Step 3 通过 stylus-driven tool proxy 完成
- **THEN** 系统 MUST 仍由 `CutSkin` 或 `CutAirway` 计算切口方向、长度和位置有效性

#### Scenario: report scoring remains compatible
- **WHEN** 使用 stylus input 完成 Step 1-3 后生成训练报告
- **THEN** 报告系统 MUST 使用现有 training evaluation data structures 和 scoring rules

### Requirement: 训练接入保留在项目内脚本
系统 MUST 通过项目内脚本和场景配置实现 stylus training integration。

#### Scenario: 接入 stylus 到 Step 1-3
- **WHEN** stylus input 需要驱动 Step 1-3 tool proxies、commands 或 feedback
- **THEN** 系统 MUST 使用项目内 bridge scripts 和现有 training scripts
- **AND** 系统 MUST NOT 修改 Logitech 提供的脚本、prefab 或 input action assets

### Requirement: 优先使用预配置场景组件
系统 MUST 优先使用预配置的场景组件完成 training integration，而不是在初始化阶段动态创建可提前配置的组件。

#### Scenario: bridge can be configured in the scene
- **WHEN** training bridge component 可以在场景中提前挂载和配置
- **THEN** 系统 MUST 使用该预配置组件
- **AND** 系统 MUST NOT 在初始化时动态创建并挂载同一组件
