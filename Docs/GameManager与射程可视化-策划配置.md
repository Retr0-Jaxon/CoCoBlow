# GameManager 与射程可视化 — 策划配置说明

> 给策划/关卡同学：知道「在哪儿改、改什么、游戏里什么效果」即可。

---

## 一、在哪儿改

| 配置内容 | Unity 里选谁 |
|----------|--------------|
| 升级消耗、风力数值、换模型、椰子树、纸条 | 场景里的 **`GameManager`** |
| 吹风机初始数值、拾取姿态、出风方向 | 场景 **`HairDryer_lv1`** 或 Prefab 上的 **`HairDryer`** |
| 风锥颜色、是否常显 | **`HairDryer/Nozzle/RangeVisual`** |

**当前工作场景：** `Assets/LRB_Scene.unity`

---

## 二、GameManager 配置

### 2.1 基础引用（References）

| 字段 | 作用 |
|------|------|
| **Simple Hud** | 屏幕 HUD（椰子数量、升级提示） |
| **Simple Panel UI** | 升级面板 UI |
| **Hair Dryer** | 场景里**当前**使用的吹风机（开局一般是 `HairDryer_lv1`；升级换模后会自动指向新实例，一般不用手改） |
| **Coconut Spawner** | 椰子树生成器 |

---

### 2.2 吹风机升级表 — `Hair Dryer Upgrades`

数组**每一项 = 玩家点一次「升级吹风机」时生效的一档**。按顺序从第 0 项开始消耗。

| 字段 | 含义 | 策划怎么填 |
|------|------|------------|
| **Cost** | 本次升级消耗的椰子数量 | 越大越难升 |
| **Wind Force** | 升级后的风力 | 越大吹得越猛、树上椰子越容易掉 |
| **Wind Range** | 升级后的吹风距离（米） | 越大站越远也能吹到 |
| **Wind Angle** | 升级后的瞄准宽容度（度） | 越大锥口越宽、越好瞄准 |
| **Note Index** | 升级后解锁哪张纸条 | `0` = 第 1 张，`1` = 第 2 张…填 `-1` 表示不解锁纸条 |
| **Hair Dryer Prefab** | **可选。** 填 Prefab 则本次升级**换模型**；留空则**只改数值、不换外形** |

**当前 LRB 场景示例：**

| 档位 | Cost | Force | Range | Angle | 换模型 |
|------|------|-------|-------|-------|--------|
| 第 1 档 | 1 | 120 | 5.5 | 28° | ✓ 换成 `HairDryer_Lv2` |
| 第 2 档 | 2 | 150 | 7 | 34° | 不换 |
| 第 3 档 | 3 | 180 | 8 | 37° | 不换 |

**换模型规则（填了 Prefab 时）：**

- 吹风机在地上 → 新模型出现在**原位置**
- 玩家正拿在手里 → **手里直接变成新模型**，旧模型消失
- 换完后会自动套用**本档**的 Force / Range / Angle

**加新档位：** 在数组末尾 **+1**，填 Cost 和数值即可。Cost 填 `0` 或负数会被当成无效档。

---

### 2.3 椰子树升级表 — `Coconut Tree Upgrades`

| 字段 | 含义 |
|------|------|
| **Cost** | 消耗椰子数 |
| **Spawn Interval** | 两颗椰子之间的生成间隔（秒） |
| **Max Active Coconuts** | 树上最多同时存在几颗椰子 |

吹风机字段（Force / Range / Angle / Prefab）在本表里**不会生效**，可忽略。

---

### 2.4 纸条 — `Notes`

| 字段 | 含义 |
|------|------|
| **Note Object** | 场景里对应纸条物体（升级前通常隐藏） |
| **Content** | 玩家阅读时显示的文本 |

升级时若 **Note Index ≥ 0**，对应纸条会在场景里 **显示出来**。

---

## 三、吹风机本体 — `HairDryer`（Prefab / 场景物体）

升级改的是**运行时数值**；下面这些是**模型/Prefab 自带**的配置，换模型时要各自调好。

### 3.1 风力（Wind）— 开局默认值

| 字段 | 含义 |
|------|------|
| **Wind Force** | 初始风力（会被升级覆盖） |
| **Wind Range** | 初始射程 |
| **Wind Angle** | 初始锥角 |
| **Blow On Left Mouse** | 勾选 = 按住左键才吹风 |

### 3.2 拾取（Pickup）— 拿在手里长什么样

| 字段 | 含义 |
|------|------|
| **Pickup Local Position** | 相对相机的偏移（模型大小不同要单独调） |
| **Pickup Local Euler Angles** | 相对相机的旋转（如 LV1/LV2 常用 `Y=180`） |

