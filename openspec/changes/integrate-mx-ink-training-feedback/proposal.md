## Why

气管切开技能训练的第 1-3 步依赖 Marker / Scalpel 及其 tip collider 采样来完成定位、切开路径记录和结果评估。MX Ink 接入后应作为这些训练工具的触控笔代理，使学员能够用触控笔完成定位和切开操作，同时保留现有 UI 与手柄流程作为回退路径。

该 change 需要把 MX Ink 输入整理为稳定的项目内输入语义，并把这些语义接入 XRI controller action、训练工具代理、流程命令和反馈链路。训练评估、报告和 Logitech 官方资源不应因为接入 MX Ink 而改变核心口径。

## What Changes

- 定义项目内 `StylusHandler` / `StylusInputs` 作为 MX Ink / stylus 的统一输入状态来源。
- 保持 MX Ink tip、front/grab、middle、back/option 的独立输入语义，并允许这些输入映射到 XRI `select`、`activate`、`uiPress` 等 controller action。
- 在训练 Step 1-3 中使用 stylus pose 驱动 Marker / Scalpel 工具代理及其 tip collider，使现有路径采样逻辑继续工作。
- 支持将 stylus 按钮作为 Step 1-3 的确认、重试或取消等增强流程命令，同时保留现有 UI 操作路径。
- 为有效接触、确认成功和无效操作提供可降级反馈，优先使用 haptic，无法使用时退化为视觉状态或日志提示。
- 保持 Step 4 插管流程、训练评分、报告数据结构和 Logitech 官方脚本、prefab、input actions 不变。
- 场景对象、组件引用和运行时绑定通过 MCP 配置，不直接手动编辑场景文件。

## Capabilities

### New Capabilities

- `mx-ink-input-semantics`: 定义 MX Ink / stylus 的统一输入状态、独立按键语义、XRI controller action 映射和 inactive 回退行为。
- `mx-ink-training-tool-feedback`: 定义 MX Ink 在气管切开训练 Step 1-3 中的工具代理接管、流程命令增强输入、反馈和评分兼容行为。

### Modified Capabilities

- 无。

## Impact

- 输入与触控笔运行时代码：`Assets/Logitech/Scripts/StylusHandler.cs`、`Assets/Scripts/MxInkSwitcher.cs`、`Assets/Scripts/MxInkXRIControllerBridge.cs`、`Assets/Logitech/UnityXR_InputActions/MX_Ink.inputactions`。
- 训练流程代码：`Assets/Scripts/SkillTrainingManager.cs`、`Assets/Scripts/PositionDetermination.cs`、`Assets/Scripts/CutSkin.cs`、`Assets/Scripts/CutAirway.cs`。
- 训练报告链路：`Assets/Scripts/TrainingReportManager.cs`、`Assets/Scripts/SessionRecorder.cs` 应继续复用现有评价数据结构和评分规则。
- 场景配置：训练场景需要绑定 stylus runtime、XRI bridge、训练桥接组件、Marker / Scalpel / tip 对象和必要反馈对象。
