# MCP Prompt: SkillTrainingModelTable Scale Normalization

请使用 Unity MCP 辅助检查并优化当前训练场景的模型缩放与评分参考系，目标是降低碰撞体距离计算和实际操作之间的误差。

## 背景

项目是 Unity MR 医学教学平台，训练场景中 `SkillTrainingModelTable` 承载气管切开 4 步训练模型、交互碰撞体、步骤 Region、标准参考线和可视 mesh。当前评价体系已经在脚本侧改为使用无缩放局部测量坐标：

```text
local = inverse(reference.rotation) * (world - reference.position)
```

但场景层仍可能存在父物体或 mesh 的非 1:1:1 缩放，导致碰撞体、视觉 mesh 和评估参考点维护困难。

## 目标

1. 检查 `SkillTrainingModelTable` 及其子物体的 `localScale`、Collider、MeshRenderer、SkinnedMeshRenderer、XR Grab/Interactable 组件。
2. 将 `SkillTrainingModelTable` 根节点调整为 `{x:1, y:1, z:1}`，并保留其当前世界位置和旋转。
3. 如果需要视觉缩放，请创建或使用一个子物体，例如 `SkillTrainingModelVisualRoot`，把仅用于显示的 mesh 放到该子物体下，并把原来的视觉缩放转移到该子物体。
4. 碰撞体、步骤 Region、`StandardLine`、`StandardA`、`StandardB`、交互入口和训练脚本引用应留在稳定的测量根或对应 Region 下，不要跟随视觉 mesh 继续做非均匀缩放。
5. 同步检查 BoxCollider/SphereCollider/CapsuleCollider/MeshCollider 的尺寸和中心，确保世界空间碰撞区域与调整前视觉目标一致。
6. 不要破坏现有脚本引用、按钮引用、XR Interaction Toolkit 交互组件和 PaintIn3D 配置。

## 验证

- 运行场景后，步骤一定位、步骤二切皮、步骤三切气管、步骤四插管仍能触发。
- `SkillTrainingModelTable.transform.localScale` 应为 `{1,1,1}`。
- 报告中的 `s1_mark_length_cm`、`s1_midpoint_offset_cm`、`s2_incision_length_cm`、`s2_center_offset_cm`、`s3_incision_length_cm`、`s3_center_offset_cm` 与实际操作目测量级一致。
- 点击步骤四完成按钮应生成训练报告或在 Console 输出明确缺失引用 warning，不应静默无反应。

## 输出要求

请列出：

- 修改过的 GameObject 层级。
- 调整过的 scale、collider size/center。
- 保留或重新绑定过的脚本引用。
- 验证结果和任何仍需人工确认的医学阈值。
