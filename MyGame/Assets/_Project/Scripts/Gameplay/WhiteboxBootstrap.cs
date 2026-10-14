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
        public int creatures = 2;
        public int randomSeed = 20261014;

        [Header("摆放范围（世界单位，等轴测平面内）")]
        public float spawnExtent = 3f;

        GameObject _floorRoot;
        Transform _player;

        void Awake()
        {
            Random.InitState(randomSeed);

            BuildCamera();
            BuildFloor();
            _player = BuildPlayer();
            ScatterContent();
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

            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = WhiteboxSprites.Square;
            sr.color = new Color(0.85f, 0.35f, 0.55f);

            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.mass = 1.2f;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.26f;

            go.AddComponent<SortByY>();
            go.AddComponent<PlayerController>();

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

            for (int i = 0; i < creatures; i++)
            {
                SpawnBody(root.transform, "Creature",
                    RandomPointOnFloor(2.2f), new Color(0.55f, 0.82f, 0.45f),
                    new Vector2(0.45f, 0.45f), BodyClass.Creature, 2f, 0.6f);
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
            GUI.Label(new Rect(12f, 10f, 560f, 22f),
                "WASD 移动（屏幕方向）　F 触手扑击　—— 白盒验证场景");
            GUI.Label(new Rect(12f, 30f, 560f, 22f),
                "目标：小物件一碰就碎 / 大机器被击变形 / 生物受伤后退，检查等轴测前后遮挡是否正确");
        }
    }
}
