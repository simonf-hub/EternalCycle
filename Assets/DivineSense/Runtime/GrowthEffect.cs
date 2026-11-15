using UnityEngine;
namespace DivineSense { public sealed class GrowthEffect : MonoBehaviour {
 public void Apply(PlayerContext p){if(p.CanAct)p.Status.GrownUntil=p.Session.Now+p.Settings.GrowthDuration;}
} }