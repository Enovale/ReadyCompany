using GameNetcodeStuff;
using HarmonyLib;
using LethalNetworkAPI.Utils;

namespace ReadyCompany.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatches
    {
        private static bool _localPlayerUsingControllerPreviously;
        
        [HarmonyPatch(nameof(PlayerControllerB.ConnectClientToPlayerObject))]
        [HarmonyPostfix]
        public static void PlayerLoadedPatch()
        {
            if (LNetworkUtils.IsConnected && LNetworkUtils.IsHostOrServer)
            {
                ReadyHandler.ShouldPlaySound = false;
                ReadyHandler.ResetReadyUp();
            }
        }

        [HarmonyPatch(nameof(PlayerControllerB.Look_performed))]
        [HarmonyPostfix]
        public static void LookPerformedPatch()
        {
            if (StartOfRound.Instance == null)
                return;

            if (StartOfRound.Instance.localPlayerUsingController != _localPlayerUsingControllerPreviously)
                ReadyHandler.ForceReadyStatusChanged();
            
            _localPlayerUsingControllerPreviously = StartOfRound.Instance.localPlayerUsingController;
        }
    }
}