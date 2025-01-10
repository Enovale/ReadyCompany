using HarmonyLib;
using LethalNetworkAPI.Utils;

namespace ReadyCompany.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    [HarmonyPriority(Priority.HigherThanNormal)]
    [HarmonyWrapSafe]
    public class StartOfRoundPatches
    {
        [HarmonyPatch(nameof(StartOfRound.PlayerLoadedClientRpc))]
        [HarmonyPostfix]
        public static void OnClientConnectedPatch()
        {
            if (LNetworkUtils.IsHostOrServer)
                ReadyHandler.OnClientConnected();
        }
        
        [HarmonyPatch(nameof(StartOfRound.OnPlayerDC))]
        [HarmonyPostfix]
        public static void OnClientDisconnectedPatch()
        {
            if (LNetworkUtils.IsHostOrServer)
                ReadyHandler.OnClientDisconnected();
        }

        [HarmonyPatch(nameof(StartOfRound.SetShipReadyToLand))]
        [HarmonyPostfix]
        public static void OnShipReadyToLandPatch()
        {
            if (LNetworkUtils.IsHostOrServer)
                ReadyHandler.ResetReadyUp();
        }
    }
}