using System;
using MicroSense;
using UnityEngine;

namespace DivineSense
{
    [DisallowMultipleComponent]
    public sealed class CooperativeLevelController : MonoBehaviour
    {
        [Serializable]
        public sealed class Checkpoint
        {
            public Collider2D Zone;
            public Transform AngelSpawn;
            public Transform DevilSpawn;

            public Transform SpawnFor(int player) => player == 0 ? AngelSpawn : DevilSpawn;
        }

        [Serializable]
        public sealed class Lesson
        {
            public Collider2D Zone;
            public string Title;
            [TextArea] public string Instructions;
        }

        [Header("Trial")]
        public GameSession Session;
        public MatchSetupController Setup;
        public string Title;
        public Transform AngelStart;
        public Transform DevilStart;
        public Collider2D Exit;
        public Checkpoint[] Checkpoints = Array.Empty<Checkpoint>();
        public Lesson[] Lessons = Array.Empty<Lesson>();

        [Header("Exit requirements")]
        public HiddenPlatform[] RequiredReveals = Array.Empty<HiddenPlatform>();
        public BreakablePlatform[] RequiredBreaks = Array.Empty<BreakablePlatform>();
        public GodPickup[] RequiredGifts = Array.Empty<GodPickup>();
        public CooperativeGate[] RequiredGates = Array.Empty<CooperativeGate>();

        [Header("Camera and recovery")]
        [Min(3f)] public float ViewHeight = 5.5f;
        public Transform LeftLimit;
        public Transform RightLimit;
        public Transform FallLimit;

        private Checkpoint checkpoint;
        private string checkpointNotice;
        private float checkpointNoticeUntil;
        private GUIStyle titleStyle, bodyStyle, centeredStyle, completionStyle;
        private bool complete;

        private PlayerContext[] Players => Session.Registry.Players;

        private void Start()
        {
            Session.Settings.ScrollSpeed = 0;
            Session.BeginCooperativeLevel(() =>
            {
                Setup.BeginTrial();
                ResetPosition(Players[0]);
                ResetPosition(Players[1]);
            });
            FollowPlayers(AngelStart.position, DevilStart.position);
        }

        private void LateUpdate()
        {
            if (!Session.IsPlaying || complete)
                return;

            var first = Players[0];
            var second = Players[1];
            if (first.Endpoint.y < FallLimit.position.y)
                ResetPosition(first);
            if (second.Endpoint.y < FallLimit.position.y)
                ResetPosition(second);

            foreach (var gate in RequiredGates)
                gate.TryOpen(first, second);

            ActivateCheckpoint(first, second);
            FollowPlayers(first.Endpoint, second.Endpoint);
            if (!IsAtExit(first) || !IsAtExit(second) || RemainingObjective() != null)
                return;

            complete = true;
            Session.CompleteCooperativeLevel();
        }

        private void ResetPosition(PlayerContext player)
        {
            Transform spawn = checkpoint == null
                ? player.Id == 0 ? AngelStart : DevilStart
                : checkpoint.SpawnFor(player.Id);
            player.Status.Clear();
            player.Motor.SetSize(1f);
            player.Motor.Teleport(spawn.position, true);
            player.Input.DiscardActions();
        }

        private void ActivateCheckpoint(PlayerContext first, PlayerContext second)
        {
            foreach (var candidate in Checkpoints)
            {
                if (candidate == checkpoint || !ContainsBoth(candidate.Zone, first, second))
                    continue;
                checkpoint = candidate;
                checkpointNotice = "Checkpoint reached";
                checkpointNoticeUntil = Session.Now + 2.5f;
                return;
            }
        }

        private void FollowPlayers(Vector2 first, Vector2 second)
        {
            Camera view = Session.World.Camera;
            float halfHeight = Mathf.Max(ViewHeight,
                Mathf.Abs(first.y - second.y) * .5f + 2.8f,
                (Mathf.Abs(first.x - second.x) * .5f + 3.5f) / view.aspect);
            float halfWidth = halfHeight * view.aspect;
            float left = LeftLimit.position.x + halfWidth;
            float right = RightLimit.position.x - halfWidth;
            Vector2 center = (first + second) * .5f + Vector2.up * .85f;
            center.x = left <= right ? Mathf.Clamp(center.x, left, right) : (left + right) * .5f;
            center.y = Mathf.Max(FallLimit.position.y + halfHeight, center.y);
            view.orthographicSize = halfHeight;
            view.transform.position = new Vector3(center.x, center.y, view.transform.position.z);
        }

        private static bool ContainsBoth(Collider2D zone, PlayerContext first, PlayerContext second) =>
            first.CanAct && second.CanAct && zone.OverlapPoint(first.Endpoint) && zone.OverlapPoint(second.Endpoint);

