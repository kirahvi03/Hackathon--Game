# 贴图资源与替换方法

游戏里所有非白盒贴图都放在 `MyGame/Assets/_Project/Resources/Sprites/` 下，用 `Resources.Load` 按文件名加载，
**文件名就是 id**，换图不需要改代码，覆盖同名 png 即可。

```
Resources/
├── Portraits/          # 对话框里的人物立绘（researcher.png / guard.png）
└── Sprites/
    ├── octopus.png          # 章鱼平时的俯视图
    ├── octopus_attack.png   # 章鱼按 F 时的攻击差分图
    ├── researcher.png       # 研究员俯视图
    ├── guard.png            # 武装人员俯视图
    ├── floor_lab.png        # 场景背景（整张地面）
    └── Props/               # 场景杂物
        ├── rig.png       # 大机台
        ├── console.png   # 控制台
        ├── rack.png      # 机架
        ├── cabinet.png   # 储物柜
        └── cart.png      # 推车
```

## 各资源的规格

| id | 尺寸 | 用途 | 世界尺寸由谁决定 |
| --- | --- | --- | --- |
| `octopus` | 512×512 RGBA | 主角平时 | `WhiteboxBootstrap.playerSpriteHeight = 1.6`（按高度） |
| `octopus_attack` | 512×512 RGBA | 按 F 时临时替换 | 同主角，跟着同一个 SpriteRenderer |
| `researcher` / `guard` | 512×512 RGBA | 两个 NPC | `characterSpriteHeight = 1.52`（按高度，= 原 1.9 的 80%） |
| `floor_lab` | 1024×1024 | 背景地面 | `backgroundPlateWidth = 9.6`（按宽度等比） |
| `Props/*` | 512×512 RGBA | 场景杂物 | 小件 `smallPropWidth = 0.78`、大件 `largePropWidth = 1.7`（按宽度等比） |

所有贴图在运行时都被强制成 `FilterMode.Point` + `WrapMode.Clamp`，保持像素边缘锐利、透明边缘不采样到画布另一侧。

## 攻击差分图（按 F）

`PlayerController` 在 `Awake` 里加载 `octopus_attack`，`Strike()` 时把 `SpriteRenderer.sprite` 换成它，
保持 `attackSpriteHoldSeconds`（默认 **1 秒**）后由 `LateUpdate` 切回平时的 `octopus`。

- 找不到攻击图时自动退回旧的表现：把颜色闪成橙黄（`Color(1, 0.85, 0.3)`）。
- 翻转由 `SpriteFacing` 控制 `flipX`，两张图共用同一个方向逻辑。
- 想改持续时间：Inspector 里改 `PlayerController.attackSpriteHoldSeconds`，或改代码默认值。

## 背景地面

`WhiteboxBootstrap.BuildFloor()` 有两种模式：

1. **`useBackgroundPlate = true`（默认）**：加载 `floor_lab`，生成一个 `Floor/Backdrop` 精灵，`sortingOrder = -1000`，
   按 `backgroundPlateWidth` 等比缩放到盖住整个场地（场地约 8.5 × 4.5 世界单位，所以 9.6 足够）。
2. **`useBackgroundPlate = false` 或图片缺失**：回退成 16×16 的菱形瓦片（每块 1×0.5，间距 0.5，棋盘两色）。

## 杂物

`ScatterContent()` 生成 10 个小件（`BodyClass.Small`）和 3 个大件（`BodyClass.Large`）。
`WhiteboxBootstrap.SpawnBody()` 会调 `PropSprites.Fit()`：

- 大件从 `PropSprites.LargeIds = { rig, rack, cart }` 里挑，小件从 `SmallIds = { console, cabinet, cart }` 里挑；
- 挑哪一张由物体坐标算出的 hash 决定（`Mathf.Abs(RoundToInt(x*73 + y*137))`），所以同一个种子每次布局一致、又能有变化；
- 贴图缺失时回退成带颜色的白盒方块（颜色仍用作碎片颜色 `debrisColor`）。

## 图片是怎么做出来的

原始美术是"黑底 + 奶油色 1-bit 线稿"的方图，没有 alpha 通道。处理管线（Python + Pillow）：

1. 亮度 `lum = max(R,G,B)`；
2. 缩到 1024 做形态学：`coarse = box_mean(lum, 17) > 70`、`fine = lum > 100`、`box_mean(fine & coarse, 3) > 0.34`；
3. `fill_holes(min_area=300)` 把闭合区域填实（黑衣服/黑裤子与黑背景同色，只能靠"被包围"来判定是主体）；
4. 人类角色额外做桥接（膨胀 51 → 闭运算 → 与 near(41) 相交 → 腐蚀 7），把四肢之间的小空隙闭起来；
5. `MinFilter(3)` + `GaussianBlur(0.8)` 收边，放大回原尺寸，裁 bbox，垫成正方形后 LANCZOS 缩到 512。

杂物是按图上的空白带切出来的（列空白带 x≈834 / x≈1352，行空白带 y≈480–599 / y≈500–607），
每块按亮度做 alpha，黑底即透明。

## 加一件新杂物

1. 把 png 放进 `Resources/Sprites/Props/`，文件名用英文 id；
2. 在 `PropSprites.AllIds` 里加上它，并按大小放进 `LargeIds` 或 `SmallIds`；
3. 跑一次 `Hackathon → 白盒冒烟测试`，确认"杂物贴图可加载"全绿。
