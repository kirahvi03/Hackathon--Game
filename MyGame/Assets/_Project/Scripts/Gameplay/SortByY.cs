using UnityEngine;

namespace Hackathon.Gameplay
{
    /// <summary>
    /// 等轴测下的前后遮挡排序。挂在会移动的物体上（玩家、可推动的箱子、敌人）。
    /// 静止的地面不需要，直接给一个很低的 sortingOrder 就行。
    /// </summary>
    public class SortByY : MonoBehaviour
    {
        [Tooltip("基础排序层，用于把一类物体整体压上/压下")]
        public int baseOrder = 0;

        SpriteRenderer _renderer;

        void Awake()
        {
            // 视觉在子物体上（这样变形/缩放不会影响碰撞体），所以要往子级找
            _renderer = GetComponentInChildren<SpriteRenderer>();
        }

        void LateUpdate()
        {
            if (_renderer == null) return;
            _renderer.sortingOrder = IsoUtils.SortingOrderFor(transform.position, baseOrder);
        }
    }
}
