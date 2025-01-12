using HarmonyLib;

namespace ReadyCompany.Patches
{
    [HarmonyPatch(typeof(MenuManager))]
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