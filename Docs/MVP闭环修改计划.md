# CocoBlow MVP 闭环修改计划

> 对照文档：`Game_Design_Document.md` v0.5  
> 工作场景：`Assets/Scenes/LRB_Scene.unity`  
> 日期：2026-08-13

本计划合并两类工作：

1. 额外需求：椰子/树比例、开始界面、结束黑屏回标题  
2. GDD MVP 必做中尚未对齐的部分  

**本计划明确不做：**

- 去掉跳跃（保留空格跳跃）  
- 「按 E 交互」提示文案  
- 改挂点数量（保持现有 5 个）  
- 科技树「每解锁一项点亮一颗椰子」  
- 纸条解密、运输、种植、结局插画（GDD 有空再做）  
- 重写吹风机为「半径米」物理（继续用现有锥角 `windAngle`）

***

## 1. 做完后的 Demo 流程

```
封面（开始 / 离开）
  → 开局手持吹风机（可跳跃）
  → 1 棵树、开局 1 个椰子、现有 5 挂点、同时 1 个、3 秒冷却
  → 吹落 → 吹进池 → 椅子升级（吹 5/12/20 + 树 8/15/25）
  → 吹强化 1/2/3：异常 + 解锁纸条 01/02/03
  → 树强化 3：再长出第 2 棵树
  → 进黑暗区：视野变暗 + 10 秒，超时回出生点
  → 读纸条 03 → 黑屏 Thank you for playing → 点击回到封面（新一局）
```

***

## 2. 实现顺序

| 批次 | 内容 | 原因 |
|------|------|------|
| A | 比例（挂树缩放 + 树/椰子大小） | 后面挂点、第二棵树都基于正确尺寸 |
| B | 开局手持吹风机 | 和封面暂停输入有交叉，先改持有逻辑 |
| C | 生产数值 + 升级表 + 第二棵树 | 纯数据和生成逻辑 |
| D | 三异常 + 三纸条文案 | 挂在升级事件上 |
| E | 黑暗区变暗 | 独立，可与 D 并行 |
| F | 封面开始/离开 + 结束黑屏回标题 | 依赖场景能完整重载 |

场景改动尽量在 Unity 里保存，少手改 `LRB_Scene.unity` YAML。

***

## 3. 批次 A — 椰子 / 树比例

### 现状

- 椰子树在 `LRB_Scene` 与预制体上 Scale 为 **0.1**。挂点是树的子物体（本地 Y≈30），世界高度大约 3m，相对玩家偏矮。  
- 椰子预制体 `TinyCoconut` Scale 为 **1**，但 `Coconut.AttachToTree` 会 `SetParent(spawnPoint)`。挂点跟着树变成 0.1，椰子世界缩放变成 **0.1**。掉落时 `SetParent(null, true)` 把 0.1 带下去。  
- 只放大树，椰子相对更小；只放大椰子预制体，挂上树仍会被乘 0.1。

### 改法

1. 修改 `Coconut.AttachToTree()`：按父物体 `lossyScale` 补偿 `localScale`，保持预制体世界大小。  
2. `ReleaseFromTree` 脱父时继续用 `worldPositionStays = true`，落地尺寸与树上一致。  
3. `LRB_Scene` 树 Scale **0.1 → 0.2**（高度大约 6m）。挂点是子物体，一般不用手挪。  
4. Play 模式再调 `TinyCoconut` Scale（起点 **2～3**），满意后在 Edit 模式写回预制体。  
5. 碰撞会跟着变。若视觉和碰撞差太多，只微调 `SphereCollider.radius`，不要只改 Mesh。

### 文件

- `Assets/Scripts/Coconut/Coconut.cs`  
- `Assets/Prefabs/TinyCoconut.prefab`  
- `Assets/Scenes/LRB_Scene.unity`（树实例 Scale）

### 验收

- 树上椰子和树冠匹配，不再像米粒  
- 吹落、进池、碰撞仍正常  
- 不改 `CoconutSpawner` 生成逻辑  

### 影响

只动椰子挂树缩放、树实例、椰子预制体。吹风机 / 升级 / UI 无逻辑依赖。若挂点过高或穿地，再微调挂点本地坐标。

***

## 4. 批次 B — 开局手持吹风机

### 现状

GDD：开局直接手持吹风机。  
场景：`isHeld: 0`，需按 E 拾取，G 可放下。

### 改法

- 场景加载后（或点「开始」进入游戏时）对当前 `HairDryer` 调用一次 `PickUp(cameraRoot)`。  
- **保留** E 拾取（万一掉落或换模失败时仍能捡）和 **G 放下**、**空格跳跃**。  
- 升级换 LV2 已有「手里替换」，不改。  
- 封面显示期间 `timeScale = 0`，吹风机在手里也吹不了，可以接受。

### 不做

