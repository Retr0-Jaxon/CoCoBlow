# CocoBlow 椰子吹落 — 测试与参数调试指南

> 适用场景：`SampleScene` 中「树上生成椰子 → 吹风机吹落 → 落地/进池计分」垂直切片测试。  
> 最后同步代码版本：防冲天方案（释放方向修正 + 风力冷却 + Rigidbody 阻力）。

---

## 1. 测试前准备

| 项目 | 说明 |
|------|------|
| 场景 | `Assets/Scenes/SampleScene.unity` |
| 椰子树 | `coconut tree_03（之后完整模型重命名）` |
| 挂点 | 树下 `CoconutSpawnPoints/SpawnPoint_01` ~ `05` |
| 椰子预制体 | `Assets/Prefabs/TinyCoconut.prefab` |
| 提交区 | `CoconutSubmitZone`（发光池 Trigger） |
| HUD | `Canvas` → `CoconutCountText` |

**操作说明（玩家）：**

- `WASD` 移动，`鼠标` 视角
- 准星对准地面 `HairDryer`，按 `E` 拾取
- 按住 `左键` 吹风（需已拾取吹风机）
- 按 `G` 放下吹风机

---

## 2. 核心流程（测试时要走的链路）

```
CoconutSpawner 在挂点生成椰子（挂树、kinematic）
    ↓
HairDryer 对挂树椰子累计风力（ApplyWindForce）
    ↓
累计达标 → ReleaseFromTree（解除挂树、开启物理、施加释放冲量）
    ↓
0.3s 风力冷却期内不受普通 AddForce
    ↓
冷却结束后可被继续吹动 → 滚入 CoconutSubmitZone → GameManager 计分 + HUD 刷新
```

---

## 3. 可调参数总表

> **注意：** 带「场景当前值」的项以 `SampleScene` 里已序列化数值为准；脚本默认值仅作参考。  
> Play 模式下改 Inspector **不会持久保存**，调满意后请在 Edit 模式改并保存场景/预制体。

### 3.1 椰子树 — `CoconutSpawner` 组件

挂载对象：`coconut tree_03（之后完整模型重命名）`

| 参数字段 | 场景当前值 | 脚本默认 | 作用 | 调大 / 调小的效果 |
|----------|------------|----------|------|-------------------|
| **Coconut Prefab** | TinyCoconut | — | 生成的椰子预制体 | 换模型/碰撞体时改这里 |
| **Spawn Interval** | `8` | 8 | 补货间隔（秒） | 大→树上空档久；小→椰子更密 |
| **Max Active Coconuts** | `5` | 5 | 场上最多同时存在椰子数 | 大→可同时更多颗（含地上未销毁的） |
| **Spawn On Start** | ✓ | ✓ | 开局是否立刻补满 | 关→开局无椰子，只靠定时生成 |
| **Coconut Score Value** | `1` | 1 | 每颗提交得分 | 影响 GameManager 计数增量 |
| **Release Wind Force Threshold** | `120` | 120 | 吹落所需累计风力 | 大→需吹更久；小→更容易掉 |
| **Release Impulse** | `2` | 2 | 掉落瞬间冲量大小 | 大→弹得更远/可能仍偏飞；小→更「松脱落下」 |

**吹落耗时粗算（仅供参考）：**

- 每物理帧累计：`forceMagnitude × FixedDeltaTime`（约 0.02）
- 满力对准吹时，`forceMagnitude ≈ windForce`（见吹风机参数）
- 示例：`windForce=28`、阈值 `120` → 约 `120 / 28 / 50 ≈ 0.09s`（理想满力；实际因距离/角度衰减会更久）
- 示例：`windForce=242.6`、阈值 `120` → 约 **0.01s 量级**（几乎秒掉，需配合阈值一起调）

---

### 3.2 吹风机 — `HairDryer` 组件

挂载对象：场景根节点 `HairDryer`

| 参数字段 | 场景当前值 | 脚本默认 | 作用 | 调大 / 调小的效果 |
|----------|------------|----------|------|-------------------|
| **Wind Force** | **`242.6`** | 28 | 风力强度 | 大→吹动/吹落更快，易飞天；小→吹不动 |
| **Wind Range** | `8` | 8 | 风锥球形检测半径 | 大→更远能吹到；小→必须靠近 |
| **Wind Angle** | **`11.5`** | 28 | 风锥半角（度） | 小→必须更精准对准；大→容错高 |
| **Blow On Left Mouse** | ✓ | ✓ | 是否按住左键才吹风 | 关→拿起即吹 |

