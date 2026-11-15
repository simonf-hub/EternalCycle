using UnityEngine;
namespace DivineSense { public sealed class FollowerCollectible : MonoBehaviour {
 public GameSession Session; bool claimed;
 void OnTriggerEnter2D(Collider2D c){var p=c.GetComponent<PlayerContext>();if(claimed||!p||!p.CanAct||!Session.IsPlaying)return;claimed=true;p.Score.Add(Session.Settings.FollowerPickupValue);Destroy(gameObject);}
} }