- 不删跳跃  
- 不增加「按 E 交互」UI 文案（现有准星/手形可保留）

### 文件

- `Assets/Scripts/FirstPersonController.cs` 或新建流程里由 `GameFlowController` 调用 `PickUp`  
- `Assets/Scenes/LRB_Scene.unity`（确认 `cameraRoot` 引用）

### 验收

点开始进入游戏后，玩家已拿着吹风机，左键可吹。

### 影响

与批次 F 的开始界面配合：菜单打开时不锁鼠标、不处理移动；点开始后再 `PickUp` 并锁鼠标。

***

## 5. 批次 C — 椰子生产 + 升级表 + 第二棵树

### 5.1 生产数值（GDD §4）

| 项目 | GDD | 现状 | 本计划 |
|------|-----|------|--------|
| 椰子树 | 1 棵 | 1 | 开局保持 1 |
| 开局树上椰子 | 1 个 | spawnOnStart 补满 5 | `maxActiveCoconuts = 1` 后开局自然 1 个 |
| 可生成挂点 | 4 个 | 5 个（01～05） | **不改，保持 5 个** |
| 同时生成上限 | 1 个 | 5，且含地上未进池 | 上限 1；**只统计挂在树上的** |
| 生成冷却 | 3 秒 | 8 秒 | `spawnInterval = 3` |

地上未进池的椰子不占生成名额，否则吹下来就停产，Demo 节奏会卡死。

### 5.2 吹风机升级表（GDD §6.1）

场景里当前消耗是测试值 **1 / 2 / 3**，改回文档。风力物理继续用锥角，按 `atan(半径/射程)` 近似，不重写检测。

| 档 | 消耗 | 风力 | 范围 | 锥角（近似） |
|----|------|------|------|----------------|
| 初始 | 0 | 100 | 5 | ≈17°（对应半径 1.5m） |
| 强化 1 | 5 | 120 | 5.5 | ≈20° |
| 强化 2 | 12 | 150 | 7 | ≈19° |
| 强化 3 | 20 | 180 | 8 | ≈21° |

第 1 档换 `HairDryer_Lv2` 模型可保留。

### 5.3 椰子树升级表（GDD §6.2）

| 档 | 消耗 | 效果 |
|----|------|------|
| 强化 1 | 8 | 冷却 3s → 1.5s |
| 强化 2 | 15 | 同时可生成 2 个（只计挂树） |
| 强化 3 | 25 | **再实例化 1 棵同样的树（沿用现有 5 挂点结构）** |

### 5.4 第二棵树

当前 `GameManager.ApplyCoconutTreeUpgrade` 只改现有那一棵的间隔和上限，没有第二棵树。

- `GameManager` 增加：树预制体（或复制当前树）、生成位置（Inspector 拖空物体锚点）。  
- 强化 3：`Instantiate`，拷贝间隔 / 上限 / 椰子预制体。  
- `coconutSpawner` 改成列表，改间隔/上限时两棵都改。  
- 第二棵树独立生成；强化 2 之后**每棵**上限 2（两棵最多 4 个挂树椰子）。若全局仍上限 2，第二棵几乎不长椰子。

### 5.5 不做

科技树节点点亮（已解锁变亮）本轮不做。现有点击选中 + 文字状态（已升级 / 需要前置）保持即可。

### 文件

- `Assets/Managers/GameManager.cs`  
- `Assets/Scripts/Coconut/CoconutSpawner.cs`  
- `Assets/Scenes/LRB_Scene.unity`（升级数组、树组件、第二棵树锚点）

### 验收

- 开局 1 个椰子；吹落后约 3 秒补 1 个；强化前树上最多 1 个  
- 挂点仍为 5 个  
- 吹风机消耗 5 / 12 / 20，树消耗 8 / 15 / 25  
- 树强化 3 后场上出现第二棵树并长椰子  

### 影响

生成节奏变慢（更接近 10 分钟 Demo）。升级消耗变高，需按 GDD 主线约 37 椰子通关。HUD、进池逻辑不改。

***

## 6. 批次 D — 三异常 + 三纸条

### 6.1 异常（接到已有 `OnHairDryerUpgradeEvent`）

新建 `AnomalyController.cs`，不要把表现堆进 `GameManager`。异常只加不减：强化 3 之后 1+2+3 的效果都在。

| 顺序 | 触发 | 表现（按本次修订） |
|------|------|-------------------|
| 异常 1 | 吹风机强化 1 | **之后椰子落地时闪一次光**（不是持续发光） |
| 异常 2 | 吹风机强化 2 | **椰子落地后立即向随机方向移动一次**（不是持续朝玩家滚） |
| 异常 3 | 吹风机强化 3 | 播放异常声音 |

