using UnityEngine;

namespace Hackathon.Gameplay
{
    /// <summary>
    /// 交互器：每帧找最近的可交互角色，按 E 开始对话。
    /// 只在对话没打开时响应，避免和 DialogueSystem 的"继续"抢同一个按键。
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("范围")]
        [Tooltip("能触发交互的最大距离（世界单位）")]
        public float radius = 1.6f;

        [Tooltip("检测用的层，默认全部")]
        public LayerMask mask = ~0;

        /// <summary>当前范围内最近的可交互对象（供 HUD 显示提示）。</summary>
        public Interactable Nearest { get; private set; }

        void Update()
        {
            Nearest = FindNearest();

            if (Nearest == null) return;
            if (!Input.GetKeyDown(KeyCode.E)) return;

            DialogueSystem dialogue = DialogueSystem.Instance;
            if (dialogue != null && !dialogue.IsOpen)
            {
                dialogue.TryOpen(Nearest);
            }
        }

        Interactable FindNearest()
        {
            Interactable best = null;
            float bestSqr = radius * radius;

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, mask);
            for (int i = 0; i < hits.Length; i++)
            {
                Interactable candidate = hits[i].GetComponentInParent<Interactable>();
                if (candidate == null) continue;
                if (candidate.singleUse && candidate.consumed) continue;
                if (candidate.lines == null || candidate.lines.Length == 0) continue;

                float d = ((Vector2)hits[i].transform.position - (Vector2)transform.position).sqrMagnitude;
                if (d <= bestSqr)
                {
                    bestSqr = d;
                    best = candidate;
                }
            }

            return best;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 0.9f, 0.7f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
