using System;
using System.Collections.Generic;
using System.Linq;
using LethalNetworkAPI;
using LethalNetworkAPI.Utils;
using ReadyCompany.Config;
using ReadyCompany.Patches;
using ReadyCompany.Util;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ReadyCompany
{
    public static class ReadyHandler
    {
        private const string READY_STATUS_SIG = MyPluginInfo.PLUGIN_GUID + "_readyStatusVar";
        private const string READY_EVENT_SIG = MyPluginInfo.PLUGIN_GUID + "_playerReadyEvent";
        
        internal const string LEVER_DISABLED_TIP = "[ Lobby must be ready to start ]";
        internal const string LEVER_WARNING_TIP = "[ WARNING: Lobby Not Ready ]";
        internal const string LEVER_NORMAL_TIP = "Start game : [LMB]";

        public static LNetworkVariable<ReadyMap> ReadyStatus { get; } = LNetworkVariable<ReadyMap>.Connect(identifier: READY_STATUS_SIG, onValueChanged: ReadyStatusChanged);

        private static readonly LNetworkMessage<bool> readyUpMessage = LNetworkMessage<bool>.Connect(identifier: READY_EVENT_SIG, ReadyUpFromClient);

        private static readonly Dictionary<int, bool> _playerReadyMap = new();

        internal static bool ShouldPlaySound { get; set; }
        
        public static event Action<ReadyMap>? NewReadyStatus;

        public static bool InVotingPhase => (StartOfRound.Instance?.inShipPhase ?? false) ||
                                            (StartOfRound.Instance?.currentLevel.levelID == 3 && StartOfRound.Instance.shipHasLanded);
        public static ulong? ActualLocalClientId => StartOfRound.Instance?.localPlayerController?.actualClientId;
        public static int? LocalPlayerId => !ActualLocalClientId.HasValue ? null : (StartOfRound.Instance?.ClientPlayerList.TryGetValue((ulong)ActualLocalClientId, out var result) ?? false ? result : null);

        internal static void InitializeEvents()
        {
            ReadyStatus.Value = new(_playerReadyMap);
            // There's a weird bug where sometimes the input system isn't initialized at Awake()
            // So we defer it to the first scene load instead
            SceneManager.sceneLoaded += OnSceneLoaded;
            NewReadyStatus += HUDPatches.UpdateTextBasedOnStatus;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            InitializeInputActions();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private static void InitializeInputActions()
        {
            if (ReadyCompany.InputActions == null)
            {
                ReadyCompany.InputActions = new ReadyInputs();
                ReadyCompany.InputActions.ReadyInput.performed += ReadyInputPerformed;
                ReadyCompany.InputActions.ReadyInput.started += context => { InteractionBarUI.Instance.ReadyInteraction = context.interaction; };
                ReadyCompany.InputActions.ReadyInput.canceled += context => { InteractionBarUI.Instance.ReadyInteraction = null; };
                ReadyCompany.InputActions.UnreadyInput.performed += UnreadyInputPerformed;
                ReadyCompany.InputActions.UnreadyInput.started += context => { InteractionBarUI.Instance.UnreadyInteraction = context.interaction; };
                ReadyCompany.InputActions.UnreadyInput.canceled += context => { InteractionBarUI.Instance.UnreadyInteraction = null; };
                ReadyCompany.Config.UpdateBindingsBasedOnConfig();
            }
        }

        public static bool IsLobbyReady(ReadyMap map) => map.LobbySize > 0 && (float)map.PlayersReady / map.LobbySize >=
            ReadyCompany.Config.PercentageForReady.Value / 100f;

        public static bool IsLobbyReady() => IsLobbyReady(ReadyStatus.Value);

        // Don't really like this method of forcing an update even if nothing's changed
        // (noone ready, reset and verify = noone ready still)
        public static void ResetReadyUp()
        {
            ReadyCompany.Logger.LogDebug($"ReadyUp reset: {ReadyStatus.Value}");
            if (NetworkManager.Singleton != null && LNetworkUtils.IsHostOrServer)
            {
                _playerReadyMap.Clear();
                UpdateReadyMap();
            }

            StartMatchLeverPatches.HasShownReadyWarning = false;
            ReadyStatusChangedReal(ReadyStatus.Value);
            ReadyCompany.Logger.LogDebug($"Reset done: {ReadyStatus.Value}");
        }

        internal static void OnClientConnected()
        {
            UpdateReadyMap();
        }

        internal static void OnClientDisconnected()
        {
            UpdateReadyMap();
        }

        private static void ReadyStatusChanged(ReadyMap oldValue, ReadyMap? newValue)
        {
            if (newValue == null)
                return;
            
            ReadyCompany.Logger.LogDebug($"Readyup about to change: {newValue}");
            ReadyStatusChangedReal(newValue);
        }

        internal static void ReadyStatusChangedReal(ReadyMap newValue)
        {
            PopupReadyStatus(newValue);
            NewReadyStatus?.Invoke(newValue);
            UpdateShipLever(newValue);
        }

        internal static void PopupReadyStatus(ReadyMap map)
        {
            if (HUDManager.Instance == null || !InVotingPhase)
                return;

            AudioClip[] sfx;
            if (IsLobbyReady(map) && ReadyCompany.Config.CustomLobbyReadySounds.Count > 0)
                sfx = ReadyCompany.Config.CustomLobbyReadySounds.ToArray();
            else
                sfx = ReadyCompany.Config.CustomPopupSounds.Count > 0
                    ? ReadyCompany.Config.CustomPopupSounds.ToArray()
                    : HUDManager.Instance.tipsSFX;

            var statusText = GetBriefStatusDisplay(map);
            CustomDisplayTip("Ready Up!", statusText, sfx);
            CustomDisplaySpectatorTip(statusText);
        }

        private static void CustomDisplaySpectatorTip(string body)
        {
            var hud = HUDManager.Instance;
            if (hud == null)
                return;
            
            hud.spectatorTipText.text = body;
            hud.spectatorTipText.enabled = true;
        }

        private static void CustomDisplayTip(string headerText, string bodyText, AudioClip[]? sfx = null)
        {
            var hud = HUDManager.Instance;
            if (hud == null)
                return;

            if (ReadyCompany.Config.ShowPopup.Value)
            {
                hud.tipsPanelHeader.text = headerText;
                hud.tipsPanelBody.text = bodyText;
                hud.tipsPanelAnimator.SetTrigger("TriggerHint");
            }

            if (ReadyCompany.Config.PlaySound.Value && ShouldPlaySound)
            {
                sfx ??= hud.tipsSFX;
                if (sfx.Length <= 0)
                    sfx = hud.tipsSFX;
                Utils.PlayRandomClip(hud.UIAudio, sfx, ReadyCompany.Config.SoundVolume.Value / 100f);
            }

            ShouldPlaySound = true;
        }

        internal static string GetBriefStatusDisplay(ReadyMap map) =>
            $"{map.PlayersReady} / {map.LobbySize} Players are ready.\n" +
            (map.LocalPlayerReady ? $"{ReadyCompany.InputActions?.UnreadyInputName} to Unready!" : $"{ReadyCompany.InputActions?.ReadyInputName} to Ready Up!");

        internal static void UpdateShipLever(ReadyMap map)
        {
            if (StartOfRound.Instance == null || !InVotingPhase || StartOfRound.Instance.travellingToNewLevel)
                return;
            
            ReadyCompany.Logger.LogDebug($"Shiplever updating: {map}");
            var lever = UnityEngine.Object.FindObjectOfType<StartMatchLever>();
            var lobbyReady = IsLobbyReady(map);
            if (ReadyCompany.Config.RequireReadyToStart.Value)
            {
                lever.triggerScript.disabledHoverTip = lobbyReady ? "" : LEVER_DISABLED_TIP;
                lever.triggerScript.hoverTip = lobbyReady ? LEVER_NORMAL_TIP : LEVER_WARNING_TIP;
                lever.triggerScript.interactable = lobbyReady || LNetworkUtils.IsHostOrServer;
            }
            else if (lever.triggerScript.disabledHoverTip == LEVER_DISABLED_TIP || lever.triggerScript.hoverTip == LEVER_WARNING_TIP)
            {
                lever.triggerScript.disabledHoverTip = "";
                lever.triggerScript.hoverTip = LEVER_NORMAL_TIP;
                lever.triggerScript.interactable = true;
            }

            if (ReadyCompany.Config.AutoStartWhenReady.Value && LNetworkUtils.IsHostOrServer && lobbyReady)
            {
                lever.LeverAnimation();
                lever.PullLever();
            }
        }

        private static void ReadyUpFromClient(bool isReady, ulong clientId)
        {
            var playerId = TryGetPlayerIdFromClientId(clientId);
            _playerReadyMap[playerId] = isReady;

            UpdateReadyMap();
        }

        public static void UpdateReadyMap()
        {
            if (!LNetworkUtils.IsConnected)
                return;
            
            VerifyReadyUpMap();
            
            ReadyStatus.Value = new(_playerReadyMap);
            ReadyStatus.MakeDirty();
        }

        private static void VerifyReadyUpMap()
        {
            if (!LNetworkUtils.IsConnected)
                return;
            
            foreach (var (clientId, _) in _playerReadyMap.Where(kvp => kvp.Key != LocalPlayerId && !StartOfRound.Instance.ClientPlayerList.ContainsValue(kvp.Key)).ToList())
            {
                _playerReadyMap.Remove(clientId);
            }

            if (StartOfRound.Instance != null)
            {
                foreach (var (clientId, playerId) in StartOfRound.Instance.ClientPlayerList)
                {
                    _playerReadyMap.TryAdd(playerId, false);
                }
                
                var localPlayerId = LocalPlayerId;
                if (localPlayerId.HasValue)
                    _playerReadyMap.TryAdd((int)localPlayerId, false);
            }
        }

        private static void ReadyInputPerformed(InputAction.CallbackContext context)
        {
            if (!LNetworkUtils.IsConnected)
                return;
            
            if (ReadyStatus is { Value.LocalPlayerReady: false })
            {
                ReadyCompany.InputActions?.UnreadyInput.Disable();
                ReadyCompany.InputActions?.UnreadyInput.Enable();
            }
            
            readyUpMessage.SendServer(true);
        }

        private static void UnreadyInputPerformed(InputAction.CallbackContext context)
        {
            if (!LNetworkUtils.IsConnected)
                return;
            
            if (ReadyStatus is { Value.LocalPlayerReady: true })
            {
                ReadyCompany.InputActions?.ReadyInput.Disable();
                ReadyCompany.InputActions?.ReadyInput.Enable();
            }
            
            readyUpMessage.SendServer(false);
        }

        private static int TryGetPlayerIdFromClientId(ulong clientId)
        {
            return (StartOfRound.Instance?.ClientPlayerList.TryGetValue(clientId, out var playerId) ?? false) ? playerId : -1;
        }
    }
}