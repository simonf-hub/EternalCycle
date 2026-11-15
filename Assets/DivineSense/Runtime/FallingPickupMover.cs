using UnityEngine;
namespace DivineSense { public sealed class FallingPickupMover : MonoBehaviour {
 public GodPickup Pickup; public Rigidbody2D Body;
 void FixedUpdate(){if(Pickup.Session&&Pickup.Session.IsPlaying)Body.MovePosition(Body.position+Vector2.down*Pickup.Session.Settings.PickupFallSpeed*Time.fixedDeltaTime);}
 void Update(){if(Pickup.Session&&transform.position.y<Pickup.Session.World.VisibleBounds.yMin-1)Destroy(gameObject);}
} }