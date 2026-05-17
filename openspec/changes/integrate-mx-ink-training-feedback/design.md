## Context

气管切开技能训练的 Step 1-3 已围绕实体工具 tip collider 建立采样和评估：

- Step 1 使用 `MarkerTip` 采样定位路径。
- Step 2 使用 `ScalpelTip` 采样皮肤和组织切开路径。
- Step 3 使用 `ScalpelTip` 采样气管切开路径。

MX Ink 接入应复用这些训练工具和测量链路。触控笔输入层负责提供 pose、按钮、压力和 active 状态；训练桥接层负责把这些状态转换为工具代理位姿、流程命令和反馈。

## Goals / Non-Goals

**Goals:**

- 使用 `StylusHandler` 作为本 change 的统一 stylus 输入抽象。
- 保持 tip、front/grab、middle、back/option 的独立语义，并输出到 `StylusInputs`。
- 通过 XRI bridge 将 stylus 输入映射到 `ActionBasedController` 的交互状态。
- 在 Step 1-3 中使用 stylus 驱动 Marker / Scalpel 工具代理及其 tip collider。
- 支持 stylus 按钮触发 Step 1-3 的确认、重试或取消等增强命令。
- 为有效接触、确认成功和无效操作提供 haptic、视觉或日志反馈。
- 保留现有 UI 与手柄路径，使 stylus 不可用时仍可完成训练。

**Non-Goals:**

- 不要求通过 Meta input 或 Meta 内部 profile 完成设备识别。
- 不重写 `PositionDetermination`、`CutSkin`、`CutAirway` 的路径测量算法。
- 不改变训练评价报告的核心评分规则和结果口径。
- 不在本 change 中为 Step 4 插管流程接入 MX Ink 工具代理。
- 不修改 Logitech 提供的脚本、prefab 或 input actions 资源。
- 不直接手动编辑场景文件；场景对象、引用和组件接入通过 MCP 完成。

## Decisions

### 1. 使用 `StylusHandler` 作为输入抽象

上层训练和 XRI 逻辑只依赖 `StylusHandler.CurrentState` 暴露的 `StylusInputs`。具体 runtime 可以由项目内组件实现，只要稳定输出 active、inking pose、tip pressure、front、middle、back 和 double tap 等状态。

理由：训练流程只需要统一的触控笔状态，不应绑定某个底层设备 profile 或供应商运行时实现。

### 2. 保留 MX Ink 独立输入语义

tip、front/grab、middle、back/option 继续作为独立输入源处理。模拟量输入使用阈值生成 active 状态，布尔输入直接生成 pressed 状态。多个输入源映射到同一 action 时，最终值采用最大值或任一激活的合并方式。

理由：独立语义可以同时支持训练流程命令、XRI action 映射和诊断，不会把不同按钮折叠成不可区分的单一触发。

### 3. XRI 映射由独立 bridge 负责

XRI bridge 读取 `StylusInputs`，并把 stylus 输入映射到 `ActionBasedController` 的 pose、select、activate 和 uiPress 等状态。stylus inactive 或 bridge 不接管时，controller input 与 tracking 设置恢复为原始配置。

理由：XRI 交互状态和训练工具接管属于不同层，保持 bridge 独立可以避免训练脚本直接操作 controller internals。

### 4. 训练接入由训练桥接组件负责

训练桥接组件根据 `SkillTrainingManager.CurrentStep` 选择当前工具代理：

- Step 1：stylus pose 驱动 Marker / MarkerTip。
- Step 2：stylus pose 驱动 Scalpel / ScalpelTip。
- Step 3：stylus pose 驱动 Scalpel / ScalpelTip。

该组件只负责工具位姿、接管状态、流程命令映射和反馈触发，不直接计算训练评分指标。

理由：现有训练步骤已经通过 tip collider 完成采样。让 stylus 驱动同一套 tip 对象，可以最大化复用现有测量逻辑。

### 5. 反馈与回退保持低侵入

stylus 可触发轻量 haptic 反馈；haptic 不可用时退化为视觉状态或日志。stylus 按钮命令只作为增强输入，现有 UI 按钮仍是完整回退路径。

理由：训练流程应在 stylus 不可用、设备断开或反馈失败时继续可用。

## Risks / Trade-offs

- stylus pose 与 Marker / Scalpel 模型之间需要场景内偏移校准，否则 tip collider 可能与可见模型不一致。
- 训练桥接如果直接复制 UI 逻辑，后续流程改动会产生重复维护成本。
- 场景引用未完整绑定时，代码和 specs 已满足但运行体验仍可能不完整。
- haptic 能力依赖底层 runtime，可用性需要运行时验证。

## Implementation Plan

1. 使用项目内 stylus runtime 输出 `StylusInputs`，保持 MX Ink 输入语义独立。
2. 使用 XRI bridge 将 stylus 状态映射到 controller action，并支持 inactive 回退。
3. 新增或完善训练桥接组件，连接 `StylusHandler`、`SkillTrainingManager`、Marker、MarkerTip、Scalpel、ScalpelTip。
4. 在 Step 1-3 激活时由训练桥接组件接管对应工具位姿；在其他步骤或 stylus inactive 时释放接管。
5. 接入 stylus 按钮流程命令、可降级反馈和现有 UI 回退。
6. 通过 MCP 完成场景组件和对象引用绑定，并验证 Step 1-3、Step 4、反馈和报告兼容性。
