using UnityEngine;

namespace GymRats
{
    public sealed class RatPlayerIndicator : MonoBehaviour
    {
        [SerializeField] private Color color = Color.cyan;
        private RatInputOwner owner;
        private GUIStyle label;
        private void Awake() => owner = GetComponent<RatInputOwner>();
        private void OnGUI()
        {
            if (owner == null || Camera.main == null) return;
            if (label == null)
            {
                label = new GUIStyle { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                label.normal.textColor = Color.white;
            }
            Vector3 point = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 3.45f);
            if (point.z <= 0f) return;
            Color old = GUI.color;
            var rect = new Rect(point.x - 22, Screen.height - point.y - 14, 44, 28);
            GUI.color = new Color(0.03f, 0.05f, 0.08f, 0.85f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = color;
            GUI.Label(rect, "P" + owner.PlayerNumber, label);
            GUI.color = old;
        }
    }
}
