using GameNetcodeStuff;
using LethalCompanyInputUtils.Api;
using UnityEngine.InputSystem;

namespace ReadyCompany
{
    public class ReadyInputs : LcInputActions
    {
        [InputAction("<Keyboard>/r", GamepadPath = "<Gamepad>/select", Name = "Ready Up", KbmInteractions = "hold(duration = 1.5)", ActionType = InputActionType.Value)]
        public InputAction ReadyInput { get; set; } = null!;
        [InputAction("<Keyboard>/r", GamepadPath = "<Gamepad>/select", Name = "Unready", KbmInteractions = "multiTap(tapTime = 0.2, tapCount = 3)", ActionType = InputActionType.Value)]
        public InputAction UnreadyInput { get; set; } = null!;

        public string ReadyInputName => ReadyInput.bindings[0].ToDisplayString();
        public string UnreadyInputName => UnreadyInput.bindings[0].ToDisplayString();
    }
}