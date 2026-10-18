using UnityEngine;

namespace Hackathon.Gameplay
{
    /// <summary>
    /// 主角的生命与 pH 状态。
    /// pH 是这套设计里的第二资源：中性 7.4，受刺激/受伤时向酸性偏移（数值下降）。
    /// </summary>
    public class PlayerVitals : MonoBehaviour
    {
        [Header("生命")]
        public float maxHealth = 100f;
        public float health = 100f;

        [Header("pH")]
        [Tooltip("中性约 7.4；数值下降代表向酸性偏移")]
        public float ph = 7.4f;
        public float maxPh = 14f;
        public float minPh = 0f;

        public bool IsDead { get { return health <= 0f; } }

        /// <summary>累计承受的伤害，用于调试与 HUD 闪红。</summary>
        public float LastDamageTime { get; private set; }

        /// <summary>
        /// 施加一次影响：healthDelta 为正表示扣血，phDelta 为负表示向酸性偏移。
        /// </summary>
        public void ApplyDamage(float healthDelta, float phDelta)
        {
            if (healthDelta != 0f)
            {
                health = Mathf.Clamp(health - healthDelta, 0f, maxHealth);
                LastDamageTime = Time.time;
            }
            if (phDelta != 0f)
            {
                ph = Mathf.Clamp(ph + phDelta, minPh, maxPh);
            }
        }

        /// <summary>当前 pH 的色调：越酸越偏红，用于 HUD。</summary>
        public Color PhColor()
        {
            float t = Mathf.InverseLerp(4f, 9f, ph);
            return Color.Lerp(new Color(0.95f, 0.35f, 0.30f), new Color(0.55f, 0.85f, 0.55f), t);
        }
    }
}
