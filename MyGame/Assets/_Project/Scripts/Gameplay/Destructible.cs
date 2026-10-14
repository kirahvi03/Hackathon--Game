using System.Collections;
using UnityEngine;

namespace Hackathon.Gameplay
{
    /// <summary>物体分类，决定被击中时的表现。</summary>
    public enum BodyClass
    {
        /// <summary>小物件：一碰就碎（箱子、瓶子、玻璃）</summary>
        Small,

        /// <summary>大物件：会变形，打到耐久耗尽才碎（机器、柜子）</summary>
        Large,

        /// <summary>生物：受伤、被击退，血量归零才倒下</summary>
        Creature
    }

    /// <summary>
    /// 可破坏物 / 敌人。核心循环里"小物件破碎、大物件变形、生物受伤"三件事
    /// 都由这一个组件按 <see cref="bodyClass"/> 分派，方便快速调参。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Destructible : MonoBehaviour, IDamageable
    {
        [Header("分类与耐久")]
        public BodyClass bodyClass = BodyClass.Small;

        [Tooltip("耐久。Small 通常一碰就碎（无视此值），Large/Creature 按此扣血")]
        public float integrity = 1f;

        [Tooltip("勾上则完全打不坏（用于当基准靶子）")]
        public bool indestructible = false;

        [Header("碎裂表现")]
        public Color debrisColor = new Color(0.78f, 0.6f, 0.35f, 1f);
        public int debrisCount = 6;
        public float debrisLifetime = 3f;

        [Header("击退")]
        [Tooltip("冲量对自身速度的影响系数，越大越容易被击飞")]
        public float knockbackScale = 1f;

        SpriteRenderer _visual;
        bool _broken;

        void Awake()
        {
            _visual = GetComponentInChildren<SpriteRenderer>();
        }

        public void TakeHit(Hit hit)
        {
            if (_broken || indestructible) return;

            var body = GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.AddForce(hit.impulse * knockbackScale, ForceMode2D.Impulse);
            }

            switch (bodyClass)
            {
                case BodyClass.Creature:
                    integrity -= hit.damage;
                    StartCoroutine(Flash(new Color(1f, 0.35f, 0.35f, 1f), 0.12f));
                    if (integrity <= 0f) Break();
                    break;

                case BodyClass.Large:
                    // 变形而不是立刻碎：压扁一点 + 轻微旋转变形，留"被打凹了"的观感
                    integrity -= hit.damage * 0.5f;
                    Deform(hit);
                    StartCoroutine(Flash(new Color(0.7f, 0.8f, 1f, 1f), 0.1f));
                    if (integrity <= 0f) Break();
                    break;

                default:
                    // Small：一碰就碎
                    Break();
                    break;
            }
        }

        /// <summary>大物件变形：只缩放视觉子物体，避免改变碰撞体体积。</summary>
        void Deform(Hit hit)
        {
            if (_visual == null) return;

            Vector3 s = _visual.transform.localScale;
            _visual.transform.localScale = new Vector3(s.x * 0.93f, s.y * 0.88f, s.z);

            // 朝被击方向轻微位移，做出"凹陷"的错觉
            Vector2 nudge = hit.impulse.sqrMagnitude > 0.0001f ? hit.impulse.normalized * 0.04f : Vector2.zero;
            _visual.transform.localPosition += (Vector3)nudge;

            float tilt = Mathf.Clamp(hit.impulse.x * 1.5f, -9f, 9f);
            _visual.transform.localRotation = Quaternion.Euler(0f, 0f, _visual.transform.localRotation.eulerAngles.z + tilt);
        }

        void Break()
        {
            _broken = true;

            int count = bodyClass == BodyClass.Large ? debrisCount * 2 : debrisCount;
            for (int i = 0; i < count; i++) SpawnDebris();

            Destroy(gameObject);
        }

        void SpawnDebris()
        {
            var go = new GameObject("Debris");
            go.transform.position = transform.position + (Vector3)(Random.insideUnitCircle * 0.15f);

            float size = Random.Range(0.06f, 0.15f);
            go.transform.localScale = new Vector3(size, size, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = WhiteboxSprites.Square;
            sr.color = debrisColor;
            sr.sortingOrder = IsoUtils.SortingOrderFor(go.transform.position, 100);

            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.drag = 3.5f;

            Vector2 dir = Random.insideUnitCircle;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
            body.AddForce(dir.normalized * Random.Range(1.5f, 4.5f), ForceMode2D.Impulse);

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;

            Destroy(go, debrisLifetime);
        }

        IEnumerator Flash(Color color, float duration)
        {
            if (_visual == null) yield break;

            Color original = _visual.color;
            _visual.color = color;
            yield return new WaitForSeconds(duration);
            if (_visual != null) _visual.color = original;
        }
    }
}
