## Context

当前项目已经通过 `MetaMxInkRuntime` 读取右手交互 profile 来判断 MX Ink 是否接入，并在接入时切换触控笔模型与右手控制器模型。这个检测路径依赖 Meta 内部注册的 MX Ink 兼容 profile；该 profile 会与 Logitech 官方 OpenXR MX Ink profile 冲突，因此本 change 不把接入检测迁移到官方 Logitech profile。

现有 `MetaMxInkRuntime` 主要承担 profile 检测、模型显示和 `StylusInputs` 暴露职责，但当前输入状态被清空，没有恢复 tip、front、middle、back 的独立语义。项目内已有 Logitech 官方 `MX_Ink.inputactions` 和 `MxInkHandler`，其中清楚定义了官方语义映射：tip 对应笔尖压力，front/grab 对应前键，middle 对应中键压力，back/option 对应后键。后续实现应以这些语义作为目标，而不是直接依赖官方 profile 完成设备识别。

技能培训流程第 1-3 步已经围绕实体工具尖端构建：Step 1 使用 `MarkerTip` 采样定位路径，Step 2 和 Step 3 使用 `ScalpelTip` 采样切开路径。接入 MX Ink 的合理方式是让触控笔作为这些工具的物理代理，驱动工具对象和尖端碰撞体，而不是重写训练评价、路径测量和报告系统。

## Goals / Non-Goals

**Goals:**

- 保持现有基于 Meta 内部 profile 的 MX Ink 接入检测与模型切换方式。
- 恢复 MX Ink 的独立输入语义，使 tip、front、middle、back 不再被通用 controller action 错误折叠。
- 将恢复后的输入统一写入 `StylusInputs`，供绘制、视觉反馈、训练桥接和后续功能复用。
- 在第 1-3 步中使用 MX Ink 驱动 Marker/Scalpel 的位姿与尖端碰撞采样。
- 为触控笔接触、按压、步骤确认和无效操作提供可配置的 haptic 或视觉反馈。
- 保留现有 UI 与手柄操作路径，在 MX Ink 未接入或输入不可用时仍可完成训练。

**Non-Goals:**

- 不启用或切换到 Logitech 官方 OpenXR MX Ink profile 作为唯一接入检测方式。
- 不使用 `RightHandDeviceDetector` 或 `RightHandMxInkSwitcher` 作为本 change 的实现依赖。
- 不重写 `PositionDetermination`、`CutSkin`、`CutAirway` 的路径测量算法。
- 不改变现有训练评价报告的核心评分规则。
- 不要求 Step 4 插管流程在本 change 中接入 MX Ink。
- 不修改 Logitech 提供的脚本、预制体或其他资源；所有行为扩展都必须通过项目内脚本完成。
- 不直接编辑场景文件；场景对象、引用和组件接入应通过 MCP 完成。
- 不在初始化阶段动态挂载本可提前放入场景的脚本。

## Decisions

### 1. 保留 Meta 内部 profile 检测，单独恢复输入语义

实现应继续由 `MetaMxInkRuntime` 或其直接协作组件读取当前右手 profile，判断 MX Ink 是否处于激活状态。输入语义恢复应作为独立层处理，输出统一的 `StylusInputs`。

理由：当前项目已经证明可以通过 profile 判断触控笔接入；改用 Logitech 官方 profile 会引入已知冲突，风险高且偏离当前可用路径。

备选方案：完全迁移到 Logitech 官方 OpenXR profile。该方案语义更正统，但会与 Meta 内部 profile 冲突，且可能破坏现有自动切换，因此不采用。

### 2. 以官方 `MX_Ink.inputactions` 作为语义契约，而不是检测契约

官方 action asset 用于定义按钮目标语义：`Ink_Tip`、`Grab`、`Ink_MiddleButton`、`Option`。实现可以直接复用这些 `InputActionReference`，也可以在 Meta 内部 profile 下建立等价映射；无论底层读取路径如何，最终都必须输出同一套 `StylusInputs` 字段。

理由：这样既保留官方按钮含义，又不强迫项目走官方 profile 检测路径。

备选方案：继续使用通用 controller action。该方案改动小，但会延续 front 按键承担多个功能的问题，因此不采用。

