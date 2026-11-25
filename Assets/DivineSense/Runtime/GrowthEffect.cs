using UnityEngine;

namespace DivineSense
{
    public sealed class GrowthEffect : MonoBehaviour
    {
        public void Apply(PlayerContext player) => Grow(player);

        internal static void Grow(PlayerContext player)
        {
            if (player.CanAct)
                player.Status.GrownUntil = player.Session.Now + player.Settings.GrowthDuration;
        }
    }
}
