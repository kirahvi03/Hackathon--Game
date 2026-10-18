# 白盒验证场景（Whitebox）

> 目标：在**没有任何美术资产**的情况下，先把核心循环跑通并可试玩。
> 对应里程碑里的 **25%（能跑起来的白盒）→ 50%（可玩闭环）**。

---

## 1. 怎么跑起来

场景完全由代码生成，不需要手工连线：

1. Unity Hub 打开 `MyGame`（**确认只有一个 Unity 实例**）
2. 菜单 `File → New Scene`，选 `Basic (Built-in)`，保存为 `Assets/_Project/Scenes/Whitebox.unity`
3. 层级面板右键 → `Create Empty`，命名 `Boot`
4. 把 `Assets/_Project/Scripts/Gameplay/WhiteboxBootstrap.cs` 拖到 `Boot` 上
5. 按 **Play**

> 之后再把 `Boot` 对象和场景提交进仓库，别人拉下来直接 Play 就行。

---

## 2. 操作

| 按键 | 行为 |
| --- | --- |
| `W` `A` `S` `D` | 按**屏幕方向**移动（等轴测下自动换算成斜向） |
| `F` | 触手扑击：自身前冲 + 判定身前圆形范围 |
| `E` | 交互/对话：靠近角色按 E 打开对话框，再按 E 逐句推进，`Esc` 关闭 |

---

## 3. 代码结构与职责

| 文件 | 职责 |
| --- | --- |
| `IsoUtils.cs` | 等轴测数学：输入方向 → 世界方向、世界 → 屏幕、按 Y 算排序层 |
| `IDamageable.cs` | `Hit` 结构 + `IDamageable` 接口（攻击只认接口，方便扩展） |
| `PlayerController.cs` | 等轴测移动 + F 扑击（含惯性、冷却、判定、闪光反馈） |
| `Destructible.cs` | 可破坏物/敌人：按 `BodyClass` 分派 小物件碎 / 大物件变形 / 生物受伤 |
| `SortByY.cs` | 等轴测前后遮挡排序（挂到会动的物体上） |
| `WhiteboxSprites.cs` | 运行时生成白色方块与 2:1 菱形精灵，**不依赖任何美术资产** |
| `Interactable.cs` | 可交谈角色：名字、逐句台词、立绘、对话结束后的效果 |
| `PlayerInteractor.cs` | 每帧找最近的可交互角色，按 E 触发对话 |
| `DialogueSystem.cs` | 对话框与立绘渲染（IMGUI）、逐句推进、结束结算 |
| `PlayerVitals.cs` | 主角生命与 pH |
| `HudOverlay.cs` | 左上角 HP / pH 状态条与「按 E 交谈」提示 |
| `MuzzleFlash.cs` | 武装人员开枪的枪口闪光与弹道 |
| `UiFont.cs` | IMGUI 中文字体（内置 Arial 无中文字形，会显示方框） |
| `WhiteboxBootstrap.cs` | 搭场景：相机、地面、玩家、散布箱子/机器/生物/研究员/武装人员 |

### 关键设计点

- **视觉放在子物体 `Visual` 上**：这样"变形"只缩放视觉，不改变碰撞体体积，
  避免大物件被打变形后碰撞行为跟着变。
- **等轴测靠美术与排布，不靠转相机**：相机的 `rotation` 保持单位四元数，
  斜 45° 观感来自菱形地面与精灵本身。
- **地面菱形间距取 0.5 世界单位**：菱形精灵正好 1 宽 0.5 高，取一半间距即可无缝拼满。
- **按 Y 排序**：`sortingOrder = -round(y * 100)`，Y 越小越靠前越晚绘制。

---

## 4. 验收标准（按这个检查是否算"可信的白盒"）

- [ ] WASD 按下去，章鱼在**屏幕上**朝着按键方向走（不是世界轴向）
- [ ] 按 F 会前冲一段并自然滑停（有惯性感，不是瞬移）
- [ ] 小箱子被打中 → 碎裂成若干碎片并飞散
- [ ] 大机器被打中 → **变形但不立即碎**，打若干次后才碎
- [ ] 生物被打中 → 闪红、被击退、血量归零后消失
- [ ] 走近研究员，屏幕下方出现「按 E 与「研究员」交谈」
- [ ] 按 E 弹出对话框：左侧是研究员立绘，右侧四句台词逐句推进
- [ ] 走近武装人员按 E → 三句之后直接开枪（有弹道闪光），HP 与 pH 各掉一截
- [ ] 章鱼走到箱子**后面**时被箱子挡住、走到前面时盖住箱子（排序正确）
- [ ] 所有碎片会自动消失（不会无限堆积）

