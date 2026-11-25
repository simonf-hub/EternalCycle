using UnityEngine;

namespace DivineSense
{
    public sealed class MatchSetupController : MonoBehaviour
    {
        public GameSession Session;

        public void BeginTrial() => Begin(ActiveKind.Sight, MovementKind.Touch, ActiveKind.Sound, MovementKind.Taste);

        public void Begin(ActiveKind angelSense, MovementKind angelMovement, ActiveKind devilSense, MovementKind devilMovement)
        {
            if (Session.State != MatchState.Setup)
                return;

            PlayerContext[] players = Session.Registry.Players;
            players[0].HomeLoadout = new RuntimeLoadout(0, angelSense, angelMovement);
            players[1].HomeLoadout = new RuntimeLoadout(1, devilSense, devilMovement);

            foreach (PlayerContext player in players)
            {
                player.EquippedLoadout = player.HomeLoadout;
                player.Score.Initialize();
                player.Input.Placement(false);
                player.Motor.Begin();
            }

            Session.StartRound();
        }
    }
}
