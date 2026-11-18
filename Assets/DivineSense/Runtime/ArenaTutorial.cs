using EternalCycle;
using UnityEngine;

namespace DivineSense
{
    internal sealed class ArenaTutorial
    {
        internal const int PageCount = 3;

        private static readonly Color Ivory = new Color(0.95f, 0.90f, 0.80f);
        private static readonly Color Muted = new Color(0.73f, 0.76f, 0.81f);
        private static readonly Color Control = new Color(0.56f, 0.79f, 0.92f);
        private static readonly Color Card = new Color(0.09f, 0.11f, 0.15f);
        private readonly GameSettings settings;
        private readonly (ActiveKind Sense, MovementKind Movement)[] loadouts;
        private GUIStyle title, heading, controlHeading, body, key;

        internal ArenaTutorial(GameSettings settings, (ActiveKind, MovementKind) angel, (ActiveKind, MovementKind) devil)
        {
            this.settings = settings;
            loadouts = new[] { angel, devil };
        }

        internal void Draw(int page, Vector2 canvas)
        {
            if (title == null)
            {
                title = CenteredLabel(42, FontStyle.Bold);
                heading = CenteredLabel(23, FontStyle.Bold);
                controlHeading = new GUIStyle(heading) { wordWrap = false };
                body = CenteredLabel(20);
                key = CenteredLabel(21, FontStyle.Bold);
            }

            Vector2 center = canvas * 0.5f;
            switch (page)
            {
                case 0: DrawMovement(center); break;
                case 1: DrawAbilities(center); break;
                case 2: DrawGoal(center); break;
            }
        }

        private void DrawMovement(Vector2 center)
        {
            Label(center, -240f, 900f, 56f, "Controls", title, Ivory);

            Vector2 angel = center + Vector2.left * 340f;
            Vector2 devil = center + Vector2.right * 340f;
            Vector2 controller = center;
            DrawKeyboardControls(angel, 0, "A", "D", "W", "S");
            DrawKeyboardControls(devil, 1, "←", "→", "↑", "↓");

            Panel(controller, -146f, 300f, 257f);
            Label(controller, -126f, 270f, 32f, "CONTROLLER", controlHeading, Control);
            Keycap(controller, -68f, -78f, 120f, "Left stick");
            Keycap(controller, 68f, -78f, 94f, "D-pad");
            Label(controller, -30f, 270f, 26f, "Move left / right", body, Muted);
            Keycap(controller, -68f, 15f, 120f, "A / Cross");
            Keycap(controller, 68f, 15f, 120f, "B / Circle");
            Label(controller + Vector2.left * 68f, 62f, 130f, 26f, "Jump · bottom", body, Muted);
            Label(controller + Vector2.right * 68f, 62f, 130f, 26f, "Cast · right", body, Muted);

            Label(center, 137f, 950f, 28f, "Touch · Hold jump while falling to glide, then release to dash upward.", body, Muted);
            Label(center, 173f, 950f, 28f, "Taste · Press jump again in midair. Tap the same move direction twice quickly to dash.", body, Muted);
        }

        private void DrawKeyboardControls(Vector2 center, int player, string left, string right, string jump, string cast)
        {
            Panel(center, -146f, 300f, 257f);
            Label(center, -126f, 270f, 32f, $"Mortal {player + 1} · KEYBOARD",
                controlHeading, HUDController.FollowerColor(player));
            Keycap(center, 0f, -78f, 54f, jump);
            Label(center, -30f, 270f, 26f, "Jump", body, Muted);
            Keycap(center, -84f, 15f, 54f, left);
            Keycap(center, 0f, 15f, 54f, cast);
            Keycap(center, 84f, 15f, 54f, right);
            Label(center + Vector2.left * 84f, 62f, 82f, 26f, "Left", body, Muted);
            Label(center, 62f, 82f, 26f, "Cast", body, Muted);
            Label(center + Vector2.right * 84f, 62f, 82f, 26f, "Right", body, Muted);
        }

        private void DrawAbilities(Vector2 center)
        {
            Label(center, -232f, 900f, 58f, "Use your abilities", title, Ivory);
            for (int player = 0; player < loadouts.Length; player++)
            {
                Vector2 column = center + Vector2.right * (player == 0 ? -244f : 244f);
                Color accent = HUDController.FollowerColor(player);
                var (sense, movement) = loadouts[player];
                Panel(column, -140f, 460f, 310f);
                Label(column, -119f, 410f, 31f, LocalGamepads.ContestantName(player), heading, accent);
                Label(column, -77f, 410f, 32f, sense.ToString(), heading, Ivory);
                Label(column, -41f, 410f, 48f, sense == ActiveKind.Sight
                    ? "Release a wave that briefly hides a nearby rival."
                    : "Release a wave that knocks a nearby rival away.", body, Muted);
                bool touch = movement == MovementKind.Touch;
                Label(column, 32f, 410f, 32f, touch ? "Touch · Glide" : "Taste · Double jump & dash", heading, Ivory);
                Label(column, 70f, 410f, 74f, touch
                    ? "Slows your fall and charges an upward dash. A longer glide gives you a stronger dash."
                    : "Grants a second jump in midair and a quick horizontal dash on the ground or in the air.",
                    body, Muted);
            }
        }

        private void DrawGoal(Vector2 center)
        {
            Label(center, -207f, 950f, 62f, "Win more followers", title, Ivory);
            Label(center, -120f, 800f, 52f, "Collect the golden follower pickups on the arena platforms.", heading, Muted);

            Panel(center, -39f, 490f, 118f);
            Label(center, -28f, 460f, 56f, $"+{settings.FollowerPickupValue}", title, HUDController.FollowerColor(0));
            Label(center, 30f, 460f, 32f, "FOLLOWERS PER PICKUP", heading, Ivory);

            Label(center, 100f, 880f, 54f, settings.DeathLossFraction > 0f
                ? $"Stay on the moving platforms. Each death costs {settings.DeathLossFraction * 100f:0.#}% of your followers."
                : "Stay on the moving platforms. You keep your followers if you die.", body, Muted);
            Label(center, 154f, 880f, 54f,
                "Most followers when time runs out wins. Equal totals are a draw.", body, Ivory);
        }

        private void Keycap(Vector2 center, float x, float y, float width, string label)
        {
            center.x += x;
            Fill(new Rect(center.x - width / 2f, center.y + y + 3f, width, 42f), new Color(0.18f, 0.23f, 0.29f));
            Fill(new Rect(center.x - width / 2f, center.y + y, width, 40f), new Color(0.24f, 0.30f, 0.38f));
            Label(center, y, width, 40f, label, key, Color.white);
        }

        private static void Panel(Vector2 center, float y, float width, float height)
            => Fill(new Rect(center.x - width / 2f, center.y + y, width, height), Card);

        private static void Fill(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
        }

        private static void Label(Vector2 center, float y, float width, float height, string value, GUIStyle style, Color color)
        {
            GUI.color = color;
            GUI.Label(new Rect(center.x - width / 2f, center.y + y, width, height), value, style);
        }

        private static GUIStyle CenteredLabel(int size, FontStyle weight = FontStyle.Normal)
            => new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = size,
                fontStyle = weight,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
    }
}