落地判定建议：挂树释放后，第一次与地面碰撞（`OnCollisionEnter`，法线偏上）或刚体速度接近静止且已离树。只触发一次，避免弹跳连闪/连冲。

**异常 1 实现要点：**

- 落地瞬间开 Emission 或短时 Point Light，约 0.2～0.4 秒后关  
- 仅强化 1 及之后生成/落地的椰子；强化前已在地上的不必补闪  

**异常 2 实现要点：**

- 落地瞬间给一次水平随机冲量（`AddForce` Impulse），不是每帧追踪玩家  
- 与异常 1 可叠：先闪光再冲一次  

**异常 3 实现要点：**

- 资源已有：`Assets/Sounds/紧张事件长trigger/mixkit-terror-radio-frequency-2566.wav`（或同目录「逐渐恐怖」）  
- 在场景/预制体 `AudioManager` 注册名称，例如 `anomaly`  
- 强化 3 时 `AudioManager.PlayAudio("anomaly", false)`  

### 6.2 纸条文案

三张纸、升级解锁、E 阅读已有。只改 `GameManager.notes[].content`：

| 编号 | 文案 |
|------|------|
| 01 | 这椰子要是能吃就好了 |
| 02 | 这里好像没有出口，怎么办 |
| 03 | 又回到这里了，这些该死的椰子。 |

读完 03 进结局见批次 F。

### 文件

- 新建 `Assets/Scripts/Environment/AnomalyController.cs`  
- `Assets/Scripts/Coconut/Coconut.cs`（落地回调、闪光、随机冲量）  
- `Assets/Managers/GameManager.cs`（升级钩子调用 `AnomalyController`）  
- `AudioManager` 场景或预制体增加一条音  
- `Assets/Scenes/LRB_Scene.unity`（纸条 content）

### 验收

- 升 1 后新落地的椰子闪一次光  
- 升 2 后落地立刻往随机水平方向冲一次  
- 升 3 听到异常声  
- 三张纸条为 GDD 原文  

### 影响

不改进池计分。随机冲量不要太大，避免飞出场景或冲出提交池范围；可在 Inspector 配冲量大小。

***

## 7. 批次 E — 黑暗区视野变暗

### 现状

墙、10 秒倒计时、回安全区清零、超时传到 `PlayerRespawnPoint`、不扣椰子——规则已有。  
缺「进入后视野变暗」。LRB 相机 Post Processing 关闭，上 Volume 要改 URP。

### 改法

HUD 全屏半透明黑图：

- `DarkZone` 离开安全区：显示 overlay，透明度可随剩余时间略增  
- 回安全区：关掉  
- 不改倒计时数字与传送逻辑  

### 文件

- `Assets/Scripts/Environment/DarkZone.cs`  
- `Assets/Scripts/UI/SimpleHUD.cs`  
- `Assets/Scenes/LRB_Scene.unity`（HUD 加全屏 Image）

### 验收

出安全区画面明显变暗且显示倒计时；10 秒传送；回来恢复。

### 影响

仅 HUD 表现。与 `KeepInsideArea`（其它场景的 7 秒出界）无关，LRB 不接入那套。

***

## 8. 批次 F — 开始界面 + 结束黑屏回标题

全程留在 `LRB_Scene`，用重载场景复位（椰子、升级、吹风机、纸条、玩家位置），不手写完整 `ResetGame()`。

### 8.1 开始界面

Canvas 下新建 `StartMenuPanel`（Sort Order 高于 HUD）：

| 元素 | 做法 |
|------|------|
| 背景 | 全屏 `Image`，Sprite = `Assets/Images/cover.png`，`Preserve Aspect` |
| 开始 | 按钮文案「开始」 |
| 离开 | 按钮文案「离开」 |

新建 `GameFlowController.cs`：

- 场景 `Start()`：显示开始界面，解锁鼠标，`Time.timeScale = 0`  
- 停走路 / 吹风机 Loop 音（`timeScale = 0` 时 AudioSource 仍可能响）  
- **开始：** 关面板，`timeScale = 1`，锁鼠标，执行批次 B 的 `PickUp`  
- **离开：** `Application.Quit()`；`#if UNITY_EDITOR` 下停止 Play  

`CoconutSpawner.spawnOnStart` 在 `timeScale = 0` 时 `Start()` 仍可能补货。封面挡住即可；若要严格「点开始才长椰子」，再改为由 Flow 调用生成。

`FirstPersonController.OnEnable` 目前会立刻锁鼠标。菜单打开时不得锁鼠标、不得处理移动。

### 8.2 结束界面

现有：关纸条 → `GameManager.OnNoteRead` → `ShowEndingPanel()`。保留这条触发链。

改 `EndingPanel`：

- 纯黑全屏  
- 居中 TMP：`Thank you for playing`  
- 去掉关闭按钮；整块可点  

