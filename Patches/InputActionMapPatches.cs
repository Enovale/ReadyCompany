using HarmonyLib;
using UnityEngine.InputSystem;

namespace ReadyCompany.Patches
{
    [HarmonyPatch(typeof(InputActionMap))]
    internal class InputActionMapPatches
    {
        [HarmonyPatch(nameof(InputActionMap.OnBindingModified))]
        [HarmonyPostfix]
        public static void OnBindingModifiedPatch(ref InputActionMap __instance)
        {
            if (__instance == ReadyCompany.InputActions?.ReadyInput.actionMap ||
                __instance == ReadyCompany.InputActions?.UnreadyInput.actionMap)
                ReadyHandler.ForceReadyStatusChanged();
        }
    }
}