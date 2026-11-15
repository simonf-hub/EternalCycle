using UnityEngine;
namespace DivineSense { public sealed class PlayerMotor2D : MonoBehaviour {
 public PlayerContext Owner; public Rigidbody2D Body; public BoxCollider2D Collider;
 public Transform BodyVisual; public GlideAbility Glide; public DoubleJumpAbility DoubleJump;
 public bool Grounded {get;private set;} bool extraUsed,jumpUsed; float impulse; Vector2 queued;
 readonly ContactPoint2D[] contacts=new ContactPoint2D[16]; float size=1;
 public void Begin(){Body.simulated=true;Body.gravityScale=Owner.Settings.Gravity;}
 void FixedUpdate(){
 if(!Owner.CanAct)return;
 int count=Body.GetContacts(contacts); Grounded=false; float support=0;
 for(int i=0;i<count;i++) if(contacts[i].normal.y>.5f && Body.linearVelocity.y<=Owner.Settings.ScrollSpeed+.5f) {Grounded=true; support=Owner.Session.World.ScrollVelocity;}
 if(Grounded){extraUsed=false;jumpUsed=false;}
 Vector2 v=Body.linearVelocity;
 bool press=Owner.Input.TakeJump();
 if(press && ((!jumpUsed&&Grounded)||DoubleJump.Allowed(Owner,extraUsed))) {
 bool airborne = !Grounded;
 if(airborne)extraUsed=true; jumpUsed=true;v.y=DoubleJump.JumpVelocity(Owner,airborne)+Mathf.Max(0,support);Grounded=false;
 }
 float direction=Owner.Input.Horizontal*(Owner.Status.Reversed?-1:1);
 impulse+=queued.x;v.y+=queued.y;queued=Vector2.zero;
 v.x=direction*Owner.Settings.MoveSpeed*(Owner.Status.Grown?Owner.Settings.GrowthSlowdown:1)+impulse;
 impulse=Mathf.MoveTowards(impulse,0,Owner.Settings.KnockbackDecay*Time.fixedDeltaTime);
 v.y=Glide.Limit(Owner,v.y); Body.linearVelocity=v;
 SetSize(Owner.Status.Grown?Owner.Settings.GrowthScale:1);
 if(Owner.Input.TakeAbility())Owner.Abilities.TryActivateActive();
 }
 public void QueueKnockback(Vector2 delta){queued+=delta;}
 public void Teleport(Vector2 position, bool resetVelocity)
 {
     // A non-simulated body is re-created from its Transform when simulation resumes.
     // Set both poses and reset interpolation so neither can restore the death position.
     RigidbodyInterpolation2D interpolation = Body.interpolation;
     Body.interpolation = RigidbodyInterpolation2D.None;
     transform.position = new Vector3(position.x, position.y, transform.position.z);
     Body.position = position;
     Body.interpolation = interpolation;
     Grounded = false;
     if (resetVelocity)
     {
         Body.linearVelocity = Vector2.zero;
         impulse = 0;
         queued = Vector2.zero;
         extraUsed = jumpUsed = false;
     }
 }
 public void SetSize(float scale){if(Mathf.Approximately(size,scale))return;float foot=Body.position.y-Collider.size.y*.5f;size=scale;Collider.size=new Vector2(.7f,1)*scale;BodyVisual.localScale=Vector3.one*scale;Body.position=new Vector2(Body.position.x,foot+Collider.size.y*.5f);if(scale>1)Body.position=ClearPosition(Body.position);}
 public Vector2 ClearPosition(Vector2 requested){
 var bounds=Owner.Session.World.VisibleBounds; Vector2 half=Collider.size*.5f;
 for(int i=0;i<100;i++){
 Vector2 offset=i==0?Vector2.zero:new Vector2(Mathf.Cos(i*2.4f),Mathf.Sin(i*2.4f))*Mathf.Sqrt(i)*.22f;
 Vector2 p=requested+offset;p.x=Mathf.Clamp(p.x,bounds.xMin+half.x,bounds.xMax-half.x);p.y=Mathf.Clamp(p.y,bounds.yMin+half.y,bounds.yMax-half.y);
 if(!Physics2D.OverlapBox(p,Collider.size*.95f,0,1<<9))return p;
 }
 return bounds.center;
 }
 public void Freeze(){Body.linearVelocity=Vector2.zero;Body.simulated=false;SetSize(1);}
} }