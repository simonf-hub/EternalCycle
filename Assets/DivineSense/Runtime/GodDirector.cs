using System;
using UnityEngine;

namespace DivineSense
{
    public sealed class GodDirector : MonoBehaviour
    {
        // Keep enum IDs stable for serialized assets; Zeus no longer enters this pool.
        private static readonly GodKind[] PickupGods =
            { GodKind.Hermes, GodKind.Demeter, GodKind.Athena, GodKind.Dionysus };

        public GameSession Session;
        public Transform PickupsRoot;
        public PositionSwapEffect PositionSwap;
        public GrowthEffect Growth;
        public AbilitySwapEffect AbilitySwap;
        public ReverseControlsEffect Reverse;
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
            Cue = kind + " DROPS — " + Description(kind);
            for (int i = 0; i < Session.Settings.GodPickupCount; i++) Drop(kind);
            Intervened?.Invoke(kind);
        }

        private string Description(GodKind kind) => kind == GodKind.Hermes ? "POSITION SWAP" :
            kind == GodKind.Demeter ? "GROWTH" : kind == GodKind.Athena ? "ABILITY SWAP" :
            "OPPONENT CONTROLS REVERSED";

        private void Drop(GodKind kind)
        {
            Rect bounds = Session.World.VisibleBounds;
            GameObject gift = Instantiate(Session.Settings.PickupPrefabFor(kind),
                new Vector3(UnityEngine.Random.Range(bounds.xMin + 1, bounds.xMax - 1),
                    bounds.yMax - Session.Settings.PickupSpawnInset, 0), Quaternion.identity, PickupsRoot);
            GodPickup pickup = gift.GetComponent<GodPickup>();
            pickup.Session = Session;
            pickup.Kind = kind;
            pickup.Effect = collector =>
            {
                switch (kind)
                {
                    case GodKind.Hermes: PositionSwap.Apply(); break;
                    case GodKind.Demeter: Growth.Apply(collector); break;
                    case GodKind.Athena: AbilitySwap.Apply(); break;
                    case GodKind.Dionysus: Reverse.Apply(collector.Registry.Opponent(collector)); break;
                }
            };
            var label = gift.GetComponentInChildren<TextMesh>();
            if (label != null) label.text = kind.ToString();
        }
    }
}
