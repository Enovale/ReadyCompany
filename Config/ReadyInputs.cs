using LethalCompanyInputUtils.Api;
using UnityEngine.InputSystem;

namespace ReadyCompany.Config
{
    public class ReadyInputs : LcInputActions
    {
        [InputAction("<Keyboard>/r", GamepadPath = "<Gamepad>/select", Name = "Ready Up", ActionType = InputActionType.Button)]
        public InputAction ReadyInput { get; set; } = null!;
        [InputAction("<Keyboard>/r", GamepadPath = "<Gamepad>/select", Name = "Unready", ActionType = InputActionType.Button)]
        public InputAction UnreadyInput { get; set; } = null!;

        public string ReadyInputName => ReadyInput.bindings[0].ToDisplayString();
        public string UnreadyInputName => UnreadyInput.bindings[0].ToDisplayString();
    }
}