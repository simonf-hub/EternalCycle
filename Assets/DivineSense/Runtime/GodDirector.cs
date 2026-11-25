using System;
using UnityEngine;

namespace DivineSense
{
    public sealed class GodDirector : MonoBehaviour
    {
        private static readonly GodKind[] PickupGods =
            { GodKind.Athena, GodKind.Zeus, GodKind.Hera, GodKind.Ares };

        public GameSession Session;
        public Transform PickupsRoot;
        public string Cue = "THE COLOSSEUM AWAITS";
        private float nextAt;
        private int previous = -1;

        public event Action<GodKind> Intervened;
        public static int ExcludingPrevious(int roll, int previous) =>
            previous >= 0 && roll >= previous ? roll + 1 : roll;

        private void Start() => nextAt = Session.Settings.GodInterval;

        private void Update()
        {
            if (!Session.IsPlaying) return;
            float remaining = nextAt - Session.Now;
            if (remaining <= 2 && remaining > 0)
                Cue = "THE GODS ARE PREPARING A POWER-UP...";
            if (Session.Now < nextAt) return;
            nextAt += Session.Settings.GodInterval;
            int index = ExcludingPrevious(UnityEngine.Random.Range(0,
                previous < 0 ? PickupGods.Length : PickupGods.Length - 1), previous);
            previous = index;
            GodKind kind = PickupGods[index];
            Cue = GodPickup.DisplayName(kind) + " DROPS — " + Description(kind);
            for (int i = 0; i < Session.Settings.GodPickupCount; i++) Drop(kind);
            Intervened?.Invoke(kind);
        }

        private static string Description(GodKind kind) => kind switch
        {
            GodKind.Athena => "SWAP POSITIONS",
            GodKind.Zeus => "GROW, JUMP HIGHER, AND BECOME IMMUNE TO SIGHT AND SOUND",
            GodKind.Hera => "PULL THE OTHER MORTAL TOWARD YOU",
            GodKind.Ares => "LAUNCH A HOMING BOMB",
            _ => string.Empty
        };

        private void Drop(GodKind kind)
        {
            Rect bounds = Session.World.VisibleBounds;
            GameObject prefab = Session.Settings.PickupPrefabFor(kind);
            GameObject gift = Instantiate(prefab,
                new Vector3(UnityEngine.Random.Range(bounds.xMin + 1, bounds.xMax - 1),
                    bounds.yMax - Session.Settings.PickupSpawnInset, 0), Quaternion.identity, PickupsRoot);
            GodPickup pickup = gift.GetComponent<GodPickup>();
            pickup.BeginFalling(Session, kind, prefab == Session.Settings.GodPickupPrefab);
        }
    }
}
