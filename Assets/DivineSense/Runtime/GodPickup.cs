using System;
using UnityEngine;
namespace DivineSense { public sealed class GodPickup : MonoBehaviour {
 public GameSession Session; public GodKind Kind; public Action<PlayerContext> Effect; bool claimed;
 void OnTriggerEnter2D(Collider2D c){var p=c.GetComponent<PlayerContext>();if(claimed||!p||!p.CanAct||!Session.IsPlaying)return;claimed=true;Effect?.Invoke(p);Destroy(gameObject);}
} }