        private bool IsAtExit(PlayerContext player)
        {
            if (!player.CanAct)
                return false;

            ColliderDistance2D contact = Exit.Distance(player.Motor.Collider);
            return contact.isValid && contact.distance <= 0f;
        }

        private string RemainingObjective()
        {
            foreach (var platform in RequiredReveals)
                if (!platform.IsRevealed)
                    return "Reveal the remaining hidden platforms with Sight.";
            foreach (var wall in RequiredBreaks)
                if (!wall.IsBroken)
                    return "Break the remaining cracked walls with Sound.";
            foreach (var pickup in RequiredGifts)
                if (pickup)
                    return "Collect " + GodPickup.DisplayName(pickup.Kind) + ".";
            foreach (var gate in RequiredGates)
                if (!gate.IsOpen)
                    return "Open the remaining gates by standing on both plates together.";
            return null;
        }

        private Lesson CurrentLesson()
        {
            foreach (var lesson in Lessons)
            {
                if (lesson.Zone.OverlapPoint(Players[0].Endpoint) ||
                    lesson.Zone.OverlapPoint(Players[1].Endpoint))
                    return lesson;
            }
            return null;
        }

        private void OnGUI()
        {
            if (Session.IsShowingNarrative || (!Session.IsPlaying && !complete))
                return;

            InitializeStyles();
            Matrix4x4 previousMatrix = ResponsiveGui.ApplyScale(out var canvas);
            Color previousColor = GUI.color;

            if (complete && Session.Trial == TrialLevel.Cooperative)
            {
                var panel = new Rect((canvas.x - 660f) / 2f, canvas.y / 2f - 116f, 660f, 228f);
                Fill(panel, new Color(.025f, .035f, .065f, .97f));
                GUI.Label(new Rect(panel.x + 20f, panel.y + 29f, panel.width - 40f, 70f),
                    "The trial is complete", completionStyle);
                GUI.Label(new Rect(panel.x + 20f, panel.y + 127f, panel.width - 40f, 52f),
                    "Press A, Space, or Enter to return to Level 0.", centeredStyle);
            }
            else if (Session.IsPlaying)
            {
                DrawStatus(canvas);
                DrawLesson(canvas);
            }

            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }

        private void DrawStatus(Vector2 canvas)
        {
            Fill(new Rect(0f, 0f, canvas.x, 88f), new Color(.025f, .035f, .065f, .92f));
            GUI.Label(new Rect(22f, 9f, canvas.x - 44f, 34f), Title, titleStyle);
            float columnWidth = (canvas.x - 60f) * .5f;

            for (int i = 0; i < Players.Length; i++)
            {
                var player = Players[i];
                var loadout = player.EquippedLoadout;
                string ready = player.Abilities.Cooldown > 0
                    ? $"{Mathf.CeilToInt(player.Abilities.Cooldown)}s"
                    : "Ready";
                GUI.color = HUDController.FollowerColor(i);
                GUI.Label(new Rect(23f + i * (columnWidth + 14f), 45f, columnWidth, 33f),
                    $"Mortal {i + 1} · {loadout.ActiveKind} / {loadout.MovementKind} · {ready}", bodyStyle);
            }
            GUI.color = Color.white;
        }

        private void DrawLesson(Vector2 canvas)
        {
            Lesson lesson = CurrentLesson();
            bool atExit = IsAtExit(Players[0]) || IsAtExit(Players[1]);
            string checkpointCue = Session.Now < checkpointNoticeUntil ? checkpointNotice : null;
            string header = checkpointCue ?? (atExit ? "Exit" : lesson?.Title);
            string instructions = atExit
                ? RemainingObjective() ?? "Bring both mortals to the exit together."
                : lesson?.Instructions;
            if (header == null && instructions == null)
                return;

            var panel = new Rect(18f, canvas.y - 105f, canvas.x - 36f, 88f);
            Fill(panel, new Color(.025f, .035f, .065f, .93f));
            GUI.Label(new Rect(panel.x + 16f, panel.y + 6f, panel.width - 32f, 29f), header, titleStyle);
            GUI.Label(new Rect(panel.x + 16f, panel.y + 34f, panel.width - 32f, 46f), instructions, bodyStyle);
        }

        private void InitializeStyles()
        {
            if (titleStyle != null)
                return;

            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };
            centeredStyle = new GUIStyle(bodyStyle) { alignment = TextAnchor.MiddleCenter };
            completionStyle = new GUIStyle(centeredStyle) { fontSize = 38, fontStyle = FontStyle.Bold };
        }

        private static void Fill(Rect area, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
