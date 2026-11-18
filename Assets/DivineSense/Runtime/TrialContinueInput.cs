using EternalCycle;
using UnityEngine.InputSystem;

namespace DivineSense
{
    internal struct TrialContinueInput
    {
        internal const string Prompt = "Press Space or RB on either controller to continue";
        private const float TriggerThreshold = 0.5f;
        private int heldInputs;

        internal void Reset() => heldInputs = ReadHeldInputs();

        internal bool Pressed()
        {
            int currentInputs = ReadHeldInputs();
            bool pressed = (currentInputs & ~heldInputs) != 0;
            heldInputs = currentInputs;
            return pressed;
        }

        private static int ReadHeldInputs()
        {
            int inputs = Keyboard.current?.spaceKey.isPressed == true ? 1 : 0;
            for (int player = 0; player < 2; player++)
            {
                if ((LocalGamepads.ForPlayer(player)?.rightTrigger.ReadValue() ?? 0f) >= TriggerThreshold)
                    inputs |= 1 << (player + 1);
            }
            return inputs;
        }
    }
}
