using EternalCycle;
using UnityEngine.InputSystem;

namespace DivineSense
{
    internal struct TrialContinueInput
    {
        internal const string Prompt = "Press A, Space, or Enter to continue";
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
            Keyboard keyboard = Keyboard.current;
            int inputs = keyboard?.spaceKey.isPressed == true ? 1 : 0;
            if (keyboard?.enterKey.isPressed == true)
                inputs |= 2;

            for (int player = 0; player < 2; player++)
            {
                if (LocalGamepads.ForPlayer(player)?.buttonSouth.isPressed == true)
                    inputs |= 4 << player;
            }

            return inputs;
        }
    }
}
