using UnityEngine;

namespace DivineSense
{
    [DisallowMultipleComponent]
    public sealed class CooperativeGate : MonoBehaviour
    {
        public Collider2D Barrier;
        public SpriteRenderer[] Visuals;
        public CooperativePressurePlate LowerPlate;
        public CooperativePressurePlate UpperPlate;
        public bool IsOpen { get; private set; }

        internal void TryOpen(PlayerContext first, PlayerContext second)
        {
            if (IsOpen)
                return;

            bool firstLower = LowerPlate.Contains(first);
            bool secondLower = LowerPlate.Contains(second);
            bool firstUpper = UpperPlate.Contains(first);
            bool secondUpper = UpperPlate.Contains(second);
            LowerPlate.SetPressed(firstLower || secondLower);
            UpperPlate.SetPressed(firstUpper || secondUpper);

            if (!(firstLower && secondUpper) && !(secondLower && firstUpper))
                return;

            IsOpen = true;
            Barrier.enabled = false;
            foreach (var visual in Visuals)
            {
                Color color = visual.color;
                color.a *= .2f;
                visual.color = color;
            }
        }
    }
}
