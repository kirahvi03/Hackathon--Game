using UnityEngine;

namespace Hackathon.Gameplay
{
    /// <summary>
    /// 左右移动时水平翻转精灵。俯视视角下用翻转代替转身，一张图就够用。
    /// </summary>
    public class SpriteFacing : MonoBehaviour
    {
        public SpriteRenderer target;
        public Rigidbody2D body;
        public float deadzone = 0.05f;

        bool _facingRight = true;
        bool _initialized;

        void Reset()
        {
            target = GetComponentInChildren<SpriteRenderer>();
            body = GetComponent<Rigidbody2D>();
        }

        void Update()
        {
            if (target == null || body == null) return;

            float vx = body.velocity.x;
            if (Mathf.Abs(vx) < deadzone) return;

            bool right = vx > 0f;
            if (_initialized && right == _facingRight) return;

            _facingRight = right;
            _initialized = true;
            target.flipX = !right;
        }
    }
}