**风向来源：** `Nozzle` 的 `up` 方向（无 Nozzle 时用 `transform.forward`）。

---

### 3.3 椰子预制体 — `TinyCoconut` → `Rigidbody`

路径：`Assets/Prefabs/TinyCoconut.prefab`

| 参数字段 | 当前值 | 作用 | 调大 / 调小的效果 |
|----------|--------|------|-------------------|
| **Mass** | `1` | 质量 | 大→同样风力加速度小，手感更沉 |
| **Drag** | **`0.75`** | 线性阻力 | 大→速度上限低，不易越吹越快 |
| **Angular Drag** | **`0.25`** | 旋转阻力 | 大→滚转更快停下 |
| **Use Gravity** | ✓ | 重力 | 释放后必须为 ✓ |
| **Is Kinematic** | ✗（预制体） | 预制体默认 | 挂树时由脚本临时设为 kinematic |

---

### 3.4 椰子脚本 — `Coconut` 组件

> 运行时由 Spawner 动态添加/初始化；`windIgnoreDurationAfterRelease` 仅在预制体或场景实例上挂了 `Coconut` 且序列化该字段时才在 Inspector 可见。

| 参数字段 | 当前值 | 作用 | 调大 / 调小的效果 |
|----------|--------|------|-------------------|
| **Wind Ignore Duration After Release** | `0.3` | 刚掉落后忽略吹风机 `AddForce` 的时长（秒） | 大→更久只受重力；小→更容易被续吹顶飞 |

**释放方向逻辑（代码内固定比例，暂非 Inspector）：**

- 取吹风方向的**水平分量**
- 混合 **`向下 × 0.35`** 后归一化，再乘 `Release Impulse`
- 若几乎纯竖直吹 → 纯向下冲量

---

### 3.5 挂点 — `CoconutSpawnPoint`

挂载对象：`CoconutSpawnPoints/SpawnPoint_xx`

| 调整方式 | 说明 |
|----------|------|
| **Transform 位置** | 决定椰子长在树上的位置 |
| **Scene 视图 Gizmo** | 绿色=空闲，橙色=已有椰子 |

---

## 4. 标准测试用例

### TC-01 挂树静止

| 步骤 | 预期 |
|------|------|
| 进入 Play | 树上出现椰子（最多 5 颗，取决于挂点与 Max Active） |
| 不操作 5 秒 | 椰子不自行掉落、不下落 |

### TC-02 吹落树下

| 步骤 | 预期 |
|------|------|
| 拾取吹风机，对准树上椰子，按住左键 | 持续吹一段时间后椰子脱落 |
| 脱落瞬间 | **主要向下/斜下**落，不明显飞天 |
| 脱落约 0.3s 内 | 即使仍按住吹风，椰子先受重力下落（冷却期） |
| 落地后 | 可再次被吹动滚走 |

### TC-03 进池计分

| 步骤 | 预期 |
|------|------|
| 将椰子吹/滚进 `CoconutSubmitZone` | 椰子销毁 |
| 观察 HUD | `Coconut : N` 数字 +1 |
| Console（可选） | 出现提交成功 Log |

### TC-04 补货

| 步骤 | 预期 |
|------|------|
| 吹落或提交若干椰子 | 挂点空出 |
| 等待 Spawn Interval | 新椰子在空闲挂点生成 |

### TC-05 防冲天回归（重点）

| 步骤 | 预期 |
|------|------|
| 站在树下**抬头**对准椰子持续吹 | 椰子掉落，**不会**刚 kinematic=false 就直飞天空 |
| 若仍偏高 | 按第 5 节「问题对照表」逐项降参 |

---

## 5. 常见问题 → 该调哪个参数

