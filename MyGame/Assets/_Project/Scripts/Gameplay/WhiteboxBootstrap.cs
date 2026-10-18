using UnityEngine;

namespace Hackathon.Gameplay
{
    /// <summary>
    /// 白盒场景生成器 —— 整个验证场景由代码搭出来，不依赖任何美术资产或手工连线。
    ///
    /// 用法：
    ///   1. Unity 里新建/打开一个空场景
    ///   2. 新建空物体（GameObject → Create Empty），命名 Boot
    ///   3. 把本脚本拖到 Boot 上
    ///   4. 按 Play
    ///
    /// 操作：WASD 移动（屏幕方向）、F 触手扑击。
    /// </summary>
    public class WhiteboxBootstrap : MonoBehaviour
    {
        [Header("地面")]
        public int tilesX = 16;
        public int tilesY = 16;
        public Color floorColorA = new Color(0.24f, 0.26f, 0.30f);
        public Color floorColorB = new Color(0.20f, 0.22f, 0.26f);
        public Color backgroundColor = new Color(0.07f, 0.08f, 0.10f);

        [Header("相机")]
        public float orthographicSize = 5f;

        [Header("场景内容")]
        public int smallCrates = 10;
        public int largeMachines = 3;
        public int researchers = 1;
        public int guards = 2;
        public int randomSeed = 20261014;

        [Header("摆放范围（世界单位，等轴测平面内）")]
        public float spawnExtent = 3f;

        [Header("角色贴图尺寸（世界单位高度）")]
        public float playerSpriteHeight = 1.6f;
        public float characterSpriteHeight = 1.9f;

        GameObject _floorRoot;
        bool _built;
        Transform _player;

        void Awake()
        {
            // 防止重复构造（编辑器手动调用 / 热重载）
            if (_built) return;
            _built = true;
            Random.InitState(randomSeed);

            // 对话系统与 HUD 直接挂在 Boot 上，场景里不需要额外对象或 prefab
            DialogueSystem dialogue = gameObject.AddComponent<DialogueSystem>();
            gameObject.AddComponent<HudOverlay>();

            BuildCamera();
            BuildFloor();
            _player = BuildPlayer();
            ScatterContent();

            // 对话结束后的扣血 / pH 变化需要主角状态
            var vitals = _player != null ? _player.GetComponent<PlayerVitals>() : null;
            if (vitals != null) dialogue.Bind(vitals);
        }

        // ---------------------------------------------------------------- 相机

        void BuildCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = backgroundColor;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.rotation = Quaternion.identity;
        }

        // ---------------------------------------------------------------- 地面

        void BuildFloor()
        {
            _floorRoot = new GameObject("Floor");

            // 菱形精灵正好 1 宽 0.5 高，因此世界间距取 0.5 就能无缝拼满
            const float step = 0.5f;
            for (int i = 0; i < tilesX; i++)
            {
                for (int j = 0; j < tilesY; j++)
                {
                    var tile = new GameObject(string.Format("Tile_{0}_{1}", i, j));
                    tile.transform.SetParent(_floorRoot.transform, false);
                    tile.transform.position = new Vector3(i * step, -j * step, 0f);

                    var sr = tile.AddComponent<SpriteRenderer>();
                    sr.sprite = WhiteboxSprites.Diamond();
                    sr.color = ((i + j) % 2 == 0) ? floorColorA : floorColorB;
                    sr.sortingOrder = -1000; // 永远在最底层
                }
            }

            // 把整块地面挪到原点居中，方便相机取景
            _floorRoot.transform.position = new Vector3(
                -(tilesX - 1) * step * 0.5f,
                (tilesY - 1) * step * 0.5f,
                0f);
        }

        // ---------------------------------------------------------------- 玩家

        Transform BuildPlayer()
        {
            var go = new GameObject("Player");
            go.transform.position = Vector3.zero;

            // 视觉放在子物体上：这样变形/缩放不影响碰撞体
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

            // 章鱼俯视贴图；加载失败才退回白盒方块
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.color = Color.white;
            if (!ActorSprites.Fit(sr, ActorSprites.Load(ActorSprites.OctopusId), playerSpriteHeight))
            {
                sr.sprite = WhiteboxSprites.Square;
                sr.color = new Color(0.85f, 0.35f, 0.55f);
                visual.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
            }

            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.mass = 1.2f;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.26f;

            var facing = go.AddComponent<SpriteFacing>();
            facing.target = sr;
            facing.body = body;

            go.AddComponent<SortByY>();
            go.AddComponent<PlayerController>();
            go.AddComponent<PlayerVitals>();
            go.AddComponent<PlayerInteractor>();

            return go.transform;
        }

        // ---------------------------------------------------------------- 内容

        void ScatterContent()
        {
            var root = new GameObject("Content");

            for (int i = 0; i < smallCrates; i++)
            {
                SpawnBody(root.transform, "Crate",
                    RandomPointOnFloor(), new Color(0.78f, 0.6f, 0.35f),
                    new Vector2(0.34f, 0.34f), BodyClass.Small, 1f, 0.35f);
            }

            for (int i = 0; i < largeMachines; i++)
            {
                SpawnBody(root.transform, "Machine",
                    RandomPointOnFloor(1.8f), new Color(0.42f, 0.5f, 0.62f),
                    new Vector2(0.9f, 0.7f), BodyClass.Large, 3f, 0f);
            }

            for (int i = 0; i < researchers; i++)
            {
                SpawnCharacter(root.transform, CharacterKind.Researcher,
                    RandomPointOnFloor(1.6f), new Color(0.86f, 0.84f, 0.74f));
            }

            for (int i = 0; i < guards; i++)
            {
                SpawnCharacter(root.transform, CharacterKind.Guard,
                    RandomPointOnFloor(1.6f), new Color(0.30f, 0.33f, 0.38f));
            }
        }

