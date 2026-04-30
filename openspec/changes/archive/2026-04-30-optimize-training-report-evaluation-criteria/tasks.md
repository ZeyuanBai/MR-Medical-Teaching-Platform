## 1. 评分配置与数据模型

- [x] 1.1 修改 `Assets/Scripts/RubricConfig.cs`，补齐分步权重、评分阈值、合法方向、门槛规则、采样率、手势稳定性阈值、报告建议和版本信息配置。
- [x] 1.2 修改 `Assets/Scripts/TrainingEvaluationModels.cs`，补齐会话事件、位姿采样、原始指标、标准化指标、分步得分、主评分、门槛状态、观察指标、导出元数据和趋势展示所需的数据结构。
- [x] 1.3 在 `Assets/Scenes/MRMedicalTeachingPlatform.unity` 中连接评分配置，确保主训练场景使用当前场景指定的评分规则和版本信息。

## 2. 会话采集与训练步骤接入

- [x] 2.1 修改 `Assets/Scripts/SessionRecorder.cs`，统一采集步骤生命周期事件、重试事件、强制结束事件、异常流程事件和器械尖端位姿采样。
- [x] 2.2 修改 `Assets/Scripts/SkillTrainingManager.cs`，将训练开始、步骤切换、步骤完成、重试和训练结束接入会话采集。
- [x] 2.3 修改 `Assets/Scripts/PositionDetermination.cs`，向会话记录提供定位步骤的标记和定位结果证据。
- [x] 2.4 修改 `Assets/Scripts/CutSkin.cs` 和 `Assets/Scripts/CutAirway.cs`，向会话记录提供切开步骤的方向、位置、长度和路径证据。
- [x] 2.5 修改 `Assets/Scripts/InsertTracheal.cs`，通过碰撞体进入/退出状态提供插管成功证据；本 change 不要求稳定停留或插管方向依据。
- [x] 2.6 在 `Assets/Scenes/MRMedicalTeachingPlatform.unity` 中补齐会话采集组件、训练步骤脚本引用和器械尖端引用。

## 3. 分步骤指标计算

- [x] 3.1 修改 `Assets/Scripts/TrainingMeasurementUtility.cs`，基于目标区域局部关系计算定位角度误差、定位中点偏移和标记长度。
- [x] 3.2 修改 `Assets/Scripts/TrainingMeasurementUtility.cs`，计算切开方向误差、切开长度、切口中心偏移和路径效率。
- [x] 3.3 修改 `Assets/Scripts/InsertTracheal.cs` 和 `Assets/Scripts/TrainingReportManager.cs`，基于碰撞体插入状态计算插管成功状态；稳定停留时长和插管角度误差不作为本 change 的判定依据。
- [x] 3.4 验证模型整体位姿、场景缩放或父物体旋转变化时，局部坐标指标仍保持可复现。

## 4. 评分与门槛判定

- [x] 4.1 修改 `Assets/Scripts/ScoreEngine.cs`，按评分配置将分步骤指标标准化并计算定位、切皮、切气道和插管分步得分。
- [x] 4.2 修改 `Assets/Scripts/ScoreEngine.cs`，按分步权重汇总主评分，并保证同一输入与同一配置输出一致结果。
- [x] 4.3 修改 `Assets/Scripts/ScoreEngine.cs`，实现横切、竖切和两者均允许的方向判定。
- [x] 4.4 修改 `Assets/Scripts/ScoreEngine.cs`，实现关键步骤未完成、跳步、逆序和危险事件超阈值的门槛判定。
- [x] 4.5 修改 `Assets/Scripts/ScoreEngine.cs`，确保完成时间、分步耗时和重试次数只作为观察指标，不参与主评分扣分。

## 5. 教学报告与本地导出

- [x] 5.1 修改 `Assets/Scripts/TrainingReportManager.cs`，展示最终得分、分步得分、门槛状态、观察指标和可执行改进建议。
- [x] 5.2 修改 `Assets/Scripts/TrainingReportManager.cs`，展示重复训练的关键误差指标和主评分变化趋势。
- [x] 5.3 修改 `Assets/Scripts/ResearchExporter.cs`，生成 CSV 本地导出，包含原始指标、标准化指标、门槛状态、字段说明和版本字段。
- [x] 5.4 修改 `Assets/Scripts/ResearchExporter.cs`，生成 JSON 本地导出，包含会话元数据、事件摘要、指标明细、评分结果、门槛状态和版本字段。
- [x] 5.5 在 `Assets/Scenes/MRMedicalTeachingPlatform.unity` 中连接教学报告界面和本地导出入口。

## 6. 验证与验收

- [x] 6.1 在用户完成实操测试后，读取训练报告或本地导出结果，简单核对采集、评分、报告和导出字段是否与本次实现目标一致。
- [x] 6.2 基于用户提供的实操结果，核对横切、竖切、双路径、明显偏移、危险事件和关键步骤未完成等核心分支的评分与门槛结论是否符合预期。
- [x] 6.4 核对完成时间、分步耗时和重试次数只作为观察指标出现在报告和导出中，不参与主评分扣分。
- [x] 6.5 核对同一会话数据在相同评分配置下重复计算时，最终得分、分步得分、门槛状态和导出内容保持一致。
- [x] 6.6 运行 Unity 脚本编译检查，确认相关脚本、场景引用和资源配置不存在编译错误或缺失引用。
