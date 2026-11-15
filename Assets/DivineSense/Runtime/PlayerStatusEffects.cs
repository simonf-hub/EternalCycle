using UnityEngine;
namespace DivineSense { public sealed class PlayerStatusEffects : MonoBehaviour {
 public PlayerContext Owner; public float HiddenUntil,GrownUntil,ReversedUntil;
 public bool Hidden=>Owner.Session.Now<HiddenUntil;
 public bool Grown=>Owner.Session.Now<GrownUntil;
 public bool Reversed=>Owner.Session.Now<ReversedUntil;
 public void Clear(){HiddenUntil=GrownUntil=ReversedUntil=0;}
} }