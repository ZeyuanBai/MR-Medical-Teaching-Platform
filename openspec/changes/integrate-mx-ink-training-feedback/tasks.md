## 1. 输入语义与接入保持

- [x] 1.1 调整 `Assets/Scripts/MetaMxInkRuntime.cs`，保留当前基于 Meta 内部 MX Ink 兼容 profile 的接入判断，并把 MX Ink 的独立输入语义重新写入 `StylusInputs`。
- [x] 1.2 结合 `Assets/Logitech/Scripts/MxInkHandler.cs` 和 `Assets/Logitech/UnityXR_InputActions/MX_Ink.inputactions`，补齐 tip、front/grab、middle、back/option 的输入映射与 fallback 读取路径。
- [x] 1.3 更新 `Assets/Logitech/Scripts/StylusHandler.cs`，确保 active / inactive、按钮状态、压力值与位姿状态的输出行为和新的 MX Ink 语义一致。
- [x] 1.4 补充必要的可诊断日志或调试开关，便于排查 profile 匹配、action 可读性、fallback 命中和输入折叠问题。
- [x] 1.5 所有输入语义调整都通过项目内脚本完成，不修改 Logitech 提供的脚本、预制体或输入资源文件。

## 2. 训练工具桥接

- [ ] 2.1 新建一个 `Assets/Scripts/` 下的 MX Ink 训练桥接脚本，负责把 `MetaMxInkRuntime` 的状态接到训练流程。
- [ ] 2.2 在该桥接脚本中实现 Step 1-3 的工具接管逻辑，让 MX Ink active 时驱动 Marker、Scalpel 及其 tip 对象。
- [ ] 2.3 在该桥接脚本中实现非目标步骤释放逻辑，确保 Step 4、Idle 或 MX Ink inactive 时不接管 Tracheal 等工具对象。
- [ ] 2.4 在该桥接脚本中加入按钮到流程命令的映射，作为 Step 1-3 确认、重试或取消的增强输入，并保留现有 UI 回退路径。
- [ ] 2.5 桥接脚本按场景预挂载方式接入，避免在初始化阶段动态挂载本可以提前配置的组件。

## 3. 训练流程与反馈接入

- [ ] 3.1 视需要轻量调整 `Assets/Scripts/SkillTrainingManager.cs`，让桥接脚本能够复用 Step 1-3 的确认、重试和推进入口，而不是复制 UI 逻辑。
- [ ] 3.2 检查并必要时微调 `Assets/Scripts/PositionDetermination.cs`、`Assets/Scripts/CutSkin.cs`、`Assets/Scripts/CutAirway.cs`，保证 MX Ink 接管后仍能正常完成 collider 采样与评分。
- [ ] 3.3 为 MX Ink 的有效接触、确认成功和无效操作补充 haptic、视觉或日志反馈，落在现有训练反馈链路中。
- [ ] 3.4 确认 `Assets/Scripts/TrainingReportManager.cs`、`Assets/Scripts/SessionRecorder.cs` 等报告链路保持不变，MX Ink 只影响输入与工具驱动，不覆盖评分结果。

## 4. 场景配置与验证

- [ ] 4.1 在当前训练场景中挂载 MX Ink 桥接脚本，并绑定 `MetaMxInkRuntime`、`SkillTrainingManager` 及 Marker / Scalpel / tip / Tracheal 相关对象。
- [ ] 4.2 配置工具位姿偏移、输入 action reference 和必要的 collider / tag 关联，确保桥接逻辑能在场景中正确工作。
- [ ] 4.3 验证 profile 接入、独立输入、Step 1-3 接管、Step 4 不接管、反馈和报告兼容性，确认项目仍可回退到原有手柄和 UI 训练行为。
- [ ] 4.4 所有场景绑定和对象配置通过 MCP 完成，不直接编辑场景文件。
