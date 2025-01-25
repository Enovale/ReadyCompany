using HarmonyLib;

namespace ReadyCompany.Patches
{
    [HarmonyPatch(typeof(StartMatchLever))]
    [HarmonyPriority(Priority.Last)]
    [HarmonyWrapSafe]
    internal class StartMatchLeverPatches
    {
        public static bool HasShownReadyWarning;

        [HarmonyPatch(nameof(StartMatchLever.Update))]
        [HarmonyPrefix]
        public static bool UpdatePatch(StartMatchLever __instance)
        {
            if (ReadyHandler.ShouldOverrideLeverState(ReadyHandler.ReadyStatus.Value))
                return false;

            return true;
        }

        [HarmonyPatch(nameof(StartMatchLever.BeginHoldingInteractOnLever))]
        [HarmonyPostfix]
        public static void OnHoldLeverPatch(StartMatchLever __instance)
        {
            if (ReadyHandler.InVotingPhase && !ReadyHandler.IsLobbyReady() && !HasShownReadyWarning)
            {
                __instance.triggerScript.timeToHold = 4f;
                HUDManager.Instance.DisplayTip("HALT!", "The lobby is not ready!", true);
                HasShownReadyWarning = true;
                __instance.hasDisplayedTimeWarning = false;
            }
            else if (ReadyHandler.InVotingPhase && ReadyHandler.IsLobbyReady() && TimeOfDay.Instance.daysUntilDeadline > 0)
            {
                __instance.triggerScript.timeToHold = 0.7f;
            }
        }
    }
}