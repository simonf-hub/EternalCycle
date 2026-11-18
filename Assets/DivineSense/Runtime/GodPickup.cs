using System;
using UnityEngine;
namespace DivineSense { public sealed class GodPickup : MonoBehaviour {
 public GameSession Session; public GodKind Kind; public Action<PlayerContext> Effect; bool claimed;
 public static string DisplayName(GodKind kind) => kind switch {
 GodKind.Hermes => "Hades’ Position",
 GodKind.Demeter => "Hermes’ Agility",
 GodKind.Athena => "Medusa’s Swap",
 GodKind.Dionysus => "Eris’ Discord",
 GodKind.Poseidon => "Aphrodite’s Pull",
 GodKind.Ares => "Medusa’s Stare",
 _ => kind.ToString()
 };
 public static Color PickupColor(GodKind kind) => kind switch {
 GodKind.Hermes => new Color(.65f,.35f,1f),
 GodKind.Demeter => new Color(1f,.8f,.15f),
 GodKind.Athena => new Color(.2f,.8f,1f),
 GodKind.Dionysus => new Color(1f,.25f,.2f),
 GodKind.Poseidon => new Color(1f,.4f,.75f),
 GodKind.Ares => new Color(.3f,1f,.45f),
 _ => Color.white
 };
 public void RefreshAppearance(){
 gameObject.name=DisplayName(Kind);
 var label=GetComponentInChildren<TextMesh>();
 if(label)label.text=DisplayName(Kind);
 var sprite=GetComponent<SpriteRenderer>();
 if(sprite)sprite.color=PickupColor(Kind);
 }
 void OnTriggerEnter2D(Collider2D c){var p=c.GetComponent<PlayerContext>();if(claimed||!p||!p.CanAct||!Session.IsPlaying)return;claimed=true;Effect?.Invoke(p);Destroy(gameObject);}
} }