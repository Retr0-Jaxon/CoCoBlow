# CocoBlow MVP 闭环 — 已落地对照

> 对照文档：`Game_Design_Document.md` v0.5  
> 工作场景：`Assets/Scenes/LRB_Scene.unity`  
> 日期：2026-08-14  
> 状态：**已按当前仓库实现落地**（相对 2026-08-13 开工计划有若干联调修订）

本文件记录「计划要做 / 实际做成了什么」，作为 `dev` → `main` 的变更摘要。

**本轮明确不做（仍成立）：**

- 去掉跳跃（保留空格跳跃）
- 「按 E 交互」提示文案
- 改挂点数量（保持现有 5 个）
- 科技树「每解锁一项点亮一颗椰子」
- 纸条解密、运输、种植、结局插画（GDD 有空再做）
- 重写吹风机为「半径米」物理（继续用现有锥角 `windAngle`）

***

## 1. Demo 流程（当前实现）

```
封面 cover.png（「开始」/「离开」）
  → timeScale=0，不锁鼠标，不处理移动/吹风，不播氛围乐
  → 点「开始」：持吹风机、播 atmosphere、进入第一人称
  → 1 棵树、开局挂树上限 2、现有 5 挂点、冷却 3 秒
  → 吹落（阈值 45）→ 吹进池 → 椅子升级
  → 吹强化 1/2/3：异常 1/2/3 + 解锁纸条 01/02/03
  → 树强化 1：每棵挂树上限 4（冷却仍 3s）
  → 树强化 2：冷却 1.5s（上限仍 4）
  → 树强化 3：再长出第 2 棵树（每棵上限 4）
  → 进黑暗区：纯黑遮罩 alpha 0.7→1 + 10 秒，超时回出生点
  → 读纸条后世界物体消失；读 03 → 黑屏 Thank you for playing
  → 点击任意处 LoadScene 回封面（新一局）
```

点「离开」：打包版 `Application.Quit()`；编辑器停止 Play。

***

## 2. 相对原开工计划的修订

| 原计划 | 当前实现 |
|--------|----------|
| 开局挂树上限 1 | **2**（只统计 `IsAttached`） |
| 树强化 1 = 冷却 1.5s | 树强化 **1 = 上限 4**（冷却仍 3s） |
| 树强化 2 = 上限 2 | 树强化 **2 = 冷却 1.5s**（上限仍 4） |
| 异常 2 = 落地随机水平冲一次 | 落地等一帧物理后**朝玩家水平冲**（速度 6.5，清竖直速度，避免弹跳盖掉冲量） |
| 升级消耗改回 GDD 5/12/20、8/15/25 | **脚本默认仍是 GDD 值**；**场景 `LRB_Scene` 当前为联调测试消耗 1/2/3** |
| 黑暗遮罩半透明黑图 | 1×1 白图染色纯黑，alpha **0.7→1**，挂在 Canvas 全屏 |
| 读完纸条只关面板 | 关面板后 **纸条物体 `SetActive(false)` 消失** |
| 封面可接受氛围乐被 timeScale 压住 | 封面主动 `StopAudio("atmosphere")`，**点开始才播** |
| 吹落阈值按旧值（约 120） | **45**；摇晃频率脚本默认 10 |
| `AudioManager` 不要 `DontDestroyOnLoad` | **未改**：仍 `DontDestroyOnLoad`。重载场景靠停 loop 音 + 封面不播 BGM 规避重复感 |

进池 Trigger 曾因顶面埋在地下不加分，已把 `CoconutSubmitPool` 的 BoxCollider 调到地面以上。

***

## 3. 批次落地说明

场景数值尽量在 Unity 里保存，少手改 `LRB_Scene.unity` YAML（YAML 改完再 `SaveScene` 会把升级表打回旧值）。

### A — 椰子 / 树比例

- `Coconut.AttachToTree()` 按父物体 `lossyScale` 补偿 `localScale`，掉落后世界大小与树上一致。
- 树实例 Scale **0.2**。
- `TinyCoconut` Scale ≈ **0.306**（玩家 CharacterController 高 2，约 1/3 人高）。
- 挂点仍为 5，未改生成逻辑。

### B — 开局手持吹风机

- 点「开始」时 `GameFlowController` 对当前 `HairDryer` 调用 `PickUp(cameraRoot)`。
- 保留 E 拾取、G 放下、空格跳跃。
- 封面 / 结局期间 `IsBlockingGameplay`：不锁鼠标、不处理移动、吹风机不吹。

### C — 生产 + 升级 + 第二棵树

- `maxActiveCoconuts` **只统计挂在树上的**；地上未进池不占名额。
- 开局：`spawnInterval = 3`，`maxActiveCoconuts = 2`。
- 树强化 3：在 `secondTreeAnchor` `Instantiate` 第二棵树，拷贝间隔 / 上限 / 预制体；两棵独立生成。
- 吹风机第 1 档可换 `HairDryer_Lv2` 模型。
- 风力继续用锥角 `windAngle`，不重写半径米检测。

**吹风机升级（效果以场景为准；消耗见下表备注）：**

| 档 | 场景消耗 | 脚本默认消耗 | 风力 | 范围 | 锥角 |
|----|----------|--------------|------|------|------|
| 初始 | 0 | 0 | 100 | 5 | 17° |
| 强化 1 | 1 | 5 | 120 | 5.5 | 20° |
| 强化 2 | 2 | 12 | 150 | 7 | 19° |
| 强化 3 | 3 | 20 | 180 | 8 | 21° |

**椰子树升级：**

