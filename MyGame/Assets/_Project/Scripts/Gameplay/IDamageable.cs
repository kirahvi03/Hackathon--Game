using UnityEngine;

namespace Hackathon.Gameplay
{
    /// <summary>一次命中的全部信息。</summary>
    public struct Hit
    {
        /// <summary>造成的损伤量，1 表示"标准一击"。</summary>
        public float damage;

        /// <summary>施加给目标刚体的冲量（已经包含方向与大小）。</summary>
        public Vector2 impulse;

        /// <summary>命中点（世界坐标）。</summary>
        public Vector2 point;

        public Hit(float damage, Vector2 impulse, Vector2 point)
        {
            this.damage = damage;
            this.impulse = impulse;
            this.point = point;
        }
    }

    /// <summary>
    /// 可被触手击中的目标。玩家的攻击只认这个接口，
    /// 因此以后加新的可破坏物/敌人不需要改攻击代码。
    /// </summary>
    public interface IDamageable
    {
        void TakeHit(Hit hit);
    }
}
