using System;
using System.Collections.Generic;
using UnityEngine;

namespace DivineSense
{
    [CreateAssetMenu(menuName = "Divine-Sense/Trial Story")]
    public sealed class TrialStory : ScriptableObject
    {
        [SerializeField] private NarrativePage[] opening = Array.Empty<NarrativePage>();
        [SerializeField] private NarrativePage[] zeusTransition = Array.Empty<NarrativePage>();
        [SerializeField] private NarrativePage[] athenaTransition = Array.Empty<NarrativePage>();

        public IReadOnlyList<NarrativePage> Opening => opening;
        public IReadOnlyList<NarrativePage> ZeusTransition => zeusTransition;
        public IReadOnlyList<NarrativePage> AthenaTransition => athenaTransition;
    }

    [Serializable]
    public sealed class NarrativePage
    {
        [Tooltip("Use to display a standalone narration page. Dialogue pages take priority when both are filled.")]
        [SerializeField, TextArea(3, 10)] private string narration;
        [SerializeField] private NarrativeLine[] dialogue = Array.Empty<NarrativeLine>();

        internal string Narration => narration;
        internal IReadOnlyList<NarrativeLine> Dialogue => dialogue;
    }

    [Serializable]
    public sealed class NarrativeLine
    {
        [Tooltip("Leave blank for a stage direction.")]
        [SerializeField] private string speaker;
        [SerializeField, TextArea(2, 6)] private string text;

        internal string Speaker => speaker;
        internal string Text => text;
    }
}
