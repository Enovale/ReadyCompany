using LethalCompanyInputUtils.Api;
using UnityEngine.InputSystem;

namespace ReadyCompany.Config
{
    public class ReadyInputs : LcInputActions
    {
        [InputAction("<Keyboard>/c", GamepadPath = "<Gamepad>/select", Name = "Ready Up", ActionType = InputActionType.Button)]
        public InputAction ReadyInput { get; set; } = null!;
        [InputAction("<Keyboard>/c", GamepadPath = "<Gamepad>/select", Name = "Unready", ActionType = InputActionType.Button)]
        public InputAction UnreadyInput { get; set; } = null!;

        internal int CurrentBinding => StartOfRound.Instance?.localPlayerUsingController ?? false ? 1 : 0;

        public string ReadyInputName => ReadyInput.bindings[CurrentBinding].ToDisplayString();
        public string UnreadyInputName => UnreadyInput.bindings[CurrentBinding].ToDisplayString();
    }
}