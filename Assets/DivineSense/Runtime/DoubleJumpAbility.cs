using UnityEngine;

namespace DivineSense
{
    public sealed class DoubleJumpAbility : MonoBehaviour
    {
        private float dashRemaining;
        private float dashVelocity;

        public bool Allowed(PlayerContext player, bool used) =>
            !used && player.EquippedLoadout.MovementKind == MovementKind.Taste;

        public float JumpVelocity(PlayerContext player, bool secondJump)
        {
            // Jump height scales with velocity squared under constant gravity.
            float heightMultiplier = secondJump
                ? Mathf.Clamp(player.Settings.TasteSecondJumpHeightMultiplier, .1f, 1f)
                : 1f;
            if (player.Status.Grown)
                heightMultiplier *= Mathf.Max(1f, player.Settings.GrowthJumpHeightMultiplier);
            return player.Settings.JumpSpeed * Mathf.Sqrt(heightMultiplier);
        }

        public float DashVelocity(PlayerContext player, int tapDirection, float walkingVelocity)
        {
            if (player.EquippedLoadout.MovementKind != MovementKind.Taste)
            {
                CancelDash();
                player.Input.ClearDashTaps();
                return walkingVelocity;
            }

            if (dashRemaining <= 0 && tapDirection != 0 && player.Settings.TasteDashDistance > 0)
            {
                dashRemaining = Mathf.Max(.01f, player.Settings.TasteDashDuration);
                int direction = player.Status.Reversed ? -tapDirection : tapDirection;
                dashVelocity = direction * player.Settings.TasteDashDistance / dashRemaining;
            }
            if (dashRemaining <= 0)
                return walkingVelocity;

            // Keep the configured distance independent of the physics timestep.
            float step = Mathf.Min(dashRemaining, Time.fixedDeltaTime);
            dashRemaining -= step;
            return (dashVelocity * step + walkingVelocity * (Time.fixedDeltaTime - step)) / Time.fixedDeltaTime;
        }

        public void CancelDash() => dashRemaining = 0;
    }
}
