using LethalCompanyInputUtils.Api;
using UnityEngine.InputSystem;

namespace ReadyCompany
{
    public class ReadyInputs : LcInputActions
    {
        [InputAction("<Keyboard>/r", GamepadPath = "<Gamepad>/select", Name = "Ready Up", KbmInteractions = "hold(duration = 2)", ActionType = InputActionType.Value)]
        public InputAction ReadyInput { get; set; } = null!;
        [InputAction("<Keyboard>/r", GamepadPath = "<Gamepad>/select", Name = "Unready", KbmInteractions = "multiTap(tapTime = 0.2, tapCount = 3)", ActionType = InputActionType.Value)]
        public InputAction UnreadyInput { get; set; } = null!;
    }
}