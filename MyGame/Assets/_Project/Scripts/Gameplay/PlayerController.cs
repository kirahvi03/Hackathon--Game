using UnityEngine;

namespace Hackathon.Gameplay
{
    /// <summary>
    /// 章鱼主角：等轴测移动 + 触手扑击。
    ///
    /// 设计决策（2026-10-14）：**不做跳跃**。俯视等轴测没有天然的垂直轴，
    /// 伪高度会引入排序与碰撞的一堆 bug。垂直方向的动感改由"扑击"提供：
    /// 按下攻击键时给自身一个前冲速度，带惯性滑行，同时判定身前圆形范围内的目标。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("移动")]
        [Tooltip("最大移动速度（世界单位/秒）")]
        public float moveSpeed = 4.5f;

        [Tooltip("加速度，越大手感越硬")]
        public float acceleration = 35f;

        [Tooltip("松手后的减速（越小滑得越远，惯性感越强）")]
        public float drag = 7f;

        [Header("扑击")]
        [Tooltip("扑击给自身的冲量，这就是「垂直动作感」的来源")]
        public float lungeImpulse = 9f;

        [Tooltip("两次扑击的最小间隔（秒）")]
        public float lungeCooldown = 0.45f;

        [Tooltip("攻击判定圆半径")]
        public float strikeRadius = 0.9f;

        [Tooltip("判定的圆心相对身体前方的距离")]
        public float strikeReach = 0.7f;

        [Tooltip("对目标造成的损伤量")]
        public float strikeDamage = 1f;

        [Tooltip("对目标施加的冲量大小")]
        public float strikeImpulse = 7f;

        [Tooltip("扑击后的无敌/不可再次输入时长（秒）")]
        public float strikeDuration = 0.18f;

        public KeyCode strikeKey = KeyCode.F;

        Rigidbody2D _body;
        SpriteRenderer _visual;
        Color _baseColor;

        Vector2 _facing = Vector2.right;
        float _nextStrikeTime;
        float _strikeEndTime;

        /// <summary>是否正在扑击动作中。</summary>
        public bool IsStriking { get { return Time.time < _strikeEndTime; } }

        /// <summary>当前朝向（世界空间，等轴测方向）。</summary>
        public Vector2 Facing { get { return _facing; } }

        void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _visual = GetComponentInChildren<SpriteRenderer>();
            if (_visual != null) _baseColor = _visual.color;

            _body.gravityScale = 0f;
            _body.drag = drag;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        void Update()
        {
            Vector2 rawInput = new Vector2(
                (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f),
                (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f));

            Vector2 wish = IsoUtils.InputToWorld(rawInput);
            if (wish.sqrMagnitude > 0.01f) _facing = wish;

            // 扑击期间不接受移动输入，让冲量自然衰减 —— 这是"惯性"手感的关键
            if (!IsStriking)
            {
                Vector2 target = wish * moveSpeed;
                _body.velocity = Vector2.MoveTowards(_body.velocity, target, acceleration * Time.deltaTime);
            }

            if (Input.GetKeyDown(strikeKey) && Time.time >= _nextStrikeTime)
            {
                Strike();
            }
        }

        void Strike()
        {
            _nextStrikeTime = Time.time + lungeCooldown;
            _strikeEndTime = Time.time + strikeDuration;

            // 1) 自身前冲：用速度覆盖，手感是"窜出去一下"
            _body.velocity = _facing * lungeImpulse;

            // 2) 视觉反馈：闪一下颜色
            if (_visual != null) _visual.color = new Color(1f, 0.85f, 0.3f, 1f);

            // 3) 判定：身体前方的圆形范围
            Vector2 center = (Vector2)transform.position + _facing * strikeReach;
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, strikeRadius);

            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hit = hits[i];

                // 别打到自己
                if (hit.attachedRigidbody != null && hit.attachedRigidbody.gameObject == gameObject) continue;

                IDamageable target = hit.GetComponentInParent<IDamageable>();
                if (target == null) continue;

                Vector2 toTarget = ((Vector2)hit.transform.position - (Vector2)transform.position);
                Vector2 direction = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : _facing;

                target.TakeHit(new Hit(strikeDamage, direction * strikeImpulse, hit.ClosestPoint(center)));
            }
        }

        void LateUpdate()
        {
            // 扑击闪光结束，恢复原色
            if (_visual != null && !IsStriking && _visual.color != _baseColor)
            {
                _visual.color = _baseColor;
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.6f);
            Vector2 facing = Application.isPlaying ? _facing : Vector2.right;
            Vector2 center = (Vector2)transform.position + facing * strikeReach;
            Gizmos.DrawWireSphere(center, strikeRadius);
        }
    }
}
