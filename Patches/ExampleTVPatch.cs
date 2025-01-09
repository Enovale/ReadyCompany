using HarmonyLib;

namespace ReadyCompany.Patches;

[HarmonyPatch(typeof(TVScript))]
public class ExampleTVPatch
{
    [HarmonyPatch(nameof(TVScript.SwitchTVLocalClient))]
    [HarmonyPrefix]
    private static void SwitchTVPrefix(TVScript __instance)
    {
    }
}