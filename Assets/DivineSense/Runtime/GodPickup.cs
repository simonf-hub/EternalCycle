using System;
using UnityEngine;

namespace DivineSense
{
    public sealed class GodPickup : MonoBehaviour
    {
        public GameSession Session;
        public GodKind Kind;
        public event Action<PlayerContext> Collected;

        internal bool IsFalling { get; private set; }
        private bool claimed;

        public static string DisplayName(GodKind kind) => kind switch
        {
            GodKind.Athena or GodKind.Hermes => "Athena’s Position",
            GodKind.Zeus or GodKind.Demeter => "Zeus’s Agility",
            GodKind.Hera or GodKind.Poseidon => "Hera’s Pull",
            GodKind.Ares => "Ares’s Stare",
            _ => kind.ToString()
        };

        public static Color PickupColor(GodKind kind) => kind switch
        {
            GodKind.Athena or GodKind.Hermes => new Color(.65f, .35f, 1f),
            GodKind.Zeus or GodKind.Demeter => new Color(1f, .8f, .15f),
            GodKind.Hera or GodKind.Poseidon => new Color(1f, .4f, .75f),
            GodKind.Ares => new Color(.3f, 1f, .45f),
            _ => Color.white
        };

        public void RefreshAppearance()
        {
            string displayName = DisplayName(Kind);
            gameObject.name = displayName;
            var label = GetComponentInChildren<TextMesh>();
            if (label) label.text = displayName;
            var sprite = GetComponent<SpriteRenderer>();
            if (sprite) sprite.color = PickupColor(Kind);
        }

        internal void BeginFalling(GameSession session, GodKind kind, bool useDefaultAppearance)
        {
            Session = session;
            IsFalling = true;
            bool refresh = useDefaultAppearance || Kind != kind;
            Kind = kind;
            if (refresh) RefreshAppearance();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (claimed || !isActiveAndEnabled) return;
            var player = other.GetComponentInParent<PlayerContext>();
            if (!player || !player.CanAct || (Session && player.Session != Session)) return;

            claimed = true;
            switch (Kind)
            {
                case GodKind.Athena:
                case GodKind.Hermes:
                    PositionSwapEffect.Apply(player.Session);
                    break;
                case GodKind.Zeus:
                case GodKind.Demeter:
                    GrowthEffect.Grow(player);
                    break;
                case GodKind.Hera:
                case GodKind.Poseidon:
                    PullOpponentEffect.Apply(player);
                    break;
                case GodKind.Ares:
                    HomingBomb.Launch(player, transform.parent);
                    break;
                case GodKind.Dionysus:
                    ReverseControlsEffect.Reverse(player.Registry.Opponent(player));
                    break;
            }
            Destroy(gameObject);
            Collected?.Invoke(player);
        }
    }
}
