## Why

当前项目已经能够通过读取右手交互 profile 判断 Logitech MX Ink 触控笔何时接入，并基于 Meta 内部注册的 MX Ink 兼容 profile 完成触控笔与右手控制器的自动切换；但由于这个内部 profile 会与 Logitech 官方提供给 OpenXR 的 MX Ink profile 冲突，项目目前不能直接切换到官方 profile 路径。现有输入仍落在通用控制器语义上，触控笔 tip、front、middle、back 等按键语义没有按官方 MX Ink action 正确恢复，导致多个交互功能表现为被 front 按键承担。

技能培训流程的第 1-3 步本身已经围绕 MarkerTip 与 ScalpelTip 的物理路径采样构建，适合接入 MX Ink 作为真实手持器械的空间输入与触觉反馈来源，让定位、切开皮肤/组织、切开气管三个步骤更接近实体操作。

## What Changes

- 保持现有基于 Meta 内部注册 profile 的 MX Ink 接入检测与右手模型自动切换机制，不改为完全依赖官方 Logitech profile 检测。
- 为 MX Ink 建立独立的输入语义恢复能力，以官方 `MX_Ink.inputactions` 中的 tip、front/grab、middle、back/option 映射作为目标语义。
- 在当前 Meta 内部 profile 运行路径下，将触控笔输入转换为项目统一的 `StylusInputs` 状态，避免 tip、middle、back 等输入被通用 controller action 误折叠到 front/grab。
- 将 MX Ink 接入技能培训第 1-3 步，使其可作为 Marker 与 Scalpel 的物理代理，驱动工具尖端位姿、碰撞采样与流程反馈。
- 为第 1-3 步提供面向触控笔的反馈策略，例如接触/下压反馈、步骤确认反馈和无效操作反馈。
- 保留现有手柄与 UI 操作路径作为回退，不要求用户必须使用 MX Ink 才能完成训练。
- 所有实现必须通过项目内新建或现有自有脚本完成，不修改 Logitech 提供的脚本、预制体或其他资源。
- 场景接入必须通过 MCP 完成，不直接编辑场景文件。
- 能提前挂载在场景中的脚本优先在场景中预挂载，不在初始化阶段再动态挂载。

## Capabilities

### New Capabilities

- `mx-ink-input-semantics`: 定义 MX Ink 在现有 Meta 内部 profile 接入方式下的输入检测、按钮语义恢复、统一状态输出与回退行为。
- `mx-ink-training-tool-feedback`: 定义 MX Ink 在气管切开技能培训第 1-3 步中作为 Marker/Scalpel 物理代理与触觉反馈来源的训练交互行为。

### Modified Capabilities

- 无。

## Impact

- 受影响的输入与触控笔运行时代码包括 `Assets/Scripts/MetaMxInkRuntime.cs`、`Assets/Logitech/Scripts/StylusHandler.cs`、`Assets/Logitech/Scripts/MxInkHandler.cs`、`Assets/Logitech/UnityXR_InputActions/MX_Ink.inputactions` 以及 Logitech OpenXR interaction profile 相关代码。Logitech 官方 action/profile 资源作为语义参考与必要的输入绑定依据，但实现必须兼容当前 Meta 内部 profile 的接入路径。
- 受影响的培训流程包括 `Assets/Scripts/SkillTrainingManager.cs`、`Assets/Scripts/PositionDetermination.cs`、`Assets/Scripts/CutSkin.cs`、`Assets/Scripts/CutAirway.cs` 以及 Marker/Scalpel 相关场景引用、碰撞体和标签配置。
- 可能需要新增一个运行时桥接组件，用于将 MX Ink 位姿与按钮状态映射到当前训练步骤的物理工具对象，并在触控笔未接入时保持现有工具与 UI 流程可用。
- 不引入新的外部依赖；依赖现有 Unity Input System、OpenXR、XR Interaction Toolkit、Meta XR SDK 与项目内已有 Logitech MX Ink action/profile 资源。
- 不修改 Logitech 提供的脚本、预制体或场景资源；如需扩展行为，只能通过项目内自有脚本、组件引用和运行时桥接实现。
- 场景层面的接入与引用绑定由 MCP 处理，不把场景序列化文件作为直接编辑目标。
