using System;
using MicroSense;
using EternalCycle;
using UnityEngine;

namespace DivineSense
{
    internal sealed class ArenaResults : MonoBehaviour
    {
        private const float ArrivalDuration = 1.4f;
        private const float ExitDuration = 0.45f;

        private readonly Contestant[] contestants = new Contestant[2];
        private TrialContinueInput continueInput;
        private Action onComplete;
        private float startedAt;
        private float exitStartedAt = -1f;
        private string verdict;
        private GUIStyle hudScore, nameStyle, scoreStyle, caption, verdictStyle, prompt;

        internal void Play(GameSession session, Action complete)
        {
            for (int i = 0; i < contestants.Length; i++)
                contestants[i] = new Contestant(session.Registry.Players[i], session.World.Camera);

            verdict = contestants[0].Score == contestants[1].Score ? "THE TRIAL ENDS IN A DRAW" : session.Result;
            onComplete = complete;
            startedAt = Time.unscaledTime;
            continueInput.Reset();
        }

        private void Update()
        {
            bool pressed = continueInput.Pressed();
            float now = Time.unscaledTime;
            if (exitStartedAt < 0f)
            {
                if (now - startedAt >= ArrivalDuration && pressed)
                    exitStartedAt = now;
                return;
            }

            if (now - exitStartedAt < ExitDuration)
                return;

            enabled = false;
            onComplete();
            Destroy(this);
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            InitializeStyles();
            Matrix4x4 previousMatrix = ResponsiveGui.ApplyScale(out Vector2 canvas);
            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;
            GUI.depth = -100;

            float elapsed = (Time.unscaledTime - startedAt) / ArrivalDuration;
            float movement = Mathf.SmoothStep(0f, 1f, elapsed);
            float darkness = Mathf.SmoothStep(0f, 1f, elapsed / 0.8f);
            float content = exitStartedAt < 0f ? 1f
                : 1f - Mathf.SmoothStep(0f, 1f, (Time.unscaledTime - exitStartedAt) / ExitDuration);
            GUI.color = new Color(0f, 0f, 0f, darkness);
            GUI.DrawTexture(new Rect(Vector2.zero, canvas), Texture2D.whiteTexture);

            Vector2 center = canvas * 0.5f;
            for (int i = 0; i < contestants.Length; i++)
                DrawContestant(contestants[i], i, canvas, center, movement, content);

            GUI.color = new Color(0.95f, 0.90f, 0.80f, movement * content);
            GUI.Label(new Rect(0f, center.y - 165f, canvas.x, 54f), verdict, verdictStyle);
            GUI.color = new Color(0.53f, 0.76f, 0.89f,
                Mathf.SmoothStep(0f, 1f, (elapsed - 0.75f) / 0.25f) * content);
            GUI.Label(new Rect(0f, canvas.y - 64f, canvas.x, 36f), TrialContinueInput.Prompt, prompt);

            GUI.depth = previousDepth;
            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }

        private void DrawContestant(Contestant contestant, int player, Vector2 canvas, Vector2 center, float movement, float opacity)
        {
            float side = player == 0 ? -1f : 1f;
            Vector2 avatarTarget = center + new Vector2(side * 310f, 12f);
            contestant.DrawAvatar(canvas, avatarTarget, movement, opacity);

            Rect labelRect = HUDController.FollowerLabelRect(player);
            Vector2 hudScale = new Vector2(canvas.x / 1280f, canvas.y / 800f);
            Vector2 scoreTarget = center + new Vector2(side * 140f, 12f);
            Vector2 scoreCenter = Vector2.Lerp(Vector2.Scale(labelRect.center, hudScale), scoreTarget, movement);
            float scoreReveal = Mathf.SmoothStep(0f, 1f, (movement - 0.15f) / 0.65f);

            string name = LocalGamepads.ContestantName(player);
            Color color = HUDController.FollowerColor(player);
            color.a = (1f - scoreReveal) * opacity;
            GUI.color = color;
            // Start in the same coordinate space as the gameplay HUD, then crossfade into the results typography.
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(hudScale.x, hudScale.y, 1f)) * previousMatrix;
            labelRect.center = new Vector2(scoreCenter.x / hudScale.x, scoreCenter.y / hudScale.y);
            GUI.Label(labelRect, name + "   " + contestant.ScoreText + " followers", hudScore);
            GUI.matrix = previousMatrix;

            color.a = scoreReveal * opacity;
            GUI.color = color;
            GUI.Label(Centered(scoreCenter, -54f, 27f), name, nameStyle);
            GUI.Label(Centered(scoreCenter, -30f, 62f), contestant.ScoreText, scoreStyle);
            GUI.color = new Color(0.82f, 0.80f, 0.78f, scoreReveal * opacity);
            GUI.Label(Centered(scoreCenter, 32f, 27f), "FOLLOWERS", caption);
        }

