using UnityEngine;
namespace DivineSense { public sealed class FollowerCollectible : MonoBehaviour {
 public GameSession Session; bool claimed;
 void OnTriggerEnter2D(Collider2D c){var p=c.GetComponentInParent<PlayerContext>();if(claimed||!p||!p.CanAct||(Session&&p.Session!=Session))return;claimed=true;p.Score.Add(p.Settings.FollowerPickupValue);Destroy(gameObject);}
} }
