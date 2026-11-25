using System;
using DivineSense;
using EternalCycle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace DivineSenseEditor
{
    public static class ControllerValidation
    {
        [InitializeOnLoadMethod] private static void Resume()
        {
            if (SessionState.GetBool("ControllerValidation", false))
            {
                // Configure headless routing before scene Awake enables gameplay maps.
                InputSystem.settings = UnityEngine.Object.Instantiate(InputSystem.settings);
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                EditorApplication.update += Attach;
            }
        }
        public static void Run()
        {
            SessionState.SetBool("ControllerValidation", true);
            EditorSceneManager.OpenScene("Assets/Scenes/Archive/DivineSense_Arena.unity");
            EditorApplication.isPlaying = true;
        }
        private static void Attach()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            EditorApplication.update -= Attach;
            new GameObject("Controller Validation").AddComponent<ControllerValidationRunner>();
        }
    }

    public sealed class ControllerValidationRunner : MonoBehaviour
    {
        private Gamepad first, second;
        private Keyboard keyboard;
        private PlayerContext a, b;
        private int stage = -3, frame;
        private GameSession session;
        private HUDController hud;
        private double started;
        private void Start()
        {
            started = EditorApplication.timeSinceStartup;
            keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            foreach(var connected in Gamepad.all)InputSystem.DisableDevice(connected);
            first = InputSystem.AddDevice<Gamepad>(); second = InputSystem.AddDevice<Gamepad>();
            session = FindFirstObjectByType<GameSession>();
            hud = FindFirstObjectByType<HUDController>();
            a = session.Registry.Players[0]; b = session.Registry.Players[1];
            foreach (var p in session.Registry.Players) { p.Motor.enabled = false; p.Motor.Body.simulated = false; p.Life.enabled = false; }
            FindFirstObjectByType<GodDirector>().enabled = false;
        }
        private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        private void LateUpdate()
        {
            try
            {
                if (EditorApplication.timeSinceStartup - started > 30) throw new Exception("Controller test timed out");
                if (++frame % 4 != 0) return;
                switch (stage++)
                {
                    case -3:
                        InputSystem.QueueStateEvent(second,new GamepadState().WithButton(GamepadButton.DpadRight).WithButton(GamepadButton.DpadUp)); break;
                    case -2:
                        InputSystem.QueueStateEvent(second,new GamepadState()); break;
                    case -1:
                        InputSystem.QueueStateEvent(first,new GamepadState().WithButton(GamepadButton.Start)); break;
                    case 0:
                        Check(session.IsPlaying && b.HomeLoadout.ActiveKind == ActiveKind.Sight && b.HomeLoadout.MovementKind == MovementKind.Touch,"Controller-only setup and start failed");
                        hud.enabled=false;
                        Check(LocalGamepads.ForPlayer(0)?.Device == first && LocalGamepads.ForPlayer(1)?.Device == second, "Controller assignment failed");
                        InputSystem.QueueStateEvent(first, new GamepadState { leftStick = new Vector2(.8f, 0) }.WithButton(GamepadButton.South)); break;
                    case 1:
                        Check(a.Input.Horizontal > .5f && Mathf.Abs(b.Input.Horizontal) < .01f, "First controller movement must affect only Angel");
                        Check(a.Input.Held && a.Input.TakeJump() && !b.Input.TakeJump(), "Controller jump/glide isolation failed");
                        InputSystem.QueueStateEvent(first, new GamepadState());
                        InputSystem.QueueStateEvent(second, new GamepadState { leftStick = new Vector2(-.8f, 0) }.WithButton(GamepadButton.East)); break;
                    case 2:
                        Check(b.Input.Horizontal < -.5f && Mathf.Abs(a.Input.Horizontal) < .01f, "Second controller leaked movement");
                        Check(b.Input.TakeAbility() && !a.Input.TakeAbility(), "Controller ability isolation failed");
                        InputSystem.QueueStateEvent(second, new GamepadState());
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A, Key.RightArrow)); break;
                    case 3:
                        Check(a.Input.Horizontal < 0 && b.Input.Horizontal > 0, "Keyboard coexistence failed");
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                        a.Input.Placement(true);
                        InputSystem.QueueStateEvent(first, new GamepadState { leftStick = Vector2.up });
                        InputSystem.QueueStateEvent(second, new GamepadState { leftStick = Vector2.left }); break;
                    case 4:
                        Check(a.Input.Marker.y > .5f && b.Input.Marker == Vector2.zero && b.Input.Horizontal < -.5f, "Controller respawn placement failed");
                        a.Input.Placement(false); a.Status.ApplyFreeze();
                        InputSystem.QueueStateEvent(first, new GamepadState { leftStick = Vector2.right }.WithButton(GamepadButton.South).WithButton(GamepadButton.East)); break;
                    case 5:
                        Check(a.Input.Horizontal == 0 && !a.Input.Held && !a.Input.TakeJump() && !a.Input.TakeAbility(), "Medusa input lock bypassed by controller");
                        InputSystem.RemoveDevice(first);
                        Check(LocalGamepads.ForPlayer(1)?.Device == second && LocalGamepads.ForPlayer(0) == null, "Disconnect reassigned other player");
                        a.Status.ClearFreeze();
                        first = InputSystem.AddDevice<Gamepad>();
                        InputSystem.QueueStateEvent(first, new GamepadState { leftStick = Vector2.right }); break;
                    case 6:
                        Check(LocalGamepads.ForPlayer(0)?.Device == first && LocalGamepads.ForPlayer(1)?.Device == second, "Replacement controller assignment failed");
                        Check(a.Input.Horizontal > .5f, "Replacement controller did not control Angel");
                        var legacy = new MicroSense.PlayerInputBindings(Key.A, Key.D, Key.W, Key.S, Key.Q, Key.E);
                        Check(legacy.ReadHorizontal() > .5f, "Original levels do not accept controller input");
                        Debug.Log("CONTROLLER_VALIDATION_OK: controller-only setup/start, separate gamepads, movement, jump/glide, ability, keyboard coexistence, respawn, Medusa lock, disconnect/replacement and original-level input");
                        SessionState.SetBool("ControllerValidation", false); EditorApplication.Exit(0); break;
                }
            }
            catch (Exception error) { Debug.LogException(error); SessionState.SetBool("ControllerValidation", false); EditorApplication.Exit(1); }
        }
    }
}