| 档 | 场景消耗 | 脚本默认消耗 | 效果 |
|----|----------|--------------|------|
| 强化 1 | 1 | 8 | 冷却仍 3s，每棵挂树上限 **4** |
| 强化 2 | 2 | 15 | 冷却 **1.5s**，上限仍 4 |
| 强化 3 | 3 | 25 | 再实例化 1 棵同样的树（沿用 5 挂点） |

发版前若要按 GDD 主线约 37 椰子通关，把场景消耗改回脚本默认即可，不必改代码。

### D — 三异常 + 三纸条

新建 `AnomalyController`，接到 `OnHairDryerUpgradeEvent`。异常只加不减。

| 顺序 | 触发 | 当前表现 |
|------|------|----------|
| 异常 1 | 吹强化 1 | 落地闪 Point Light，约 0.3s |
| 异常 2 | 吹强化 2 | 落地 `WaitForFixedUpdate` 后朝玩家水平 `velocity = dir * 6.5`，清角速度 |
| 异常 3 | 吹强化 3 | `AudioManager.PlayAudio("anomaly")` |

落地判定：离树后第一次地面碰撞（法线偏上），每颗椰子只触发一次。

纸条为 GDD 原文：

| 编号 | 文案 |
|------|------|
| 01 | 这椰子要是能吃就好了 |
| 02 | 这里好像没有出口，怎么办 |
| 03 | 又回到这里了，这些该死的椰子。 |

读完关闭后面板物体消失。读完 03 进结局。

### E — 黑暗区视野变暗

墙、10 秒倒计时、回安全区清零、超时传到 `PlayerRespawnPoint`、不扣椰子——沿用已有规则。

新增：`SimpleHUD` 全屏纯黑 overlay，alpha 随剩余时间从 0.7 升到 1。不改倒计时数字与传送。与 `KeepInsideArea` 无关。

### F — 开始界面 + 结束黑屏回标题

全程留在 `LRB_Scene`，用 `SceneManager.LoadScene` 复位。

- 封面：全屏 `cover.png`，「开始」「离开」（中文 TMP）。
- 点开始才 `PickUp` + `PlayAudio("atmosphere")`。
- 结局：纯黑全屏、居中 `Thank you for playing`、隐藏关闭按钮、整块可点；`timeScale = 0`，不允许 Esc 关掉后继续玩。
- Build Settings：`LRB_Scene` 为第一个启用场景；`SampleScene` 已禁用。

***

## 4. 文件总表

| 文件 | 批次 | 动作 |
|------|------|------|
| `Assets/Scripts/Coconut/Coconut.cs` | A, D | 挂树缩放补偿；落地闪光；朝玩家冲 |
| `Assets/Prefabs/TinyCoconut.prefab` | A | Scale ≈ 0.306 |
| `Assets/Scripts/FirstPersonController.cs` | B, F | 菜单时不锁鼠标、不处理移动 |
| `Assets/Scripts/HairDryer.cs` | B, F | 菜单/结局期间不吹 |
| `Assets/Scripts/Coconut/CoconutSpawner.cs` | C | 间隔/上限；只统计挂树数量 |
| `Assets/Managers/GameManager.cs` | C, D, F | 升级表、第二棵树、异常钩子、读完纸条消失、结局入口 |
| `Assets/Scripts/UI/UpgradeNodeButton.cs` | — | **本轮不改** |
| `Assets/Scripts/Environment/AnomalyController.cs` | D | 异常 1/2/3 开关与触发 |
| `Assets/Scripts/Environment/DarkZone.cs` | E | 通知 HUD 变暗 |
| `Assets/Scripts/UI/SimpleHUD.cs` | E | 全屏纯黑 overlay |
| `Assets/Scripts/UI/GameFlowController.cs` | F | 开始、暂停、退出、持吹风机、重载 |
| `Assets/Scripts/UI/SimplePanelUI.cs` | F | 结局黑屏、点击回标题 |
| `Assets/Scenes/LRB_Scene.unity` | 全部 | 数值、UI、锚点、纸条、进池 Trigger |
| `ProjectSettings/EditorBuildSettings.asset` | F | 优先启用 `LRB_Scene` |
| `AudioManager` 场景/预制体 | D | 注册 `anomaly` 音效 |

未纳入本轮提交：`.vsconfig`、未使用的 `Assets/Models/glow coconut.fbx`。

***

## 5. 验收总清单（相对 GDD MVP 必做）

- [x] 移动、视角、按住左键吹风（保留跳跃）
- [x] 开局手持吹风机（点开始后）
- [x] 椰子生成 / 掉落 / 吹入池提交（开局挂树上限 2、冷却 3s；挂点仍为 5；进池 Trigger 已抬到地面以上）
- [x] 椅子升级：吹×3 + 树×3（第 3 档出第二棵树；**场景消耗现为测试值 1/2/3**，脚本默认仍为 5/12/20 与 8/15/25）
- [x] 平地外墙 + 黑暗 10 秒倒计时 + 视野变暗（纯黑 0.7→1）
- [x] 异常 1：落地闪一次光
- [x] 异常 2：落地后朝玩家水平冲一次
- [x] 异常 3：异常声音
- [x] 三纸条为 GDD 原文；读完后物体消失
- [x] 读 03 → 黑屏 Thank you for playing → 点击回封面
- [x] 封面：cover 背景、开始、离开；封面不播氛围乐
- [x] 椰子与树比例正常（树 0.2，椰子 ≈0.306）

***

## 6. 发版前可选收尾（非本轮阻塞）

- 把场景升级消耗从测试值 1/2/3 改回 GDD 5/12/20、8/15/25。
- 若重载场景出现重复 `AudioManager`，再去掉 `DontDestroyOnLoad`。
- 科技树节点点亮、纸条解密 / 运输 / 种植 / 结局插画：明确不做。
