using HarmonyLib;
using TMPro;
using UnityEngine;

namespace ReadyCompany.Patches
{
    [HarmonyPatch(typeof(HUDManager))]
    internal class HUDPatches
    {
        internal static TextMeshProUGUI? ReadyStatusTextMesh;

        [HarmonyPatch(nameof(HUDManager.Awake))]
        [HarmonyPostfix]
        private static void Start(ref HUDManager __instance)
        {
            var parent = __instance.HUDElements[2].canvasGroup.transform.parent;
            var val = new GameObject("ReadyStatusDisplay", typeof(TextMeshProUGUI), typeof(CanvasGroup));
            val.transform.SetParent(parent);
            __instance.HUDElements = __instance.HUDElements.AddToArray<HUDElement>(new HUDElement
            {
                canvasGroup = val.GetComponent<CanvasGroup>(),
                targetAlpha = 1f
            });
            var component = val.GetComponent<RectTransform>();
            component.anchorMax = component.anchorMin = component.pivot = new(1, 0);
            component.sizeDelta = new Vector2(350f, 20f);
            component.localScale = Vector3.one;
            component.anchoredPosition3D = new Vector3(-75f, 55f, -0.075f);
            
            ReadyStatusTextMesh = val.GetComponent<TextMeshProUGUI>();
            ReadyStatusTextMesh.font = __instance.controlTipLines[0].font;
            ReadyStatusTextMesh.fontSize = 16f;
            ReadyStatusTextMesh.enableWordWrapping = false;
            //ReadyStatusTextMesh.color = new Color(0, 135, 32);
            ReadyStatusTextMesh.color = HUDManager.Instance.weightCounter.color;
            ReadyStatusTextMesh.alignment = TextAlignmentOptions.BottomRight;
            ReadyStatusTextMesh.overflowMode = 0;
            ReadyStatusTextMesh.enabled = true;
            ReadyStatusTextMesh.text = "Test Test 123123";
        }

        public static void UpdateTextBasedOnStatus(ReadyMap status)
        {
            if (ReadyStatusTextMesh != null)
            {
                ReadyStatusTextMesh.text = ReadyHandler.GetBriefStatusDisplay(status);
                ReadyStatusTextMesh.enabled = StartOfRound.Instance.inShipPhase;
            }
        }
    }
}