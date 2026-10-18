using UnityEngine;

namespace Hackathon.Gameplay
{
    /// <summary>
    /// 极短命的表现特效：武装人员开枪时画一条从枪口到目标的亮线。
    /// 白盒阶段不接粒子系统，用一根缩放过的方块代替，够表达"挨了一枪"。
    /// </summary>
    public class MuzzleFlash : MonoBehaviour
    {
        public static void Spawn(Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.05f)
            {
                length = 0.6f;
                delta = Vector2.down * 0.6f;
            }

            var go = new GameObject("MuzzleFlash");
            go.transform.position = (from + to) * 0.5f;
            go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            go.transform.localScale = new Vector3(length, 0.05f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = WhiteboxSprites.Square;
            sr.color = new Color(1f, 0.92f, 0.55f, 1f);

            // 枪口处再补一个亮点
            var flash = new GameObject("Flash");
            flash.transform.SetParent(go.transform, false);
            flash.transform.position = from;
            flash.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
            var fsr = flash.AddComponent<SpriteRenderer>();
            fsr.sprite = WhiteboxSprites.Square;
            fsr.color = new Color(1f, 0.85f, 0.4f, 1f);

            Destroy(go, 0.09f);
        }
    }
}
