using UnityEngine;

namespace DivineSense
{
    public sealed class DoubleJumpAbility : MonoBehaviour
    {
        public bool Allowed(PlayerContext player, bool used) =>
            !used && player.EquippedLoadout.MovementKind == MovementKind.Taste;

        public float JumpVelocity(PlayerContext player, bool airborne)
        {
            // Jump height scales with velocity squared under constant gravity.
            float heightMultiplier = airborne
                ? Mathf.Clamp(player.Settings.TasteSecondJumpHeightMultiplier, .1f, 1f)
                : 1f;
            if (player.Status.Grown)
                heightMultiplier *= Mathf.Max(1f, player.Settings.GrowthJumpHeightMultiplier);
            return player.Settings.JumpSpeed * Mathf.Sqrt(heightMultiplier);
        }
    }
}
