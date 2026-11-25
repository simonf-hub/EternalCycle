using System;
using System.Collections.Generic;
using MicroSense;
using UnityEngine;

namespace DivineSense
{
    internal sealed class NarrativeSequence : MonoBehaviour
    {
        private const float FadeDuration = 0.45f;
        private const float DialogueInset = 32f;
        private const float DialogueGap = 12f;

        private static readonly Color Ivory = new Color(0.95f, 0.90f, 0.80f);
        private static readonly Color Gold = new Color(0.89f, 0.72f, 0.51f);
        private static readonly Color Muted = new Color(0.75f, 0.77f, 0.82f);
        private static readonly Color PromptColor = new Color(0.61f, 0.79f, 0.89f);

        private IReadOnlyList<NarrativePage> pages;
        private ArenaTutorial tutorial;
        private Texture2D controllerGuide;
        private Action onComplete;
        private int pageIndex;
        private TrialContinueInput continueInput;
        private float fadeStartedAt;
        private bool fadeIn;

        private Font storyFont;
        private GUIStyle storyStyle, dialogueStyle, directionStyle, speakerStyle, promptStyle;

        internal bool IsShowingControllerGuide => controllerGuide != null;
        private int PageCount => pages.Count + (tutorial == null ? 0 : ArenaTutorial.PageCount);
        private float Opacity => fadeIn
            ? Mathf.SmoothStep(0f, 1f, (Time.unscaledTime - fadeStartedAt) / FadeDuration)
            : 1f;

        internal void Play(IReadOnlyList<NarrativePage> pages, Action complete, bool fadeIn = false,
            ArenaTutorial tutorial = null, Texture2D controllerGuide = null)
        {
            this.pages = pages ?? Array.Empty<NarrativePage>();
            this.fadeIn = fadeIn;
            this.tutorial = tutorial;
            this.controllerGuide = controllerGuide;
            onComplete = complete;
            pageIndex = 0;
            fadeStartedAt = Time.unscaledTime;
            continueInput.Reset();

            if (PageCount == 0)
                Finish();
        }

        internal void Play(bool afterArena, Action complete, ArenaTutorial tutorial)
        {
            var story = Resources.Load<TrialStory>("TrialStory");
            Play(afterArena ? story?.AthenaTransition : story?.Opening, complete, afterArena, tutorial);
        }

        private void Update()
        {
            if (onComplete == null)
                return;

            bool pressed = continueInput.Pressed();
            if (Opacity < 1f || !pressed)
                return;

            if (IsShowingControllerGuide)
            {
                controllerGuide = null;
                return;
            }

            pageIndex++;
            if (pageIndex >= PageCount)
                Finish();
        }

        private void Finish()
        {
            Action complete = onComplete;
            onComplete = null;
            enabled = false;
            Destroy(this);
            complete?.Invoke();
        }

        private void OnGUI()
        {
            if (onComplete == null || Event.current.type != EventType.Repaint)
                return;

            InitializeStyles();
            Matrix4x4 previousMatrix = ResponsiveGui.ApplyScale(out Vector2 canvas);
            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;
            GUI.depth = -100;

            float opacity = Opacity;
            GUI.color = new Color(0.034f, 0.041f, 0.061f);
            GUI.DrawTexture(new Rect(0f, 0f, canvas.x, canvas.y), Texture2D.whiteTexture);

            if (IsShowingControllerGuide)
            {
                const float margin = 32f;
                var imageArea = new Rect(margin, margin, canvas.x - 2f * margin, canvas.y - 120f);
                GUI.color = WithOpacity(Color.white, opacity);
                GUI.DrawTexture(imageArea, controllerGuide, ScaleMode.ScaleToFit);
            }
            else if (pageIndex >= pages.Count)
                tutorial.Draw(pageIndex - pages.Count, canvas);
            else
                DrawPage(pages[pageIndex], canvas, opacity);

            GUI.color = WithOpacity(PromptColor, opacity);
            GUI.Label(new Rect(0f, canvas.y - 65f, canvas.x, 34f), TrialContinueInput.Prompt, promptStyle);

            GUI.depth = previousDepth;
            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }

        private void DrawPage(NarrativePage page, Vector2 canvas, float opacity)
        {
            Vector2 center = canvas * 0.5f + Vector2.up * 16f;
            if (page.Dialogue.Count > 0)
            {
                DrawDialogue(page.Dialogue, center, opacity);
                return;
            }

            const float width = 800f;
            var paragraph = new GUIContent(page.Narration);
            float height = storyStyle.CalcHeight(paragraph, width);
            GUI.color = WithOpacity(Ivory, opacity);
            GUI.Label(new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height),
                paragraph, storyStyle);
        }

        private void DrawDialogue(IReadOnlyList<NarrativeLine> lines, Vector2 center, float opacity)
        {
            const float textWidth = 850f;
            const float speakerHeight = 23f;
            float contentHeight = (lines.Count - 1) * DialogueGap;
            foreach (NarrativeLine line in lines)
            {
                bool hasSpeaker = !string.IsNullOrWhiteSpace(line.Speaker);
                GUIStyle style = hasSpeaker ? dialogueStyle : directionStyle;
                contentHeight += (hasSpeaker ? speakerHeight : 0f)
                    + style.CalcHeight(new GUIContent(line.Text), textWidth);
            }

            var panel = new Rect(center.x - textWidth * 0.5f - DialogueInset,
                center.y - contentHeight * 0.5f - DialogueInset,
                textWidth + 2f * DialogueInset, contentHeight + 2f * DialogueInset);
            GUI.color = new Color(0.078f, 0.093f, 0.13f, opacity);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = WithOpacity(Gold, opacity);
            GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 2f), Texture2D.whiteTexture);

            float x = panel.x + DialogueInset;
            float y = panel.y + DialogueInset;
            foreach (NarrativeLine line in lines)
            {
                bool hasSpeaker = !string.IsNullOrWhiteSpace(line.Speaker);
                if (hasSpeaker)
                {
                    GUI.color = WithOpacity(Gold, opacity);
                    GUI.Label(new Rect(x, y, textWidth, speakerHeight), line.Speaker, speakerStyle);
                    y += speakerHeight;
                }

                var content = new GUIContent(line.Text);
                GUIStyle style = hasSpeaker ? dialogueStyle : directionStyle;
                float height = style.CalcHeight(content, textWidth);
                GUI.color = WithOpacity(hasSpeaker ? Ivory : Muted, opacity);
                GUI.Label(new Rect(x, y, textWidth, height), content, style);
                y += height + DialogueGap;
            }
        }

        private void InitializeStyles()
        {
            if (storyStyle != null)
                return;

            storyFont = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Times New Roman", "DejaVu Serif" }, 29);
            storyStyle = Label(29);
            if (storyFont)
                storyStyle.font = storyFont;

            dialogueStyle = Label(22);
            directionStyle = Label(20, FontStyle.Italic);
            speakerStyle = Label(16, FontStyle.Bold);
            promptStyle = Label(17, FontStyle.Bold);
            promptStyle.alignment = TextAnchor.MiddleCenter;
        }

        private static GUIStyle Label(int size, FontStyle weight = FontStyle.Normal)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = weight,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = Color.white }
            };
        }

        private static Color WithOpacity(Color color, float opacity)
        {
            color.a = opacity;
            return color;
        }

        private void OnDestroy()
        {
            if (storyFont)
                Destroy(storyFont);
        }
    }
}
