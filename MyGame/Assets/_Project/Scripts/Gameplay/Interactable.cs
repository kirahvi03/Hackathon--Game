using UnityEngine;

namespace Hackathon.Gameplay
{
    /// <summary>可交互角色的类型，决定对话内容与对话结束后的行为。</summary>
    public enum CharacterKind
    {
        /// <summary>研究人员：尝试与章鱼交流，不攻击</summary>
        Researcher,

        /// <summary>武装人员：不愿多谈，直接开枪</summary>
        Guard
    }

    /// <summary>
    /// 挂在可交谈的角色身上。由 PlayerInteractor 发现，由 DialogueSystem 播放。
    /// 对话内容与立绘都在这里配置，方便策划直接改。
    /// </summary>
    public class Interactable : MonoBehaviour
    {
        [Header("身份")]
        public CharacterKind kind = CharacterKind.Researcher;

        [Tooltip("对话框里显示的名字")]
        public string displayName = "研究员";

        [Header("对话")]
        [Tooltip("按顺序显示的每一句")]
        [TextArea(2, 6)]
        public string[] lines = new string[0];

        [Tooltip("人物立绘（Resources/Portraits 下的 Sprite）")]
        public Sprite portrait;

        [Tooltip("勾上则整段对话只能触发一次")]
        public bool singleUse = true;

        [Header("对话结束后的效果")]
        [Tooltip("扣生命值（正数扣血）")]
        public float healthDamageOnEnd = 0f;

        [Tooltip("pH 变化（负数代表变酸）")]
        public float phDeltaOnEnd = 0f;

        [Tooltip("结束时是否播放开枪表现（枪口闪光等）")]
        public bool shootsOnEnd = false;

        /// <summary>是否已经播放过（配合 singleUse）。</summary>
        [System.NonSerialized]
        public bool consumed = false;
    }
}