| 现象 | 优先检查 | 建议调整方向 |
|------|----------|--------------|
| 一吹就掉，毫无手感 | `Release Wind Force Threshold` 太低 或 `Wind Force` 太高 | **阈值↑** 或 **Wind Force↓** |
| 怎么吹都不掉 | 阈值太高 / 风力太小 / 角度太偏 | **阈值↓** 或 **Wind Force↑** 或 **Wind Angle↑** |
| 刚掉就飞天 | 释放冲量太大 / 冷却太短 / Drag 太低 | **Release Impulse↓**、**Wind Ignore↑**、**Drag↑** |
| 掉落后吹不动 | Drag 太大 / Wind Force 太小 | **Drag↓** 或 **Wind Force↑** |
| 掉落后滚太远停不下来 | Drag 太小 | **Drag↑**（0.5~1.5 区间试） |
| 椰子生成位置不对 | 挂点 Transform | 移动 `SpawnPoint_xx` |
| 开局没有椰子 | `Spawn On Start` | 勾选；或等一个 Spawn Interval |
| 场上椰子不够 | `Max Active Coconuts` / 挂点数量 | 提高上限或增加挂点 |

---

## 6. 推荐调试顺序（团队统一流程）

1. **先固定吹风机**：在 Edit 模式记录 `Wind Force / Range / Angle`（当前场景 Wind Force 已远高于脚本默认，后续对比务必注明场景值）。
2. **调吹落难度**：只动 `Release Wind Force Threshold`，直到「需对准吹 1~3 秒」才掉。
3. **调掉落形态**：只动 `Release Impulse`（建议 1~3）+ 观察是否飞天。
4. **仍飞天**：加 `Wind Ignore Duration After Release`（0.3 → 0.4~0.5）或 **Drag**（0.75 → 1.0）。
5. **调地面吹动手感**：在椰子落地后调 `Wind Force` 与 `Drag` 的平衡。
6. **最后调经济节奏**：`Spawn Interval`、`Max Active Coconuts`。

---

## 7. 参数推荐区间（原型期）

| 参数 | 建议区间 | 备注 |
|------|----------|------|
| Wind Force | 20 ~ 40（原型） | 当前场景 **242.6** 偏高，易导致秒掉+飞天风险 |
| Wind Range | 6 ~ 12 | 与树高、玩家站位匹配 |
| Wind Angle | 20 ~ 35 | 当前 **11.5** 很窄，瞄准难度高 |
| Release Wind Force Threshold | 80 ~ 200 | 与 Wind Force 联动 |
| Release Impulse | 1 ~ 3 | 防冲天方案后默认 **2** |
| Wind Ignore Duration After Release | 0.25 ~ 0.5 | 默认 **0.3** |
| Drag | 0.5 ~ 1.5 | 当前 **0.75** |
| Mass | 1 ~ 2 | 仍太轻可试 2 |
| Spawn Interval | 5 ~ 15 | 当前 8 |
| Max Active Coconuts | 3 ~ 8 | 当前 5，挂点 5 个 |

---

## 8. Inspector 定位速查

```
SampleScene
├── HairDryer                    → HairDryer（风力）
├── coconut tree_03...           → CoconutSpawner（生成/吹落阈值/冲量）
│   └── CoconutSpawnPoints
│       ├── SpawnPoint_01~05     → CoconutSpawnPoint + 位置
├── CoconutSubmitZone            → 提交触发
├── GameManager                  → 计分逻辑
└── Canvas / HUD                 → SimpleHUD 显示

Assets/Prefabs/TinyCoconut.prefab → Rigidbody 质量/阻力
```

---

## 9. 验收清单（发版前勾选）

- [ ] 树上椰子开局/补货正常
- [ ] 椰子挂树不自动掉落
- [ ] 对准吹落耗时符合设计（非秒掉也非吹不动）
- [ ] 掉落方向以向下/斜下为主，无明显冲天
- [ ] 掉落后可吹动并滚入提交池
- [ ] HUD 计数与 GameManager 一致
- [ ] 无 Console 红色 Error（kinematic 速度警告若仍有，单独记录）

---

## 10. 变更记录

| 日期 | 内容 |
|------|------|
| 2026-08-07 | 防冲天：释放方向改水平+向下、Release Impulse 6→2、风力冷却 0.3s、Drag 0→0.75 |

---

**调试协作建议：** 每人改参时在群里注明改了哪几个字段、改前/改后数值，避免多人同时改 `SampleScene` 冲突。
