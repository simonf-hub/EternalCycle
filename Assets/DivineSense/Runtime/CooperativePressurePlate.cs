using UnityEngine;

namespace DivineSense
{
    [DisallowMultipleComponent]
    public sealed class CooperativePressurePlate : MonoBehaviour
    {
        public Collider2D Zone;
        public SpriteRenderer Indicator;
        public Color PressedColor = new Color(.56f, 1f, .76f);

        private Color releasedColor;

        private void Awake() => releasedColor = Indicator.color;

        internal bool Contains(PlayerContext player) => player.CanAct && Zone.OverlapPoint(player.Endpoint);

        internal void SetPressed(bool pressed) => Indicator.color = pressed ? PressedColor : releasedColor;
    }
}