### 3.3 视觉（Visuals）— 出风与风锥

| 字段 | 含义 |
|------|------|
| **Nozzle** | 枪口空物体，风从这儿发出 |
| **Wind Local Direction** | 在 Nozzle 局部坐标下的出风方向（见下表） |
| **Range Visual** | 拖 `Nozzle/RangeVisual` 上的组件 |

**Wind Local Direction 常用值：**

| 模型 | 推荐值 | 说明 |
|------|--------|------|
| HandFan | `(0, 1, 0)` | 沿 Nozzle 的 Up 出风 |
| LV1 / LV2 导入模型 | `(0, 0, 1)` | 沿 Nozzle 的 Forward 出风 |

填错会导致：**风锥方向对、但吹不到椰子**。

---

## 四、射程可视化 — `RangeVisual`

**路径：** `HairDryer` → `Nozzle` → **`RangeVisual`**

半透明**风锥**，给玩家和策划看「能吹多远、锥口多宽」。**形状自动跟随** `HairDryer` 的 **Wind Range** 和 **Wind Angle**，升级后锥体会一起变。

### 4.1 锥体对应关系

| 看到的效果 | 对应参数 |
|------------|----------|
| 锥体**长度** | = **Wind Range** |
| 锥体**开口大小** | = **Wind Angle**（越大口越宽） |
| 锥体**位置与朝向** | = 枪口 **Nozzle** + **Wind Local Direction** |

### 4.2 什么时候显示

| 情况 | 是否显示 |
|------|----------|
| 吹风机在地上 | ✓ 显示 |
| 拾取后**按住左键** | ✓ 显示 |
| 拾取后没按左键 | ✗ 淡出隐藏 |
| 勾选 **Always Show Range** | ✓ **一直显示**（调参推荐） |

### 4.3 RangeVisual 组件字段

| 字段 | 建议 | 作用 |
|------|------|------|
| **Segment Count** | 默认 24 | 锥体圆周分段，越大越圆 |
| **Fill Color** | 半透明蓝/橙 | 锥体颜色；**Alpha** 越小越不挡视线 |
| **Always Show Range** | 调参时 **勾选** | Play 里始终显示，改 Range/Angle 立刻对照 |
| **Fade Duration** | 0.25 | 停吹后淡出时间 |
| **Hair Dryer** | 拖**本吹风机**上的 HairDryer | 留空会自动找父物体；**复制物体后务必检查是否指对自己** |

---

## 五、推荐调参流程

### 调升级数值（GameManager）

1. 改 `Hair Dryer Upgrades` 里对应档位的 Cost / Force / Range / Angle
2. Play → 凑够椰子 → 点升级 → 感受手感
3. 满意后保存场景

### 调风锥与射程（RangeVisual + HairDryer）

1. 选中 `RangeVisual`，勾选 **Always Show Range**
2. Play，选中当前 `HairDryer`
3. 拖 **Wind Range / Wind Angle**，看锥体能否罩住树上椰子
4. 退出 Play，在 Edit 模式写回数值并保存

### 做新等级模型（如 LV3）

1. 复制现有 Prefab（如 `HairDryer_Lv2`），换模型 mesh
2. 调好 **Pickup**、**Wind Local Direction**、**Nozzle**、**RangeVisual.hairDryer 引用**
3. 在升级表某一档的 **Hair Dryer Prefab** 里拖入新 Prefab
4. 场景里**只放开局那一档**的实例，更高等级仅作 Prefab 资产

---

## 六、常见问题

| 现象 | 可能原因 | 处理 |
|------|----------|------|
| 升级后还是旧模型 | 该档 **Hair Dryer Prefab** 为空 | 填对应 Prefab |
| 风锥不在枪口上 | RangeVisual 的 **Hair Dryer** 指错了物体 | 改指向本吹风机 |
| 有锥体但吹不到椰子 | **Wind Local Direction** 配错 | 对照第三节表格改 |
| 升级没反应 | 椰子不够 **Cost** | 先提交椰子再升级 |
| 手持升级后握姿很奇怪 | 新 Prefab 的 **Pickup Local** 未单独调 | 在 Prefab 里调拾取偏移/旋转 |

---

## 七、相关 Prefab 路径

| 资产 | 路径 |
|------|------|
| LV2 吹风机 | `Assets/Prefabs/HairDryer_Lv2.prefab` |
| 风锥材质 | `Assets/Materials/HairDryerRange.mat` |
| LV1 模型 | `Assets/Models/blower/blower_01s.fbx` |
| LV2 模型 | `Assets/Models/blower/blower_02l.fbx` |
