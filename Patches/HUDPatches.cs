using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
            if (parent == null)
                return;
            
            var statusParent = new GameObject("ReadyStatusDisplay", typeof(TextMeshProUGUI), typeof(CanvasGroup));
            statusParent.transform.SetParent(parent);
            __instance.HUDElements = __instance.HUDElements.AddToArray<HUDElement>(new HUDElement
            {
                canvasGroup = statusParent.GetComponent<CanvasGroup>(),
                targetAlpha = 1f
            });
            var parentTransform = statusParent.GetComponent<RectTransform>();
            parentTransform.anchorMax = parentTransform.anchorMin = parentTransform.pivot = new(1, 0);
            parentTransform.sizeDelta = new Vector2(208f, 20f);
            parentTransform.localScale = Vector3.one;
            parentTransform.anchoredPosition3D = new Vector3(-75f, 55f, -0.075f);
            
            ReadyStatusTextMesh = statusParent.GetComponent<TextMeshProUGUI>();
            ReadyStatusTextMesh.font = __instance.controlTipLines[0].font;
            ReadyStatusTextMesh.fontSize = 16f;
            ReadyStatusTextMesh.enableWordWrapping = false;
            //ReadyStatusTextMesh.color = new Color(0, 135, 32);
            ReadyStatusTextMesh.color = HUDManager.Instance.weightCounter.color;
            ReadyStatusTextMesh.alignment = TextAlignmentOptions.BottomRight;
            ReadyStatusTextMesh.overflowMode = 0;
            ReadyStatusTextMesh.enabled = true;
            ReadyStatusTextMesh.text = "Test Test 123123";
            
            var interactionBar = new GameObject("InteractionBar", typeof(Image), typeof(InteractionBarUI));
            var interactionBarTransform = interactionBar.GetComponent<RectTransform>();
            interactionBarTransform.anchorMax = interactionBarTransform.pivot = new(1, 0);
            interactionBarTransform.anchorMin = Vector2.zero;
            interactionBarTransform.anchoredPosition3D = new(0, -5, -0.075f);
            interactionBarTransform.sizeDelta = new Vector2(0f, 5f);
            var interactionBarImage = interactionBar.GetComponent<Image>();
            interactionBarImage.fillMethod = Image.FillMethod.Horizontal;
            interactionBarImage.type = Image.Type.Filled;
            interactionBarImage.sprite = CreateSpriteFromTexture(Texture2D.whiteTexture);
            interactionBarImage.color = Color.white;
            interactionBar.transform.SetParent(parentTransform.transform, false);
        }

        private static Sprite CreateSpriteFromTexture(Texture2D texture2D)
        {
            var val = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f));
            val.name = texture2D.name;
            return val;
        }

        public static void UpdateTextBasedOnStatus(ReadyMap status)
        {
            if (ReadyStatusTextMesh != null)
            {
                ReadyStatusTextMesh.text = ReadyHandler.GetBriefStatusDisplay(status);
                ReadyStatusTextMesh.gameObject.SetActive(StartOfRound.Instance.inShipPhase);
            }
        }
    }
}