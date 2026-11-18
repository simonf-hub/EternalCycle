using System;
using MicroSense;
using UnityEngine;

namespace DivineSense
{
    internal sealed class NarrativeSequence : MonoBehaviour
    {
        private const float FadeDuration = 0.45f;

        private static readonly string[] Opening =
        {
            "For centuries, the gods of Olympus have watched over the mortal world, witnessing countless wars, triumphs, and tragedies. But after thousands of years, even the most extraordinary mortal achievements have become painfully predictable. Seeking a new source of entertainment, the gods establish the Divine Colosseum, a grand arena suspended between Olympus and the Underworld, where mortals compete for divine favour and the possibility of ascending to godhood.",
            "To select their contestants, each god nominates a mortal who has caught their attention. However, the Council quickly discovers that its members have vastly different ideas of what makes someone worthy of divinity. Unable to agree on whose nominee is most deserving, the gods decide to settle the argument through a series of trials. Two mortals are chosen to enter the arena, each determined to prove themselves worthy of a place among the gods.",
            "Throughout the competition, the contestants will encounter divine blessings that grant them extraordinary abilities associated with senses such as Sight, Sound, Touch, and Taste. These powers are scattered throughout the trials, allowing the mortals to gradually acquire new abilities as they progress. The gods present these blessings as opportunities to demonstrate their potential, although they seem just as interested in seeing how the contestants use their newfound powers against each other.",
            "Each god oversees a different trial, imposing their own interpretation of divine worthiness upon the contestants. Zeus, King of Olympus, takes the first turn. Convinced that a true god must command admiration and loyalty, he challenges the mortals to win over the crowd and gather as many followers as possible. With the promise of divinity hanging over their heads, the contestants prepare to face each other in their first trial."
        };

        private static readonly string[] Interlude =
        {
            "The first trial concludes, leaving the contestants exhausted but eager to prove themselves worthy of divine recognition. Zeus is impressed by their ambition and determination to win over the crowd, declaring the competition a tremendous success. However, not everyone on the Council shares his enthusiasm.",
            "Athena, goddess of wisdom and strategy, questions whether popularity and individual ambition are enough to determine someone's worthiness. Throughout the first trial, the contestants have begun acquiring divine abilities, each developing different strengths. Yet so far, they've only been tested on how effectively they can use those powers to outperform one another.",
            "Athena proposes a different kind of challenge, one that requires the contestants to recognize each other's strengths and combine their abilities to succeed. Under her direction, the arena transforms into a series of obstacles that neither contestant can overcome entirely alone. Despite having spent the previous trial competing against each other, the two must now cooperate to progress.",
            "However, their rivalry is far from over. Each contestant still seeks the gods' favour, and opportunities to outshine or betray their rival remain. As the next trial begins, both must decide when to trust each other and when to pursue their own ambitions."
        };

        private string[] pages;
        private ArenaTutorial tutorial;
        private Action onComplete;
        private bool afterArena;
        private int pageIndex;
        private TrialContinueInput continueInput;
        private float fadeStartedAt;
        private float Opacity => afterArena
            ? Mathf.SmoothStep(0f, 1f, (Time.unscaledTime - fadeStartedAt) / FadeDuration)
            : 1f;

        private Font storyFont;
        private GUIStyle storyStyle;
        private GUIStyle promptStyle;

        internal void Play(bool afterArena, Action complete, ArenaTutorial tutorial)
        {
            this.afterArena = afterArena;
            this.tutorial = tutorial;
            pages = afterArena ? Interlude : Opening;
            onComplete = complete;
            fadeStartedAt = Time.unscaledTime;
            continueInput.Reset();
        }

        private void Update()
        {
            bool pressed = continueInput.Pressed();

            if (onComplete == null || Opacity < 1f || !pressed)
                return;

            if (++pageIndex < pages.Length + (tutorial == null ? 0 : ArenaTutorial.PageCount))
                return;

            Action complete = onComplete;
            onComplete = null;
            if (!afterArena)
            {
                enabled = false;
                Destroy(this);
            }
            complete();
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Matrix4x4 previousMatrix = ResponsiveGui.ApplyScale(out Vector2 canvas);
            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;
            GUI.depth = -100;
            float opacity = Opacity;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0f, 0f, canvas.x, canvas.y), Texture2D.whiteTexture);

            if (onComplete != null)
            {
                InitializeStyles();
                GUI.color = new Color(1f, 1f, 1f, opacity);
                if (pageIndex < pages.Length)
                {
                    const float textWidth = 780f;
                    var paragraph = new GUIContent(pages[pageIndex]);
                    float height = storyStyle.CalcHeight(paragraph, textWidth);
                    GUI.Label(new Rect((canvas.x - textWidth) / 2f, (canvas.y - height) / 2f, textWidth, height),
                        paragraph, storyStyle);
                }
                else
                {
                    tutorial.Draw(pageIndex - pages.Length, canvas);
                    GUI.color = Color.white;
                }
                GUI.Label(new Rect(0f, canvas.y - 64f, canvas.x, 36f),
                    TrialContinueInput.Prompt, promptStyle);
            }

            GUI.depth = previousDepth;
            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }

        private void InitializeStyles()
        {
            if (storyStyle != null)
                return;

            storyFont = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Times New Roman", "DejaVu Serif" }, 29);
            storyStyle = new GUIStyle(GUI.skin.label)
            {
                font = storyFont,
                fontSize = 29,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                normal = { textColor = new Color(0.95f, 0.90f, 0.80f) }
            };
            promptStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.53f, 0.76f, 0.89f) }
            };
        }

        private void OnDestroy()
        {
            if (storyFont != null)
                Destroy(storyFont);
        }
    }
}