        GameObject SpawnBody(
            Transform parent, string name, Vector2 position, Color color,
            Vector2 size, BodyClass bodyClass, float integrity, float knockback)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(size.x, size.y, 1f);

            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = WhiteboxSprites.Square;
            sr.color = color;

            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.mass = bodyClass == BodyClass.Large ? 4f : 1f;
            body.drag = bodyClass == BodyClass.Creature ? 6f : 4f;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.9f, 0.9f);

            go.AddComponent<SortByY>();

            var target = go.AddComponent<Destructible>();
            target.bodyClass = bodyClass;
            target.integrity = integrity;
            target.knockbackScale = knockback;
            target.debrisColor = color;
            target.debrisCount = bodyClass == BodyClass.Large ? 6 : 5;

            return go;
        }

        // ---------------------------------------------------------------- 可交谈的角色

        Sprite LoadPortrait(string resourceName)
        {
            var sprite = Resources.Load<Sprite>("Portraits/" + resourceName);
            if (sprite == null)
            {
                Debug.LogWarning("[Whitebox] 立绘未找到: Resources/Portraits/" + resourceName);
            }
            return sprite;
        }

        /// <summary>研究员会尝试交流；武装人员只丢三句，然后开枪。</summary>
        static string[] LinesFor(CharacterKind kind)
        {
            switch (kind)
            {
                case CharacterKind.Guard:
                    return new[]
                    {
                        "……",
                        "站住。把触手举起来。",
                        "我不跟标本谈判。"
                    };
                default:
                    return new[]
                    {
                        "研究日志 #114：受试体展现出计划之外的工具使用能力。",
                        "……你能听懂我说话，对吗？那就先把触手放下。",
                        "它在模仿我的动作。而且它一直在看着我。",
                        "如果你真的有意识——我需要你证明给我看。"
                    };
            }
        }

        GameObject SpawnCharacter(Transform parent, CharacterKind kind, Vector2 position, Color bodyColor)
        {
            bool isGuard = kind == CharacterKind.Guard;

            var go = new GameObject(isGuard ? "Guard" : "Researcher");
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

            // 研究员 / 武装人员的俯视贴图；加载失败才退回白盒方块
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.color = Color.white;
            Sprite actorSprite = ActorSprites.Load(isGuard ? ActorSprites.GuardId : ActorSprites.ResearcherId);
            if (!ActorSprites.Fit(sr, actorSprite, characterSpriteHeight))
            {
                sr.sprite = WhiteboxSprites.Square;
                sr.color = bodyColor;
                visual.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            }

            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.mass = 1.2f;
            body.drag = 6f;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.3f;

            go.AddComponent<SortByY>();

            var target = go.AddComponent<Destructible>();
            target.bodyClass = BodyClass.Creature;
            target.integrity = isGuard ? 3f : 2f;
            target.knockbackScale = isGuard ? 0.3f : 0.6f;
            target.debrisColor = bodyColor;
            target.debrisCount = 5;

            var interactable = go.AddComponent<Interactable>();
            interactable.kind = kind;
            interactable.displayName = isGuard ? "武装人员" : "研究员";
            interactable.lines = LinesFor(kind);
            interactable.portrait = LoadPortrait(isGuard ? "guard" : "researcher");
            interactable.singleUse = true;

            // 武装人员：谈完就开枪 —— 轻微受伤 + pH 向酸性偏移
            interactable.shootsOnEnd = isGuard;
            interactable.healthDamageOnEnd = isGuard ? 8f : 0f;
            interactable.phDeltaOnEnd = isGuard ? -0.35f : 0f;

            return go;
        }

        Vector2 RandomPointOnFloor(float minDistanceFromCenter = 0.6f)
        {
            for (int attempt = 0; attempt < 12; attempt++)
            {
                var p = new Vector2(
                    Random.Range(-spawnExtent, spawnExtent),
                    Random.Range(-spawnExtent, spawnExtent));
                if (p.magnitude >= minDistanceFromCenter) return p;
            }
            return new Vector2(spawnExtent * 0.8f, spawnExtent * 0.8f);
        }

        // ---------------------------------------------------------------- 提示

        void OnGUI()
        {
            var uiFont = UiFont.Get();
            if (uiFont != null) GUI.skin.font = uiFont;
            // 状态条与操作提示由 HudOverlay 绘制，这里只放本场景的验证目标
            var style = new GUIStyle(GUI.skin.label);
            style.fontSize = 14;
            style.alignment = TextAnchor.UpperRight;
            style.normal.textColor = new Color(0.72f, 0.72f, 0.68f, 0.9f);
            GUI.Label(new Rect(Screen.width - 640f, 14f, 620f, 22f),
                "白盒验证：小物件碎 / 大机器变形 / 生物受伤后退 / 对角色按 E 对话", style);
        }
    }
}
