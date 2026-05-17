## 1. Stylus 输入语义

- [x] 1.1 提供项目内 `StylusHandler` / `StylusInputs` 作为统一 stylus 输入状态。
- [x] 1.2 在 `Assets/Scripts/MxInkSwitcher.cs` 中读取 MX Ink pose、tip、front/grab、middle、back/option 输入，并写入 `StylusInputs`。
- [x] 1.3 在 stylus inactive 时清空 tip、front、middle、back、double tap 和 aggregate 输入状态。
- [x] 1.4 保持 MX Ink 输入语义调整位于项目内脚本，不修改 Logitech 提供的脚本、prefab 或 input action assets。

## 2. XRI Controller Bridge

- [x] 2.1 提供 `Assets/Scripts/MxInkXRIControllerBridge.cs`，从 `StylusHandler.CurrentState` 读取 stylus 状态。
- [x] 2.2 支持将 tip、middle、front、back、back double tap 映射到 `select`、`activate`、`uiPress` 等 XRI controller action。
- [x] 2.3 支持模拟量阈值、布尔输入、多个输入源合并和 controller action value 输出。
- [x] 2.4 在 stylus inactive 或 bridge 停止接管时恢复原始 `ActionBasedController` input / tracking 配置。
- [x] 2.5 通过 MCP 检查训练场景中 stylus runtime、XRI bridge 与 controller 对象的最终引用绑定。

## 3. 训练工具代理接入

- [x] 3.1 确认现有 Step 1 仍通过 `MarkerTip` collider 和 `PositionDetermination` 采样定位路径。
- [x] 3.2 确认现有 Step 2 仍通过 `ScalpelTip` collider 和 `CutSkin` 采样切开路径。
- [x] 3.3 确认现有 Step 3 仍通过 `ScalpelTip` collider 和 `CutAirway` 采样切开路径。
- [x] 3.4 新增或完善训练桥接组件，将 `StylusHandler` 状态接入 `SkillTrainingManager`、Marker、MarkerTip、Scalpel 和 ScalpelTip。
- [x] 3.5 在训练桥接组件中实现 Step 1-3 的工具代理位姿接管。
- [x] 3.6 在训练桥接组件中实现 Step 4、Idle 或 stylus inactive 时释放工具接管，并确保不接管 Tracheal / TrachealTip。

## 4. 流程命令与反馈

- [x] 4.1 将 stylus 按钮映射为 Step 1 的确认、重试或取消等增强流程命令，并复用现有 UI 路径的状态更新效果。
- [x] 4.2 将 stylus 按钮映射为 Step 2-3 的完成、重试或取消等增强流程命令，并复用现有 UI 路径的状态更新效果。
- [x] 4.3 保持现有 UI buttons 在 stylus 不可用或未配置命令时可继续完成 Step 1-3。
- [ ] 4.4 为 valid contact、successful confirmation 和 invalid operation 接入 haptic、视觉或日志反馈。
- [x] 4.5 确认 feedback failure 不阻塞训练步骤状态更新。

## 5. 评分、报告与场景配置

- [x] 5.1 确认 `PositionDetermination`、`CutSkin`、`CutAirway` 保持现有测量和评分职责，未被 stylus 输入层替代。
- [x] 5.2 确认 `TrainingReportManager`、`SessionRecorder` 继续使用现有训练评价数据结构和评分规则。
- [x] 5.3 通过 MCP 在训练场景中挂载并配置训练桥接组件。
- [x] 5.4 通过 MCP 绑定 stylus runtime、`SkillTrainingManager`、Marker、MarkerTip、Scalpel、ScalpelTip、偏移和反馈对象引用。
- [ ] 5.5 验证 Step 1-3 stylus 接管、Step 4 不接管、XRI action 映射、UI 回退、反馈和报告兼容性。
