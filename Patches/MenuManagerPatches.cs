using HarmonyLib;

namespace ReadyCompany.Patches
{
    [HarmonyPatch(typeof(MenuManager))]
    [HarmonyPriority(Priority.HigherThanNormal)]
    [HarmonyWrapSafe]
    internal class MenuManagerPatches
    {
        [HarmonyPatch(nameof(MenuManager.Awake))]
        [HarmonyPostfix]
        public static void MainMenuLoaded()
        {
            ReadyHandler.ResetReadyUp();
        }
    }
}