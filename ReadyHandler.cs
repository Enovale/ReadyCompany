using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public static LNetworkVariable<ReadyMap> ReadyStatus { get; } =
            LNetworkVariable<ReadyMap>.Connect(identifier: READY_STATUS_SIG, onValueChanged: ReadyStatusChanged);

        private static readonly LNetworkMessage<bool> readyUpMessage =
            LNetworkMessage<bool>.Connect(identifier: READY_EVENT_SIG, ReadyUpFromClient);

        private static readonly Dictionary<int, bool> _playerReadyMap = new();

        internal static bool ShouldPlaySound { get; set; }

        public static event Action<ReadyMap>? NewReadyStatus;

        public static bool InVotingPhase
        {
            get
            {
                if (StartOfRound.Instance == null)
                    return false;

                if (!StartOfRound.Instance.shipLeftAutomatically &&
                    !StartOfRound.Instance.newGameIsLoading &&
                    !StartOfRound.Instance.shipIsLeaving &&
                    (StartOfRound.Instance.inShipPhase && !StartOfRound.Instance.shipHasLanded ||
                     (StartOfRound.Instance.currentLevel.levelID == 3 &&
                      StartOfRound.Instance.shipHasLanded)))
                    return true;

                return false;
            }
        }

        public static ulong? ActualLocalClientId => StartOfRound.Instance?.localPlayerController?.actualClientId;

        public static int? LocalPlayerId
        {
            get
            {
                if (!ActualLocalClientId.HasValue)
                    return null;

                if (StartOfRound.Instance?.ClientPlayerList.TryGetValue((ulong)ActualLocalClientId, out var result) !=
                    null)
                    return result;

                return null;
            }
        }

        private static bool LocalPlayerAbleToVote => StartOfRound.Instance?.localPlayerController is
            { isTypingChat: false, inTerminalMenu: false, inSpecialMenu: false, quickMenuManager.isMenuOpen: false };

        private static bool LocalPlayerDead => StartOfRound.Instance?.localPlayerController is
            { isPlayerDead: true, isPlayerControlled: false };

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
                ReadyCompany.InputActions.ReadyInput.started += context =>
                    InteractionBarUI.Instance.ReadyInteraction = InputStateChanged(context);
                ReadyCompany.InputActions.ReadyInput.canceled += context =>
                    InteractionBarUI.Instance.ReadyInteraction = InputStateChanged(null);
                ReadyCompany.InputActions.UnreadyInput.performed += UnreadyInputPerformed;
                ReadyCompany.InputActions.UnreadyInput.started += context =>
                    InteractionBarUI.Instance.UnreadyInteraction = InputStateChanged(context);
                ReadyCompany.InputActions.UnreadyInput.canceled += context =>
                    InteractionBarUI.Instance.UnreadyInteraction = InputStateChanged(null);
                ReadyCompany.Config.UpdateCustomInteractionStringsBasedOnPresets();
                ReadyCompany.Config.UpdateBindingsInteractions();
            }
        }

        private static IInputInteraction? InputStateChanged(InputAction.CallbackContext? context)
        {
            if (StartOfRound.Instance == null)
                return null;

            if (!LocalPlayerAbleToVote)
            {
                ReadyCompany.InputActions?.ReadyInput.Disable();
                ReadyCompany.InputActions?.ReadyInput.Enable();
                ReadyCompany.InputActions?.UnreadyInput.Disable();
                ReadyCompany.InputActions?.UnreadyInput.Enable();

                return null;
            }

            return context?.interaction;
        }

        public static bool IsLobbyReady(ReadyMap? map) => map is { LobbySize: > 0 } &&
                                                          (float)map.PlayersReady / map.LobbySize >=
                                                          ReadyCompany.Config.PercentageForReady.Value / 100f;

        public static bool IsLobbyReady() => IsLobbyReady(ReadyStatus.Value);

        // Don't really like this method of forcing an update even if nothing's changed
        // (noone ready, reset and verify = noone ready still)
        public static void ResetReadyUp()
        {
            ReadyCompany.Logger.LogDebug($"ReadyUp reset: {ReadyStatus.Value}");
            if (NetworkManager.Singleton != null && LNetworkUtils.IsHostOrServer)
            {
                ReadyCompany.Logger.LogDebug($"Real reset!");
                _playerReadyMap.Clear();
                UpdateReadyMap();
            }

            StartMatchLeverPatches.HasShownReadyWarning = false;
            ForceReadyStatusChanged();
            ReadyCompany.Logger.LogDebug($"Reset done: {ReadyStatus.Value}");
        }

        internal static void OnClientConnected() => UpdateReadyMap();

        internal static void OnClientDisconnected() => UpdateReadyMap();

        private static void ReadyStatusChanged(ReadyMap oldValue, ReadyMap? newValue)
        {
            if (newValue == null)
            {
                ReadyCompany.Logger.LogDebug("ReadyStatusChanged newValue is null");
                return;
            }

            ReadyCompany.Logger.LogDebug($"Readyup about to change: {newValue}");
            ReadyStatusChangedReal(newValue);
        }

        internal static void ReadyStatusChangedReal(ReadyMap newValue)
        {
            if (StartOfRound.Instance == null || HUDManager.Instance == null)
                return;

            ReadyCompany.Logger.LogDebug($"Readyup changed: {newValue}");
            PopupReadyStatus(newValue);
            NewReadyStatus?.Invoke(newValue);
            UpdateShipLever(newValue);
        }

        internal static void ForceReadyStatusChanged()
        {
            ShouldPlaySound = false;
            ReadyStatusChangedReal(ReadyStatus.Value);
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
            CustomDisplayTip(IsLobbyReady() ? "Lobby is Ready!" : "Lobby not ready yet!", statusText, sfx);
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

        internal static string GetBriefStatusDisplay(ReadyMap map)
        {
            var str = new StringBuilder()
                .Append($"{map.PlayersReady} / {map.LobbySize} Players are ready. ")
                .Append(map.LocalPlayerReady ? "<color=\"green\">\u2713</color>" : "<color=\"red\">\u2716</color>")
                .Append("\n");
            if (ReadyCompany.Config.DeadPlayersCanVote.Value && !LocalPlayerDead)
            {
                str.Append(map.LocalPlayerReady
                    ? $"{ReadyCompany.InputActions?.UnreadyInputName} to Unready!"
                    : $"{ReadyCompany.InputActions?.ReadyInputName} to Ready Up!");
            }
            return str.ToString();
        }

        internal static bool ShouldOverrideLeverState(ReadyMap map) => ReadyCompany.Config.RequireReadyToStart.Value &&
                                                                       InVotingPhase &&
                                                                       !IsLobbyReady(map) &&
                                                                       !StartOfRound.Instance.travellingToNewLevel;

        internal static void UpdateShipLever(ReadyMap map)
        {
            if (StartOfRound.Instance == null)
                return;

            var shouldOverrideLeverState = ShouldOverrideLeverState(map);
            var lever = UnityEngine.Object.FindObjectOfType<StartMatchLever>();
            if (shouldOverrideLeverState)
            {
                lever.triggerScript.disabledHoverTip = LEVER_DISABLED_TIP;
                lever.triggerScript.hoverTip = LEVER_WARNING_TIP;
                lever.triggerScript.interactable = LNetworkUtils.IsHostOrServer;
            }
            else if (string.Equals(lever.triggerScript.disabledHoverTip, LEVER_DISABLED_TIP) ||
                     string.Equals(lever.triggerScript.disabledHoverTip, LEVER_WARNING_TIP) ||
                     string.Equals(lever.triggerScript.hoverTip, LEVER_WARNING_TIP))
            {
                lever.triggerScript.disabledHoverTip = string.Empty;
                lever.triggerScript.hoverTip = string.Empty;
                lever.triggerScript.interactable = true;
                lever.updateInterval = 0f;
            }

            // This code gets run for every client connected including the host
            // The game seems to handle this gracefully but perhaps this causes some edge case issues?
            // Every client has to run this in case the host is dead (the lever doesn't let you pull it if you're dead)
            if (ReadyCompany.Config.AutoStartWhenReady.Value && InVotingPhase && !StartOfRound.Instance.travellingToNewLevel && IsLobbyReady(map))
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
            if (!LNetworkUtils.IsConnected || !LNetworkUtils.IsHostOrServer || !InVotingPhase)
                return;

            VerifyReadyUpMap();

            ReadyStatus.Value = new(_playerReadyMap);
            ReadyStatus.MakeDirty();
        }

        private static void VerifyReadyUpMap()
        {
            if (!LNetworkUtils.IsConnected && !InVotingPhase)
                return;

            var roundManager = StartOfRound.Instance;
            foreach (var (clientId, _) in _playerReadyMap.Where(kvp =>
                         kvp.Key != LocalPlayerId && !roundManager.ClientPlayerList.ContainsValue(kvp.Key)).ToList())
            {
                _playerReadyMap.Remove(clientId);
            }

            if (roundManager != null)
            {
                foreach (var (_, playerId) in roundManager.ClientPlayerList)
                {
                    _playerReadyMap.TryAdd(playerId, false);

                    if (!ReadyCompany.Config.DeadPlayersCanVote.Value)
                    {
                        var playerScript = roundManager.allPlayerScripts[playerId];
                        if (playerScript.isPlayerDead && !playerScript.isPlayerControlled)
                        {
                            _playerReadyMap[playerId] = true;
                        }
                    }
                }

                var localPlayerId = LocalPlayerId;
                if (localPlayerId.HasValue)
                    _playerReadyMap.TryAdd((int)localPlayerId, false);
            }
        }

        private static void ReadyInputPerformed(InputAction.CallbackContext context)
        {
            if (!LNetworkUtils.IsConnected || !LocalPlayerAbleToVote)
                return;

            if (ReadyStatus is { Value.LocalPlayerReady: false })
            {
                ReadyCompany.InputActions!.UnreadyInput.Disable();
                ReadyCompany.InputActions.UnreadyInput.Enable();
            }

            readyUpMessage.SendServer(true);
        }

        private static void UnreadyInputPerformed(InputAction.CallbackContext context)
        {
            if (!LNetworkUtils.IsConnected || !LocalPlayerAbleToVote)
                return;

            if (ReadyStatus is { Value.LocalPlayerReady: true })
            {
                ReadyCompany.InputActions!.ReadyInput.Disable();
                ReadyCompany.InputActions.ReadyInput.Enable();
            }

            readyUpMessage.SendServer(false);
        }

        private static int TryGetPlayerIdFromClientId(ulong clientId)
        {
            return (StartOfRound.Instance?.ClientPlayerList.TryGetValue(clientId, out var playerId) ?? false)
                ? playerId
                : -1;
        }
    }
}