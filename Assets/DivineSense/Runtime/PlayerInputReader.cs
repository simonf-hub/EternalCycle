using System.Collections.Generic;
using EternalCycle;
using UnityEngine;
using UnityEngine.InputSystem;
namespace DivineSense { public sealed class PlayerInputReader : MonoBehaviour {
 public PlayerContext Owner;
 InputActionMap gameplay,respawn;
 // Resolve from the map so device rebinding and editor reloads cannot leave stale action copies.
 InputAction Move=>gameplay.FindAction("MoveX",true);
 InputAction Jump=>gameplay.FindAction("Jump",true);
 InputAction Ability=>gameplay.FindAction("Ability",true);
 InputAction MarkerAction=>respawn.FindAction("MoveMarker",true);
 LocalController assignedPad; Keyboard assignedKeyboard;
 bool jumpPending,abilityPending,glideReleasePending,requireRelease=true;
 const float DashPressThreshold = .65f, DashReleaseThreshold = .3f;
 int heldDashDirection, lastDashTap, pendingDashDirection;
 double lastDashTapTime;
 public float Horizontal => Owner.CanAct && !Owner.Status.Frozen ? Move.ReadValue<float>() : 0;
 public bool Held => Owner.CanAct && !Owner.Status.Frozen && !requireRelease && Jump.IsPressed();
 public Vector2 Marker => MarkerAction.ReadValue<Vector2>();
 void Awake() {
 bool first=Owner.Id==0; string l=first?"a":"leftArrow",r=first?"d":"rightArrow",u=first?"w":"upArrow",d=first?"s":"downArrow";
 gameplay=new InputActionMap(first?"P1Gameplay":"P2Gameplay");
 var move=gameplay.AddAction("MoveX",InputActionType.Value);
 move.AddCompositeBinding("1DAxis").With("Negative","<Keyboard>/"+l).With("Positive","<Keyboard>/"+r);
 var jump=gameplay.AddAction("Jump",InputActionType.Button,"<Keyboard>/"+u);
 var ability=gameplay.AddAction("Ability",InputActionType.Button,"<Keyboard>/"+d);
 respawn=new InputActionMap(first?"P1Respawn":"P2Respawn");var marker=respawn.AddAction("MoveMarker",InputActionType.Value);
 marker.AddCompositeBinding("2DVector").With("Left","<Keyboard>/"+l).With("Right","<Keyboard>/"+r).With("Up","<Keyboard>/"+u).With("Down","<Keyboard>/"+d);
 move.AddBinding("<Gamepad>/leftStick/x").WithProcessor("axisDeadzone");
 move.AddBinding("<Joystick>/stick/x").WithProcessor("axisDeadzone");
 jump.AddBinding("<Joystick>/trigger");
 ability.AddBinding("<Joystick>/button2");
 marker.AddBinding("<Joystick>/stick");
 jump.AddBinding("<Gamepad>/buttonSouth");
 ability.AddBinding("<Gamepad>/buttonEast");
 marker.AddBinding("<Gamepad>/leftStick");
 if(!Owner.Session||Owner.Session.Trial==TrialLevel.Prototype){
 move.AddBinding("<Gamepad>/dpad/x");
 move.AddBinding("<Joystick>/hat/x");
 marker.AddBinding("<Joystick>/hat");
 marker.AddBinding("<Gamepad>/dpad");
 }
 move.performed += ReadDashTap;
 move.canceled += ReadDashTap;
 RefreshDevices();
 InputSystem.onDeviceChange+=DeviceChanged;
 gameplay.Enable();
 }
 void DeviceChanged(InputDevice device,InputDeviceChange change){RefreshDevices();}
 void RefreshDevices(){
 assignedPad=LocalGamepads.ForPlayer(Owner.Id);assignedKeyboard=Keyboard.current;
 var devices=new List<InputDevice>();
 if(assignedKeyboard!=null)devices.Add(assignedKeyboard);
 if(assignedPad!=null)devices.Add(assignedPad.Device);
 // An empty explicit device list prevents an unassigned reader using every gamepad.
 gameplay.devices=devices.ToArray();respawn.devices=devices.ToArray();
 DiscardActions();
 }
 void Update() {
 if(assignedPad!=LocalGamepads.ForPlayer(Owner.Id)||assignedKeyboard!=Keyboard.current)RefreshDevices();
 if(!Owner.CanAct)return;
 if(Owner.Status.Frozen){DiscardActions();return;}
 if(requireRelease) { if(!Jump.IsPressed()&&!Ability.IsPressed())requireRelease=false; return; }
 glideReleasePending|=Jump.WasReleasedThisFrame();
 jumpPending|=Jump.WasPressedThisFrame(); abilityPending|=Ability.WasPressedThisFrame();
 }
 void ReadDashTap(InputAction.CallbackContext context)
 {
     float horizontal = context.ReadValue<float>();
     if (Mathf.Abs(horizontal) <= DashReleaseThreshold)
         heldDashDirection = 0;
     if (Mathf.Abs(horizontal) < DashPressThreshold)
         return;

     int direction = horizontal > 0 ? 1 : -1;
     if (direction == heldDashDirection)
         return;
     heldDashDirection = direction;
     if (!Owner.CanAct || Owner.Status.Frozen || Owner.EquippedLoadout.MovementKind != MovementKind.Taste)
         return;

     if (direction == lastDashTap && context.time - lastDashTapTime <= Owner.Settings.TasteDashTapWindow)
     {
         pendingDashDirection = direction;
         lastDashTap = 0;
         return;
     }
     lastDashTap = direction;
     lastDashTapTime = context.time;
 }

 public int TakeDash()
 {
     int direction = pendingDashDirection;
     pendingDashDirection = 0;
     return direction;
 }

 public void ClearDashTaps() => lastDashTap = pendingDashDirection = 0;

 public void DiscardActions()
 {
     if (Owner && Owner.Motor)
     {
         if (Owner.Motor.Glide) Owner.Motor.Glide.Cancel();
         if (Owner.Motor.DoubleJump) Owner.Motor.DoubleJump.CancelDash();
     }
     ClearDashTaps();
     jumpPending = abilityPending = glideReleasePending = false;
     requireRelease = true;
 }
 public bool TakeGlideRelease(){bool value=glideReleasePending||(!requireRelease&&Jump.WasReleasedThisFrame());glideReleasePending=false;return value;}
 public bool TakeJump(){bool v=jumpPending;jumpPending=false;return v;}
 public bool TakeAbility(){bool v=abilityPending;abilityPending=false;return v;}
 public void Placement(bool value) {ClearDashTaps();jumpPending=abilityPending=glideReleasePending=false;requireRelease=true; if(value){gameplay.Disable();respawn.Enable();}else{respawn.Disable();gameplay.Enable();}}
 void OnDestroy(){InputSystem.onDeviceChange-=DeviceChanged;gameplay?.Dispose();respawn?.Dispose();}
} }
