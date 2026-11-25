using UnityEngine;

namespace DivineSense
{
    public sealed class PositionSwapEffect : MonoBehaviour
    {
        public GameSession Session;

        public void Apply() => Apply(Session);

        public static void Apply(GameSession session)
        {
            if (!session.IsPlaying) return;

            var players = session.Registry.Players;
            // Capture both endpoints before moving either body or respawn marker.
            Vector2 first = players[0].Endpoint;
            Vector2 second = players[1].Endpoint;
            players[0].SetEndpoint(second);
            players[1].SetEndpoint(first);

            if (!session.IsPuzzle)
                foreach (var player in players)
                    if (player.CanAct && !session.World.Contains(player.Endpoint))
                        player.Life.TryDie();
        }
    }
}
