using System.Collections.Generic;
using GameNetcodeStuff;
using HarmonyLib;
using LethalNetworkAPI.Utils;

namespace ReadyCompany.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    [HarmonyPriority(Priority.HigherThanNormal)]
    [HarmonyWrapSafe]
    internal class PlayerControllerBPatches
    {
        private static bool _localPlayerUsingControllerPreviously;
        private static bool _localPlayerAbleToVotePreviously;

        private static readonly Dictionary<int, bool> _ableToVoteMap = new();
        
        internal static void Reset() => _ableToVoteMap.Clear();
        
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

        [HarmonyPatch(nameof(PlayerControllerB.LateUpdate))]
        [HarmonyPostfix]
        public static void LateUpdatePatch(PlayerControllerB __instance)
        {
            if (__instance != StartOfRound.Instance?.localPlayerController)
                return;
            
            var localPlayerAbleToVote = ReadyHandler.LocalPlayerAbleToVote;
            if (_localPlayerAbleToVotePreviously != localPlayerAbleToVote)
            {
                ReadyHandler.ForceReadyStatusChanged();
            }

            _localPlayerAbleToVotePreviously = localPlayerAbleToVote;
        }


        [HarmonyPatch(nameof(PlayerControllerB.KillPlayerServerRpc))]
        [HarmonyPostfix]
        public static void KillPlayerPatch(PlayerControllerB __instance, int playerId)
        {
            UpdatePlayerAbleToVote(StartOfRound.Instance.allPlayerScripts[playerId], playerId, true);
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

        [HarmonyPatch(nameof(PlayerControllerB.UpdatePlayerPositionServerRpc))]
        [HarmonyPostfix]
        public static void UpdatePlayerPositionServerRpcPatch(PlayerControllerB __instance)
        {
            UpdatePlayerAbleToVote(__instance, ReadyHandler.TryGetPlayerIdFromClientId(__instance.actualClientId));
        }

        private static void UpdatePlayerAbleToVote(PlayerControllerB __instance, int playerId, bool force = false)
        {
            var ableToVote = ReadyHandler.PlayerAbleToVoteServer(__instance);
            if (force || _ableToVoteMap.TryGetValue(playerId, out var ableToVotePreviously) && ableToVotePreviously != ableToVote)
            {
                ReadyCompany.Logger.LogDebug($"AbleToVote changed! {ableToVote} {__instance.isPlayerDead} {__instance.isInElevator}");
                ReadyHandler.UpdateReadyMap();
            }
            
            _ableToVoteMap[playerId] = ableToVote;
        }
    }
}