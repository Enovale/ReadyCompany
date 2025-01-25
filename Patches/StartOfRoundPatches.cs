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
        [HarmonyPatch(nameof(StartOfRound.openingDoorsSequence), MethodType.Enumerator)]
        [HarmonyPostfix]
        public static void NeedsResetPatches()
        {
            if (LNetworkUtils.IsConnected)
                ReadyHandler.ResetReadyUp();

            if (!ReadyHandler.InVotingPhase && HUDManager.Instance != null)
                HUDManager.Instance.spectatorTipText.enabled = false;
        }

        [HarmonyPatch(nameof(StartOfRound.openingDoorsSequence), MethodType.Enumerator)]
        [HarmonyPostfix]
        public static void OpeningDoorsFinished()
        {
            RoundManagerPatches.GeneratingLevel = false;
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