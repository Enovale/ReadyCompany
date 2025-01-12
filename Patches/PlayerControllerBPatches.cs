using GameNetcodeStuff;
using HarmonyLib;
using LethalNetworkAPI.Utils;

namespace ReadyCompany.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatches
    {
        [HarmonyPatch(nameof(PlayerControllerB.ConnectClientToPlayerObject))]
        [HarmonyPostfix]
        public static void PlayerLoadedPatch()
        {
            if (LNetworkUtils.IsHostOrServer)
                ReadyHandler.UpdateReadyMap();
        }
    }
}