### 3. 将训练接入做成运行时桥接组件

新增或扩展一个训练桥接组件，负责根据 `SkillTrainingManager.CurrentStep` 选择当前物理代理：

- Step 1：MX Ink 驱动 Marker 与 MarkerTip。
- Step 2：MX Ink 驱动 Scalpel 与 ScalpelTip。
- Step 3：MX Ink 驱动 Scalpel 与 ScalpelTip。

桥接组件只负责工具对象的位姿、启用状态、按钮到流程命令的转换和反馈触发，不直接计算路径指标。

理由：现有训练步骤已经通过工具尖端 collider 采样，桥接位姿即可复用原有测量与评价逻辑。

备选方案：直接在 `PositionDetermination`、`CutSkin`、`CutAirway` 中读取触控笔输入。该方案会把设备输入和训练测量耦合，后续维护与回退更困难，因此不采用。

实现约束：桥接组件必须是项目自有脚本，不得依赖修改 Logitech 提供的脚本或 prefab；其挂载与引用配置应优先在场景中预置，而不是在运行时补挂。

### 4. 保留 UI 完成按钮，触控笔按钮作为增强路径

触控笔按钮可以触发步骤确认、重试或取消等流程动作，但不应移除现有 UI 按钮。Step 1-3 的完成仍应兼容当前 `SkillTrainingManager` 的完成状态字段，例如 `isPositionDetermined`、`isCutOver`。

理由：PCVR 场景中设备连接、输入绑定和操作习惯存在差异，保留 UI 是必要回退。

备选方案：强制所有 Step 1-3 操作都由 MX Ink 完成。该方案体验一致但容错差，不适合当前阶段。

### 5. 反馈策略应可配置且低侵入

触控笔反馈应优先从统一 `StylusInputs` 和步骤状态触发：

- 接触有效区域时给轻微反馈。
- 按压/下刀达到阈值时给持续或短促反馈。
- 步骤确认成功时给明确点击反馈。
- 当前步骤不匹配或操作无效时给不同反馈。

如果 haptic 在当前 profile 路径下不可用，应降级为视觉反馈或日志提示，不阻塞训练流程。

## Risks / Trade-offs

- Meta 内部 profile 的底层控制路径不稳定 -> 将输入语义恢复封装在独立层，避免散落到训练脚本中。
- 官方 action binding 在当前 profile 下可能无法直接读到值 -> 允许实现提供 fallback 映射，但输出字段必须保持官方语义。
- haptic 可能在当前接入路径下不可用 -> haptic 作为增强反馈，失败时不影响训练完成。
- 工具位姿直接跟随 MX Ink 可能与现有场景初始位置冲突 -> 桥接组件仅在 MX Ink active 且训练 Step 1-3 激活时接管工具位姿，退出时恢复现有流程。
- 按钮到流程命令映射可能影响用户习惯 -> 保留 UI 操作，并在 inspector 中暴露关键映射或阈值。

## Migration Plan

1. 在现有场景中保留 `MetaMxInkRuntime` 的 profile 检测和模型切换引用。
2. 为 `MetaMxInkRuntime` 增加或挂接输入语义恢复层，将 tip、front、middle、back 写入 `StylusInputs`。
3. 新增训练桥接组件，引用 `SkillTrainingManager`、MX Ink runtime、Marker、MarkerTip、Scalpel、ScalpelTip。
4. 在 Step 1-3 激活时由桥接组件接管对应工具位姿；在其他步骤、触控笔未接入或组件禁用时释放接管。
5. 配置触觉/视觉反馈阈值，并验证无 haptic 时的降级行为。
6. 保留现有 UI 和手柄路径作为回退；如出现设备兼容问题，可禁用桥接组件回滚到当前行为。

## Open Questions

- 当前 Meta 内部 profile 下，tip、middle、back 是否能通过 Unity Input System 直接读取到独立控制值，还是需要通过 Meta/OVR 输入路径建立 fallback？
- Step 1-3 的按钮映射是否统一使用 front 作为激活、middle/tip 作为接触压力、back 作为重试/取消，还是需要按临床工具动作分别配置？
- MX Ink 位姿与 Marker/Scalpel 模型之间是否需要固定偏移校准，以匹配真实笔尖和虚拟工具尖端？
