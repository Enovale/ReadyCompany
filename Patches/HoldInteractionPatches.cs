using HarmonyLib;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

namespace ReadyCompany.Patches
{
    [HarmonyPatch(typeof(HoldInteraction))]
    public class HoldInteractionPatches
    {
        [HarmonyPatch(nameof(HoldInteraction.Process))]
        [HarmonyPrefix]
        public static bool ProcessPrefix(HoldInteraction __instance, ref InputInteractionContext context)
        {
            if ((context.action == ReadyCompany.InputActions?.ReadyInput || context.action == ReadyCompany.InputActions?.UnreadyInput) && !ReadyHandler.LocalPlayerAbleToVote)
            {
                context.Canceled();
                return false;
            }

            return true;
        }
    }
}