最后一条最容易漏：碎片有 `debrisLifetime`，超时自动 `Destroy`。

---

## 5. 可调参数

在 `Boot` 对象的 `WhiteboxBootstrap` 组件上：

| 参数 | 含义 |
| --- | --- |
| `tilesX` / `tilesY` | 地面瓦片数（默认 16×16） |
| `smallCrates` / `largeMachines` / `creatures` | 各类物体数量 |
| `spawnExtent` | 散布范围 |
| `randomSeed` | 固定种子，保证每次布局一致，便于对比 |

在 `Player` 对象的 `PlayerController` 组件上：

| 参数 | 手感影响 |
| --- | --- |
| `moveSpeed` / `acceleration` | 移动快慢与跟手程度 |
| `drag` | **越小越滑**，惯性感越强 |
| `lungeImpulse` | 扑击冲得多远 |
| `lungeCooldown` | 连击频率 |
| `strikeRadius` / `strikeReach` | 触手攻击范围（`Reach` 是判定圆往前的距离） |

---

## 6. 下一步（白盒之后的自然延伸）

1. **触手可视化**：现在扑击只有颜色闪烁，应加一条会伸长/收回的触手精灵
2. **敌人 AI**：目前生物只是靶子，需要"武装人员追击"（直线追击即可，别上寻路）
3. **耐久的可视化**：大物件被打变形目前只压缩视觉，应加裂纹/凹陷贴图
4. **Dither 后处理**：Built-in 的 `OnRenderImage` + Bayer 矩阵
5. **音效**：破坏/扑击的反馈音，成本低但手感提升明显
6. **等轴测瓦片关卡**：用 `com.unity.2d.tilemap.extras` 的等轴测瓦片替换代码生成的地面

---

## 7. 俯视贴图（章鱼 / 研究员 / 武装人员）

白盒方块已经被替换成真正的俯视像素贴图。

### 资源位置

`MyGame/Assets/_Project/Resources/Sprites/`

| 文件 | 用途 | 内容尺寸 |
| --- | --- | --- |
| `octopus.png` | 主角（章鱼） | 1350x1204 |
| `researcher.png` | 研究员 | 920x1310 |
| `guard.png` | 武装人员 | 1194x1296 |

三张都是 512x512 RGBA，透明背景，四周留了约 2% 的呼吸边。
导入设置由编辑器自动生成（`textureType: 8` Sprite、`filterMode: 1` Bilinear、`spritePixelsToUnits: 100`），
脚本加载后会把 filterMode 强制改成 Point，保证像素颗粒不被插值糊掉。

### 代码怎么接的

- `Gameplay/ActorSprites.cs`：`Load(id)` 走 `Resources.Load<Sprite>("Sprites/" + id)`，
  带缓存、带失败警告；`Fit(renderer, sprite, targetHeight)` 按**世界单位高度**缩放，
  不依赖 PPU，换图换分辨率都不用改代码
- `Gameplay/SpriteFacing.cs`：按 `Rigidbody2D.velocity.x` 翻转 `flipX`，
  所以向左跑会自然镜像
- `WhiteboxBootstrap`：`playerSpriteHeight = 1.6`、`characterSpriteHeight = 1.52`
  （人类角色按 1.9 的 80% 缩过，和章鱼站一起比例更合理），想做大小怪就调这两个数；
  贴图加载失败会静默回退到白盒方块（不会崩）
- **旋转**：章鱼的刚体 `freezeRotation = true`（Bootstrap 与 PlayerController 双保险），
  碰撞和扑击冲量都不会把身体拧歪；唯一的"转向"是 `SpriteFacing` 按速度做的左右翻转。
  研究员/武装人员同样锁了旋转，被扑击只被推开、不转圈

### 换美术的流程

1. 把新图丢进 `Resources/Sprites/`，文件名保持不变就能直接生效
2. 图要用**透明背景 PNG**，主体居中，下方留一点脚底空白
3. 不要改 PPU —— `Fit()` 只看 `sprite.bounds`，PPU 怎么设都行