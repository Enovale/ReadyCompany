using HarmonyLib;
using ReadyCompany.Components;
using ReadyCompany.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ReadyCompany.Patches
{
    [HarmonyPatch(typeof(HUDManager))]
    [HarmonyPriority(Priority.HigherThanNormal)]
    [HarmonyWrapSafe]
    internal class HUDPatches
    {
        internal static TextMeshProUGUI? ReadyStatusTextMesh;

        private static RectTransform? _parentTransform;

        static HUDPatches()
        {
            ReadyHandler.ReadyStatusChanged += UpdateTextBasedOnStatus;
        }

        [HarmonyPatch(nameof(HUDManager.Awake))]
        [HarmonyPostfix]
        private static void Awake(ref HUDManager __instance)
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
            _parentTransform = statusParent.GetComponent<RectTransform>();
            _parentTransform.anchorMax = _parentTransform.anchorMin = _parentTransform.pivot = new(0.5f, 0);
            _parentTransform.sizeDelta = new Vector2(208f, 38f);
            _parentTransform.localScale = Vector3.one;
            _parentTransform.anchoredPosition3D = new Vector3(0f, 115f, -0.075f);

            ReadyStatusTextMesh = statusParent.GetComponent<TextMeshProUGUI>();
            ReadyStatusTextMesh.font = __instance.controlTipLines[0].font;
            ReadyStatusTextMesh.fontSize = 16f;
            ReadyStatusTextMesh.enableWordWrapping = false;
            ReadyStatusTextMesh.color = ReadyCompany.Config.StatusColor.Value;
            ReadyStatusTextMesh.alignment = TextAlignmentOptions.Top;
            ReadyStatusTextMesh.overflowMode = 0;
            ReadyStatusTextMesh.enabled = true;
            ReadyStatusTextMesh.text = "";
            
            // Tips panel text doesn't support the unicode we use so add a font that does to the fallback table
            __instance.tipsPanelHeader.m_fontAsset.fallbackFontAssetTable.Add(ReadyStatusTextMesh.font);
            __instance.tipsPanelBody.m_fontAsset.fallbackFontAssetTable.Add(ReadyStatusTextMesh.font);

            var interactionBar = new GameObject("InteractionBar", typeof(Image), typeof(InteractionBarUI));
            var interactionBarTransform = interactionBar.GetComponent<RectTransform>();
            interactionBarTransform.anchorMax = interactionBarTransform.pivot = new(1, 0);
            interactionBarTransform.anchorMin = Vector2.zero;
            interactionBarTransform.anchoredPosition3D = new(-2f, 0, 0);
            interactionBarTransform.sizeDelta = new Vector2(0f, 5f);
            var interactionBarImage = interactionBar.GetComponent<Image>();
            interactionBarImage.fillMethod = Image.FillMethod.Horizontal;
            interactionBarImage.type = Image.Type.Filled;
            interactionBarImage.sprite = CreateSpriteFromTexture(Texture2D.whiteTexture);
            interactionBarImage.color = Color.white;
            interactionBar.transform.SetParent(_parentTransform.transform, false);

            UpdatePlacementBasedOnConfig(ReadyCompany.Config.StatusPlacement.Value);
        }

        private static Sprite CreateSpriteFromTexture(Texture2D texture2D)
        {
            var val = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height),
                new Vector2(0.5f, 0.5f));
            val.name = texture2D.name;
            return val;
        }

        public static void UpdateTextBasedOnStatus(ReadyMap status)
        {
            if (ReadyStatusTextMesh != null)
            {
                ReadyStatusTextMesh.text = ReadyHandler.GetBriefStatusDisplay(status);
                ReadyStatusTextMesh.gameObject.SetActive(ReadyHandler.InVotingPhase);
            }
        }

        public static void UpdatePlacementBasedOnConfig(StatusPlacement placement)
        {
            if (ReadyStatusTextMesh == null || _parentTransform == null)
                return;

            switch (placement)
            {
                default:
                case StatusPlacement.AboveHotbar:
                    _parentTransform.anchorMax = _parentTransform.anchorMin = _parentTransform.pivot = new(0.5f, 0);
                    _parentTransform.anchoredPosition3D = new Vector3(0f, 110f, -0.075f);
                    ReadyStatusTextMesh.alignment = TextAlignmentOptions.Top;
                    break;
                case StatusPlacement.BelowHotbar:
                    _parentTransform.anchorMax = _parentTransform.anchorMin = _parentTransform.pivot = new(0.5f, 0);
                    _parentTransform.anchoredPosition3D = new Vector3(0f, 0f, -0.075f);
                    ReadyStatusTextMesh.alignment = TextAlignmentOptions.Top;
                    break;
                case StatusPlacement.RightHotbar:
                    _parentTransform.anchorMax = _parentTransform.anchorMin = _parentTransform.pivot = new(1f, 0);
                    _parentTransform.anchoredPosition3D = new Vector3(-75f, 55f, -0.075f);
                    ReadyStatusTextMesh.alignment = TextAlignmentOptions.TopLeft;
                    break;
                case StatusPlacement.LeftHotbar:
                    _parentTransform.anchorMax = _parentTransform.anchorMin = _parentTransform.pivot = new(0f, 0);
                    _parentTransform.anchoredPosition3D = new Vector3(75f, 55f, -0.075f);
                    ReadyStatusTextMesh.alignment = TextAlignmentOptions.TopRight;
                    break;
                case StatusPlacement.TopScreen:
                    _parentTransform.anchorMax = _parentTransform.anchorMin = _parentTransform.pivot = new(0.5f, 1f);
                    _parentTransform.anchoredPosition3D = new Vector3(0f, 0f, -0.075f);
                    ReadyStatusTextMesh.alignment = TextAlignmentOptions.Top;
                    break;
                case StatusPlacement.BottomRightScreen:
                    _parentTransform.anchorMax = _parentTransform.anchorMin = _parentTransform.pivot = new(1f, 0f);
                    _parentTransform.anchoredPosition3D = new Vector3(0f, 0f, -0.075f);
                    ReadyStatusTextMesh.alignment = TextAlignmentOptions.TopRight;
                    break;
                case StatusPlacement.BottomLeftScreen:
                    _parentTransform.anchorMax = _parentTransform.anchorMin = _parentTransform.pivot = new(0f, 0f);
                    _parentTransform.anchoredPosition3D = new Vector3(0f, 0f, -0.075f);
                    ReadyStatusTextMesh.alignment = TextAlignmentOptions.TopLeft;
                    break;
            }
        }
    }
}