        private static Rect Centered(Vector2 center, float offsetY, float height)
            => new Rect(center.x - 104f, center.y + offsetY, 208f, height);

        private void InitializeStyles()
        {
            if (scoreStyle != null)
                return;

            hudScore = Label(18);
            nameStyle = Label(21, FontStyle.Bold);
            scoreStyle = Label(49, FontStyle.Bold);
            caption = Label(14);
            verdictStyle = Label(30, FontStyle.Bold);
            prompt = Label(17, FontStyle.Bold);
        }

        private static GUIStyle Label(int size, FontStyle weight = FontStyle.Normal)
            => new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = size,
                fontStyle = weight,
                normal = { textColor = Color.white }
            };

        private sealed class Contestant
        {
            internal readonly int Score;
            internal readonly string ScoreText;
            private readonly SpriteLayer[] layers;
            private readonly Vector2 arenaCenter;
            private readonly float arenaHeight;
            private readonly bool wasVisible;

            internal Contestant(PlayerContext player, Camera camera)
            {
                Score = player.Score.Total;
                ScoreText = Score.ToString();
                var renderers = (SpriteRenderer[])player.Visuals.Renderers.Clone();
                Array.Sort(renderers, (a, b) => a.sortingOrder.CompareTo(b.sortingOrder));
                layers = new SpriteLayer[renderers.Length];

                bool alive = player.Life.State == LifeState.Alive;
                Vector2 offset = alive ? Vector2.zero : player.Respawn.Position - (Vector2)player.transform.position;
                Bounds bounds = renderers[0].bounds;
                foreach (SpriteRenderer renderer in renderers)
                    bounds.Encapsulate(renderer.bounds);

                Vector2 center = Project(camera, (Vector2)bounds.center + offset);
                arenaCenter = new Vector2(Mathf.Clamp01(center.x), Mathf.Clamp01(center.y));
                arenaHeight = Project(camera, bounds.min).y - Project(camera, bounds.max).y;
                wasVisible = alive && !player.Status.Hidden && renderers[0].enabled;

                player.Visuals.enabled = false;
                for (int i = 0; i < renderers.Length; i++)
                {
                    SpriteRenderer renderer = renderers[i];
                    Bounds layerBounds = renderer.bounds;
                    Vector2 topLeft = Project(camera, new Vector2(layerBounds.min.x, layerBounds.max.y) + offset);
                    Vector2 bottomRight = Project(camera, new Vector2(layerBounds.max.x, layerBounds.min.y) + offset);
                    Rect relative = new Rect(topLeft - center, bottomRight - topLeft);
                    layers[i] = new SpriteLayer(renderer, relative);
                    renderer.enabled = false;
                }
            }

            internal void DrawAvatar(Vector2 canvas, Vector2 target, float movement, float opacity)
            {
                Vector2 center = Vector2.Lerp(Vector2.Scale(arenaCenter, canvas), target, movement);
                float displayScale = Mathf.Lerp(1f, 148f / (arenaHeight * canvas.y), movement);
                float alpha = opacity * (wasVisible ? 1f : movement);
                foreach (SpriteLayer layer in layers)
                    layer.Draw(center, canvas * displayScale, alpha);
            }

            private static Vector2 Project(Camera camera, Vector3 world)
            {
                Vector3 viewport = camera.WorldToViewportPoint(world);
                return new Vector2(viewport.x, 1f - viewport.y);
            }
        }

        private readonly struct SpriteLayer
        {
            private readonly Texture2D texture;
            private readonly Rect uv;
            private readonly Rect relative;
            private readonly Color tint;

            internal SpriteLayer(SpriteRenderer renderer, Rect relative)
            {
                Sprite sprite = renderer.sprite;
                texture = sprite.texture;
                Rect pixels = sprite.rect;
                uv = new Rect(pixels.x / texture.width, pixels.y / texture.height,
                    pixels.width / texture.width, pixels.height / texture.height);
                if (renderer.flipX) { uv.x += uv.width; uv.width = -uv.width; }
                if (renderer.flipY) { uv.y += uv.height; uv.height = -uv.height; }
                this.relative = relative;
                tint = renderer.color;
            }

            internal void Draw(Vector2 center, Vector2 scale, float opacity)
            {
                Color color = tint;
                color.a *= opacity;
                GUI.color = color;
                GUI.DrawTextureWithTexCoords(new Rect(center + Vector2.Scale(relative.position, scale),
                    Vector2.Scale(relative.size, scale)), texture, uv);
            }
        }
    }
}
