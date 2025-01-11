using HarmonyLib;

namespace ReadyCompany.Patches
{
    [HarmonyPatch(typeof(StartMatchLever))]
    [HarmonyPriority(Priority.Last)]
    [HarmonyWrapSafe]
    public class StartMatchLeverPatches
    {
        [HarmonyPatch(nameof(StartMatchLever.Update))]
        [HarmonyPrefix]
        public static bool UpdatePatch(StartMatchLever __instance)
        {
            if (__instance.triggerScript.hoverTip == ReadyHandler.LEVER_DISABLED_TIP || __instance.triggerScript.hoverTip == ReadyHandler.LEVER_WARNING_TIP)
                return false;

            return true;
        }
    }
}