using HarmonyLib;
using LethalNetworkAPI.Utils;

namespace ReadyCompany.Patches
{
    [HarmonyPatch(typeof(StartMatchLever))]
    [HarmonyAfter("imabatby.lethallevelloader")]
    [HarmonyWrapSafe]
    internal class StartMatchLeverPatches
    {
        public static bool HasShownReadyWarning;
        
        private static bool _shouldOverrideLeverState;

        private static string _previousHoverTip = null!;
        private static string _previousDisabledHoverTip = null!;
        private static bool _previousInteractableState;
        private static bool _justWroteInteractableState;

        [HarmonyPatch(nameof(StartMatchLever.Start))]
        [HarmonyPostfix]
        public static void StartPatch(InteractTrigger ___triggerScript)
        {
            _previousHoverTip = ___triggerScript.hoverTip;
            _previousDisabledHoverTip = ___triggerScript.disabledHoverTip;
            _previousInteractableState = ___triggerScript.interactable;
        }

        [HarmonyPatch(nameof(StartMatchLever.Update))]
        [HarmonyPostfix]
        public static void UpdatePatch(InteractTrigger ___triggerScript)
        {
            _shouldOverrideLeverState = ReadyHandler.ShouldOverrideLeverState(ReadyHandler.ReadyStatus.Value);
            var hoverContainsTip = string.Equals(___triggerScript.hoverTip, ReadyHandler.LEVER_WARNING_TIP) ||
                                   string.Equals(___triggerScript.hoverTip, ReadyHandler.LEVER_DISABLED_TIP);
            var disabledContainsTip = string.Equals(___triggerScript.disabledHoverTip, ReadyHandler.LEVER_WARNING_TIP) ||
                                      string.Equals(___triggerScript.disabledHoverTip, ReadyHandler.LEVER_DISABLED_TIP);
            if (!hoverContainsTip && !disabledContainsTip)
            {
                _previousHoverTip = ___triggerScript.hoverTip;
                _previousDisabledHoverTip = ___triggerScript.disabledHoverTip;
                _previousInteractableState = ___triggerScript.interactable;
            }

            if (_previousInteractableState != ___triggerScript.interactable && !_justWroteInteractableState)
            {
                _previousInteractableState = ___triggerScript.interactable;
            }
            
            // This might (?) allow the host to pull the lever even if LethalLevelLoader doesn't want us to
            if (_shouldOverrideLeverState)
            {
                var interactable = _previousInteractableState ? LNetworkUtils.IsHostOrServer : _previousInteractableState;
                ___triggerScript.hoverTip = ___triggerScript.disabledHoverTip =
                    interactable ? ReadyHandler.LEVER_WARNING_TIP : ReadyHandler.LEVER_DISABLED_TIP;
                ___triggerScript.interactable = interactable;
                _justWroteInteractableState = true;
            }
            else if (hoverContainsTip || disabledContainsTip)
            {
                ___triggerScript.hoverTip = _previousHoverTip;
                ___triggerScript.disabledHoverTip = string.IsNullOrEmpty(_previousDisabledHoverTip) && !_previousInteractableState ? _previousHoverTip : _previousDisabledHoverTip;
                ___triggerScript.interactable = _previousInteractableState;
                _justWroteInteractableState = false;
            }
        }

        [HarmonyPatch(nameof(StartMatchLever.BeginHoldingInteractOnLever))]
        [HarmonyPostfix]
        public static void OnHoldLeverPatch(StartMatchLever __instance)
        {
            if (_shouldOverrideLeverState)
            {
                __instance.triggerScript.timeToHold = 4f;

                if (!HasShownReadyWarning)
                {
                    HUDManager.Instance.DisplayTip("HALT!", "The lobby is not ready!", true);
                    HasShownReadyWarning = true;
                    __instance.hasDisplayedTimeWarning = false;
                }
            }
            else if (ReadyHandler.InVotingPhase && TimeOfDay.Instance.daysUntilDeadline > 0)
            {
                __instance.triggerScript.timeToHold = 0.7f;
            }
        }
    }
}