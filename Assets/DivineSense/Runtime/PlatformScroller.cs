using UnityEngine;
namespace DivineSense { public sealed class PlatformScroller : MonoBehaviour {
 public GameSession Session; public Rigidbody2D Body;
 void FixedUpdate(){if(Session&&Session.IsPlaying)Body.MovePosition(Body.position+Vector2.up*Session.World.ScrollVelocity*Time.fixedDeltaTime);}
 void Update(){if(!Session||!Session.IsPlaying)return;var r=Session.World.VisibleBounds;if(transform.position.y<r.yMin-Session.Settings.CullMargin||transform.position.y>r.yMax+Session.Settings.CullMargin)Destroy(gameObject);}
} }