using HarmonyLib;
using LethalNetworkAPI.Utils;

namespace ReadyCompany.Patches
{
    [HarmonyPatch(typeof(RoundManager))]
    internal class RoundManagerPatches
    {
        public static bool GeneratingLevel;
        
        [HarmonyPatch(nameof(RoundManager.GenerateNewLevelClientRpc))]
        [HarmonyPatch(nameof(RoundManager.FinishGeneratingNewLevelClientRpc))]
        [HarmonyPostfix]
        public static void GenerateNewLevelClientRpcPostfix()
        {
            if (LNetworkUtils.IsConnected)
            {
                GeneratingLevel = true;
                ReadyHandler.ResetReadyUp();
            }
        }
    }
}