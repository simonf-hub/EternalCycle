using UnityEngine;
namespace DivineSense { public sealed class ReverseControlsEffect : MonoBehaviour {
 public void Apply(PlayerContext p){if(p.CanAct)p.Status.ReversedUntil=p.Session.Now+p.Settings.ReverseControlsDuration;}
} }