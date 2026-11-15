using UnityEngine;
using UnityEngine.InputSystem;
namespace DivineSense { public sealed class PlayerInputReader : MonoBehaviour {
 public PlayerContext Owner;
 InputActionMap gameplay,respawn; InputAction move,jump,ability,marker;
 bool jumpPending,abilityPending,requireRelease=true;
 public float Horizontal => Owner.CanAct ? move.ReadValue<float>() : 0;
 public bool Held => !requireRelease && jump.IsPressed();
 public Vector2 Marker => marker.ReadValue<Vector2>();
 void Awake() {
 bool first=Owner.Id==0; string l=first?"a":"leftArrow",r=first?"d":"rightArrow",u=first?"w":"upArrow",d=first?"s":"downArrow";
 gameplay=new InputActionMap(first?"P1Gameplay":"P2Gameplay");
 move=gameplay.AddAction("MoveX",InputActionType.Value);
 move.AddCompositeBinding("1DAxis").With("Negative","<Keyboard>/"+l).With("Positive","<Keyboard>/"+r);
 jump=gameplay.AddAction("Jump",InputActionType.Button,"<Keyboard>/"+u);
 ability=gameplay.AddAction("Ability",InputActionType.Button,"<Keyboard>/"+d);
 respawn=new InputActionMap(first?"P1Respawn":"P2Respawn");marker=respawn.AddAction("MoveMarker",InputActionType.Value);
 marker.AddCompositeBinding("2DVector").With("Left","<Keyboard>/"+l).With("Right","<Keyboard>/"+r).With("Up","<Keyboard>/"+u).With("Down","<Keyboard>/"+d);
 gameplay.Enable();
 }
 void Update() {
 if(!Owner.CanAct)return;
 if(requireRelease) { if(!jump.IsPressed()&&!ability.IsPressed())requireRelease=false; return; }
 jumpPending|=jump.WasPressedThisFrame(); abilityPending|=ability.WasPressedThisFrame();
 }
 public bool TakeJump(){bool v=jumpPending;jumpPending=false;return v;}
 public bool TakeAbility(){bool v=abilityPending;abilityPending=false;return v;}
 public void Placement(bool value) {jumpPending=abilityPending=false;requireRelease=true; if(value){gameplay.Disable();respawn.Enable();}else{respawn.Disable();gameplay.Enable();}}
 void OnDestroy(){gameplay?.Dispose();respawn?.Dispose();}
} }