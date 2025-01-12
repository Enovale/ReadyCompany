using HarmonyLib;
using LethalNetworkAPI.Utils;

namespace ReadyCompany.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    [HarmonyPriority(Priority.HigherThanNormal)]
    [HarmonyWrapSafe]
    internal class StartOfRoundPatches
    {
        [HarmonyPatch(nameof(StartOfRound.OnPlayerDC))]
        [HarmonyPostfix]
        public static void OnClientDisconnectedPatch()
        {
            if (LNetworkUtils.IsHostOrServer)
                ReadyHandler.OnClientDisconnected();
        }

        [HarmonyPatch(nameof(StartOfRound.SetShipReadyToLand))]
        [HarmonyPatch(nameof(StartOfRound.StartGame))]
        [HarmonyPostfix]
        public static void OnShipReadyToLandPatch()
        {
            if (LNetworkUtils.IsHostOrServer)
            {
                ReadyHandler.ResetReadyUp();
            }
        }
    }
}