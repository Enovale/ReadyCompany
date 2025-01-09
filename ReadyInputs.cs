using LethalCompanyInputUtils.Api;
using UnityEngine.InputSystem;

namespace ReadyCompany
{
    public class ReadyInputs : LcInputActions
    {
        [InputAction("<Keyboard>/r", Name = "Ready Up")]
        public InputAction ReadyKey { get; set; } = null!;
    }
}