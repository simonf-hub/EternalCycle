using System;
using UnityEngine;
namespace DivineSense { public sealed class PlayerMotor2D : MonoBehaviour {
 public PlayerContext Owner; public Rigidbody2D Body; public BoxCollider2D Collider;
 public Transform BodyVisual; public GlideAbility Glide; public DoubleJumpAbility DoubleJump;
 internal event Action DashStepped, Teleported;
 public bool Grounded {get;private set;} bool extraUsed,jumpUsed; float impulse; Vector2 queued;
 readonly ContactPoint2D[] contacts=new ContactPoint2D[16]; float size=1;
 Vector2 baseColliderSize;
 Vector3 baseVisualScale;
 bool stareLocked;
 RigidbodyConstraints2D constraintsBeforeStare;
 void Awake(){baseColliderSize=Collider.size;baseVisualScale=BodyVisual.localScale;}
 public void Begin(){Glide.ResetAirUse();Body.simulated=true;Body.gravityScale=Owner.Settings.Gravity;}
 void FixedUpdate(){
 if(!Owner.CanAct){Glide.Cancel();DoubleJump.CancelDash();Owner.Input.ClearDashTaps();return;}
 int count=Body.GetContacts(contacts); Grounded=false; float support=0;
 for(int i=0;i<count;i++) if(contacts[i].normal.y>.5f && Body.linearVelocity.y<=Owner.Settings.ScrollSpeed+.5f) {Grounded=true; support=Owner.Session.World.ScrollVelocity;}
 if(Grounded){extraUsed=false;jumpUsed=false;Glide.ResetAirUse();}
 if(Owner.Status.Frozen){
 Owner.Input.DiscardActions();
 return;
 }
 Vector2 v=Body.linearVelocity;
 bool press=Owner.Input.TakeJump();
 if(press && ((!jumpUsed&&Grounded)||DoubleJump.Allowed(Owner,extraUsed))) {
 // Falling off a platform does not consume the first jump in Taste's budget.
 bool secondJump = jumpUsed;
 if(secondJump)extraUsed=true;
 jumpUsed=true;v.y=DoubleJump.JumpVelocity(Owner,secondJump)+Mathf.Max(0,support);Grounded=false;
 }
 float direction=Owner.Input.Horizontal*(Owner.Status.Reversed?-1:1);
 impulse+=queued.x;v.y+=queued.y;queued=Vector2.zero;
 float walkingVelocity=direction*Owner.Settings.MoveSpeed*(Owner.Status.Grown?Owner.Settings.GrowthSlowdown:1);
 v.x=DoubleJump.DashVelocity(Owner,Owner.Input.TakeDash(),walkingVelocity)+impulse;
 impulse=Mathf.MoveTowards(impulse,0,Owner.Settings.KnockbackDecay*Time.fixedDeltaTime);
 Glide.ApplyMotion(Owner,ref v,Grounded,Owner.Input.TakeGlideRelease()); Body.linearVelocity=v;
 SetSize(Owner.Status.Grown?Owner.Settings.GrowthScale:1);
 if(Glide.DashedThisStep||DoubleJump.DashedThisStep)DashStepped?.Invoke();
 if(Owner.Input.TakeAbility())Owner.Abilities.TryActivateActive();
 }
 internal void LockHorizontalForStare()
 {
     float verticalVelocity = Body.linearVelocity.y;
     if (!stareLocked)
     {
         stareLocked = true;
         constraintsBeforeStare = Body.constraints;
         Body.constraints |= RigidbodyConstraints2D.FreezePositionX;
     }

     // Cancelling the glide dash also removes its timed cutoff for upward momentum.
     if (Glide.DashedThisStep) verticalVelocity = Mathf.Min(0f, verticalVelocity);
     Body.linearVelocity = new Vector2(0f, verticalVelocity);
     Body.angularVelocity = 0f;
     impulse = 0f;
     queued = Vector2.zero;
 }
 internal void ReleaseStareLock()
 {
     if (!stareLocked) return;
     stareLocked = false;
     Body.constraints = constraintsBeforeStare;
 }
 public void QueueKnockback(Vector2 delta)
 {
     if (!stareLocked) queued += delta;
 }
 public void Teleport(Vector2 position, bool resetVelocity)
 {
     // A non-simulated body is re-created from its Transform when simulation resumes.
     // Set both poses and reset interpolation so neither can restore the death position.
     RigidbodyInterpolation2D interpolation = Body.interpolation;
     Body.interpolation = RigidbodyInterpolation2D.None;
     SetBodyPosition(position);
     Body.interpolation = interpolation;
     Grounded = false;
     Glide.Cancel();
     DoubleJump.CancelDash();
     Owner.Input.ClearDashTaps();
     if (resetVelocity)
     {
         Body.linearVelocity = Vector2.zero;
         impulse = 0;
         queued = Vector2.zero;
         extraUsed = jumpUsed = false;
     }
     Teleported?.Invoke();
 }
 void SetBodyPosition(Vector2 position)
 {
     transform.position = new Vector3(position.x, position.y, transform.position.z);
     Body.position = position;
 }
 public void SetSize(float scale)
 {
     if (Mathf.Approximately(size, scale)) return;
     float foot = Body.position.y - Collider.size.y * .5f;
     size = scale;
     Collider.size = baseColliderSize * scale;
     BodyVisual.localScale = baseVisualScale * scale;
     Body.position = new Vector2(Body.position.x, foot + Collider.size.y * .5f);
     if (scale > 1) Body.position = ClearPosition(Body.position);
 }
 public Vector2 ClearPosition(Vector2 requested){
 var bounds=Owner.Session.World.VisibleBounds; Vector2 half=Collider.size*.5f;
 for(int i=0;i<100;i++){
 Vector2 offset=i==0?Vector2.zero:new Vector2(Mathf.Cos(i*2.4f),Mathf.Sin(i*2.4f))*Mathf.Sqrt(i)*.22f;
 Vector2 p=requested+offset;p.x=Mathf.Clamp(p.x,bounds.xMin+half.x,bounds.xMax-half.x);p.y=Mathf.Clamp(p.y,bounds.yMin+half.y,bounds.yMax-half.y);
 if(!Physics2D.OverlapBox(p,Collider.size*.95f,0,1<<9))return p;
 }
 return bounds.center;
 }
 public void Freeze(){Glide.Cancel();DoubleJump.CancelDash();Body.linearVelocity=Vector2.zero;Body.simulated=false;SetSize(1);}
} }
