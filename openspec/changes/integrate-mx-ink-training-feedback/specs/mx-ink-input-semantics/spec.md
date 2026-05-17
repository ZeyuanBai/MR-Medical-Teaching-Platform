## ADDED Requirements

### Requirement: 提供统一的 stylus 输入状态
系统 MUST 通过项目内 `StylusHandler` 实现暴露 MX Ink / stylus 输入，并发布统一的 `StylusInputs` 状态。

#### Scenario: stylus runtime active
- **WHEN** stylus runtime 检测到可用的 MX Ink / stylus 设备和有效输入
- **THEN** 系统 MUST 将 `StylusInputs.isActive` 设为 `true`
- **AND** 系统 MUST 持续从当前 stylus pose 更新 `StylusInputs.inkingPose`

#### Scenario: stylus runtime inactive
- **WHEN** stylus 设备不可用、不可读，或 runtime 判定其处于 inactive
- **THEN** 系统 MUST 将 `StylusInputs.isActive` 设为 `false`
- **AND** 系统 MUST 清空 tip、front、middle、back、double tap 和 aggregate 输入状态

### Requirement: 保持 MX Ink 输入语义独立
系统 MUST 将 MX Ink tip、front/grab、middle、back/option 保持为 `StylusInputs` 中相互独立的输入状态。

#### Scenario: 读取 tip pressure
- **WHEN** 用户按压 MX Ink 笔尖
- **THEN** 系统 MUST 将压力值写入 `StylusInputs.tip_value`
- **AND** 系统 MUST NOT 将同一输入写为 front、middle 或 back 按钮状态

#### Scenario: 读取 front/grab button
- **WHEN** 用户按下 MX Ink front/grab 按钮
- **THEN** 系统 MUST 将 `StylusInputs.cluster_front_value` 设为 pressed
- **AND** 系统 MUST NOT 因该输入修改 `tip_value`、`cluster_middle_value` 或 `cluster_back_value`

#### Scenario: 读取 middle pressure
- **WHEN** 用户按压 MX Ink middle 按钮
- **THEN** 系统 MUST 将压力值写入 `StylusInputs.cluster_middle_value`
- **AND** 系统 MUST NOT 将 middle 输入折叠为 front/grab 输入

#### Scenario: 读取 back/option button
- **WHEN** 用户按下 MX Ink back/option 按钮
- **THEN** 系统 MUST 将 `StylusInputs.cluster_back_value` 设为 pressed
- **AND** 系统 MUST NOT 将 back/option 输入折叠为 front/grab 输入

### Requirement: 将 stylus 状态映射到 XRI controller actions
系统 MUST 允许桥接组件将 `StylusInputs` 映射为 XRI controller 交互状态，并支持为 `select`、`activate`、`uiPress` 配置不同输入源。

#### Scenario: 映射布尔输入
- **WHEN** bridge 将 front、back 或 back double tap 映射为某个 controller action
- **THEN** 系统 MUST 在对应输入 pressed 时将该 action 标记为 active
- **AND** 系统 MUST 输出该 action value 为 `1.0`

#### Scenario: 映射模拟量输入
- **WHEN** bridge 将 tip 或 middle 映射为某个 controller action
- **THEN** 系统 MUST 使用对应模拟量作为 controller action value
- **AND** 系统 MUST 在 value 达到或超过配置阈值时将该 action 标记为 active

#### Scenario: 一个 action 配置多个 stylus 输入源
- **WHEN** 某个 controller action 同时配置多个 stylus 输入源
- **THEN** 系统 MUST 合并这些输入源得到最终 action 状态
- **AND** 系统 MUST 使用最大模拟量值和任一激活布尔输入计算最终结果

### Requirement: stylus control inactive 时恢复 controller 输入
系统 MUST 在 stylus control inactive、不可用或不再接管时恢复原始 `ActionBasedController` input 与 tracking 配置。

#### Scenario: stylus bridge 释放 controller control
- **WHEN** stylus control 从 active 转为 inactive
- **THEN** 系统 MUST 清空 bridge 写入的 controller interaction state
- **AND** 系统 MUST 恢复原始 `enableInputActions` 和 `enableInputTracking` 设置

### Requirement: 保持供应商资源不可变
系统 MUST 通过项目内脚本和场景引用实现 stylus 输入语义与 bridge 行为。

#### Scenario: 扩展 stylus 输入或 bridge 行为
- **WHEN** 需要调整 stylus input reading、state output 或 XRI action mapping
- **THEN** 系统 MUST 在项目内脚本或组件配置中实现该调整
- **AND** 系统 MUST NOT 直接修改 Logitech 提供的脚本、prefab 或 input action assets