弹出结局时 `timeScale = 0`、鼠标可见；**不允许 Esc 关掉后继续玩**。  
点击任意处：`SceneManager.LoadScene` 当前场景 → 重新 `Start()` → 又是封面。

`AudioManager` 不要做成 `DontDestroyOnLoad`（重载会重复实例）。加载瞬间可先保持黑屏，避免闪一帧游戏画面。

### 8.3 Build Settings

当前只有 `SampleScene`。把 `Assets/Scenes/LRB_Scene.unity` 加入 Build，设为第一个场景。

### 文件

- 新建 `Assets/Scripts/UI/GameFlowController.cs`  
- `Assets/Scripts/UI/SimplePanelUI.cs`（结局不可关闭，点击交给 Flow）  
- `Assets/Scripts/FirstPersonController.cs`（菜单打开时不锁鼠标）  
- `Assets/Managers/GameManager.cs`（仅确认结局仍走 `ShowEndingPanel`）  
- `Assets/Scenes/LRB_Scene.unity`  
- `ProjectSettings/EditorBuildSettings.asset`

### 验收

- 进 Play 先看到封面；点开始进入第一人称且已持吹风机  
- 离开在打包版退出；编辑器停 Play  
- 读完最后一张纸条 → 黑屏致谢 → 点击回到封面  
- 再点开始是新一局  

### 影响

不改升级、吹风机风锥、黑暗区规则。`cover.png` 已是 Sprite，不用改导入。

***

## 9. 文件总表

| 文件 | 批次 | 动作 |
|------|------|------|
| `Assets/Scripts/Coconut/Coconut.cs` | A, D | 挂树缩放补偿；落地闪光；落地随机冲量 |
| `Assets/Prefabs/TinyCoconut.prefab` | A | 调 Scale / 必要时 Collider |
| `Assets/Scripts/FirstPersonController.cs` | B, F | 开局 PickUp；菜单时不锁鼠标 |
| `Assets/Scripts/Coconut/CoconutSpawner.cs` | C | 间隔/上限；只统计挂树数量 |
| `Assets/Managers/GameManager.cs` | C, D, F | 升级表、第二棵树、异常钩子、结局入口 |
| `Assets/Scripts/UI/UpgradeNodeButton.cs` | — | **本轮不改** |
| 新建 `Assets/Scripts/Environment/AnomalyController.cs` | D | 异常 1/2/3 开关与触发 |
| `Assets/Scripts/Environment/DarkZone.cs` | E | 通知 HUD 变暗 |
| `Assets/Scripts/UI/SimpleHUD.cs` | E | 全屏变暗 overlay |
| 新建 `Assets/Scripts/UI/GameFlowController.cs` | F | 开始、暂停、退出、重载 |
| `Assets/Scripts/UI/SimplePanelUI.cs` | F | 结局黑屏、点击回标题 |
| `Assets/Scenes/LRB_Scene.unity` | 全部 | 数值、UI、锚点、纸条 |
| `ProjectSettings/EditorBuildSettings.asset` | F | 加入并优先 LRB_Scene |
| `AudioManager` 场景/预制体 | D | 注册 anomaly 音效 |

***

## 10. 本轮默认值（已拍板）

- 树 Scale 先 0.2，椰子对着树再调  
- 结束文案：`Thank you for playing`  
- 离开 = 退出游戏  
- 风力仍用锥角，用表内近似角度  
- 第二棵树与第一棵同一套间隔；每棵上限在强化 2 之后为 2  
- 异常 3 用 `mixkit-terror-radio-frequency-2566.wav`  
- 保留跳跃；无「按 E 交互」文案；挂点保持 5 个；科技树点亮不做  
- 异常 1 = 落地闪一次光；异常 2 = 落地后随机方向移动一次  

***

## 11. 验收总清单（相对 GDD MVP 必做）

- [ ] 移动、视角、按住左键吹风（保留跳跃）  
- [ ] 开局手持吹风机  
- [ ] 椰子生成 / 掉落 / 吹入池提交（开局 1 个、同时挂树 1、冷却 3s；挂点仍为 5）  
- [ ] 椅子升级：吹×3（5/12/20）+ 树×3（8/15/25，第 3 档出第二棵树）  
- [ ] 平地外墙 + 黑暗 10 秒倒计时 + 视野变暗  
- [ ] 异常 1：落地闪一次光  
- [ ] 异常 2：落地后随机方向移动一次  
- [ ] 异常 3：异常声音  
- [ ] 三纸条为 GDD 原文  
- [ ] 读 03 → 黑屏 Thank you for playing → 点击回封面  
- [ ] 封面：cover 背景、开始、离开  
- [ ] 椰子与树比例正常  

***

## 12. 建议开工顺序

A → B → C → D → E → F。

E 可与 D 并行。F 放最后，避免开始/结束流程挡住中间玩法联调。
