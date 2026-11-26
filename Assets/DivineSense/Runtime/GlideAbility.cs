using UnityEngine;

namespace DivineSense
{
    public sealed class GlideAbility : MonoBehaviour
    {
        private bool wasGliding;
        private float dashRemaining;
        private bool finishUpwardDash;
        private float chargeSeconds;
        private float charge01;
        private float dashDistance;
        private bool usedInAir;

        public bool Available => !usedInAir;

        public bool Charging => wasGliding;
        public float Charge01 => charge01;
        public float LastDashDistance => dashDistance;

        public bool Dashing => dashRemaining > 0;

        // The final physics step still moves the player after the timer reaches zero.
        internal bool DashedThisStep => dashDistance > 0f && (Dashing || finishUpwardDash);

        public float Limit(PlayerContext player, float velocity) =>
            player.EquippedLoadout.MovementKind == MovementKind.Touch && player.Input.Held && velocity < 0
                ? Mathf.Max(velocity, -player.Settings.GlideSpeed) : velocity;

        public void ApplyMotion(PlayerContext player, ref Vector2 velocity, bool grounded, bool released)
        {
            if (grounded)
            {
                ResetAirUse();
                return;
            }
            if (player.EquippedLoadout.MovementKind != MovementKind.Touch)
            {
                Cancel();
                return;
            }

            // Stop upward dash momentum after the last physics step, keeping the charge
            // distance a real cap rather than turning the burst into a much higher jump.
            if (finishUpwardDash)
            {
                velocity.y = Mathf.Min(0, velocity.y);
                finishUpwardDash = false;
            }

            // Only an intentional release of an actual descending glide earns the burst.
            // Input cancellation (stun, unplugging a controller, respawn) is not a release.
            if (!usedInAir && wasGliding && released && !player.Input.Held)
            {
                usedInAir = true;
                dashDistance = Mathf.Lerp(player.Settings.GlideDashDistance,
                    Mathf.Max(player.Settings.GlideDashDistance, player.Settings.GlideDashMaxDistance), charge01);
                dashRemaining = player.Settings.GlideDashDuration;
                chargeSeconds = charge01 = 0;
                finishUpwardDash = false;
            }
            wasGliding = !usedInAir && player.Input.Held && velocity.y < 0;
            if (wasGliding)
            {
                float maxCharge = Mathf.Max(.01f, player.Settings.GlideDashChargeTime);
                chargeSeconds = Mathf.Min(maxCharge, chargeSeconds + Time.fixedDeltaTime);
                charge01 = Mathf.Clamp01(chargeSeconds / maxCharge);
            }
            else chargeSeconds = charge01 = 0;
            velocity.y = Limit(player, velocity.y);
            if (dashRemaining <= 0) return;

            // Use the dynamic body, so walls and the other player still block the dash.
            // Scale the final step so distance remains consistent across fixed timesteps.
            float fraction = Mathf.Min(dashRemaining, Time.fixedDeltaTime) / Time.fixedDeltaTime;
            float dashSpeed = dashDistance / Mathf.Max(.01f, player.Settings.GlideDashDuration);
            velocity.y = dashSpeed * fraction;
            dashRemaining = Mathf.Max(0, dashRemaining - Time.fixedDeltaTime);
            finishUpwardDash = dashRemaining <= 0;
        }

        // Input cancellation and midair swaps/teleports cannot refund an already-used dash.
        // Ground contact and starting a new life are the only reset points.
        public void ResetAirUse() { Cancel(); usedInAir = false; }

        public void Cancel() { wasGliding = false; dashRemaining = 0; finishUpwardDash = false; chargeSeconds = charge01 = 0; }
    }
}
