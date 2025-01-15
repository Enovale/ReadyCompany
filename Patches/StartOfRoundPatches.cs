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
            if (LNetworkUtils.IsConnected && LNetworkUtils.IsHostOrServer)
                ReadyHandler.OnClientDisconnected();
        }

        [HarmonyPatch(nameof(StartOfRound.SetShipReadyToLand))]
        [HarmonyPatch(nameof(StartOfRound.StartGame))]
        [HarmonyPatch(nameof(StartOfRound.SwitchMapMonitorPurpose))]
        [HarmonyPostfix]
        public static void OnShipReadyToLandPatch()
        {
            if (LNetworkUtils.IsConnected)
                ReadyHandler.ResetReadyUp();
        }
        
        [HarmonyPatch(nameof(StartOfRound.ArriveAtLevel))]
        [HarmonyPostfix]
        public static void OnShipArriveAtLevelPatch()
        {
            if (LNetworkUtils.IsConnected && LNetworkUtils.IsHostOrServer)
                ReadyHandler.UpdateShipLever(ReadyHandler.ReadyStatus.Value);
        }
    }
}