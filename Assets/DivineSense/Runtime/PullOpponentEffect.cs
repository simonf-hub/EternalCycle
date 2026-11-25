using UnityEngine;

namespace DivineSense
{
    public static class PullOpponentEffect
    {
        public static void Apply(PlayerContext collector)
        {
            if (!collector || !collector.CanAct) return;
            PlayerContext target = collector.Registry.Opponent(collector);
            if (!target || !target.CanAct) return;
            Vector2 towardCollector = collector.Endpoint - target.Endpoint;
            if (towardCollector.sqrMagnitude < .01f) return;
            target.Motor.QueueKnockback(towardCollector.normalized * Mathf.Max(0, collector.Settings.PullImpulse));
        }
    }
}
