using UnityEngine;
namespace DivineSense { public sealed class SightAbility : MonoBehaviour {
 public void Activate(PlayerContext p){var t=p.Registry.Opponent(p);if(t.CanAct&&!t.Status.Grown&&Vector2.Distance(p.Endpoint,t.Endpoint)<=p.Settings.SightRadius)t.Status.HiddenUntil=p.Session.Now+p.Settings.SightDuration;}
} }