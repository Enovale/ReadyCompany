using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LethalNetworkAPI;
using LethalNetworkAPI.Utils;
using ReadyCompany.Patches;
using Unity.Netcode;
using UnityEngine.InputSystem;

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
        
        public static event Action<ReadyMap>? NewReadyStatus;

        public static ulong? ActualLocalClientId => StartOfRound.Instance?.localPlayerController?.actualClientId;
        public static int? LocalPlayerId => !ActualLocalClientId.HasValue ? null : (StartOfRound.Instance?.ClientPlayerList.TryGetValue((ulong)ActualLocalClientId, out var result) ?? false ? result : null);

        internal static void InitializeEvents()
        {
            ReadyStatus.Value = new(_playerReadyMap);
            LNetworkUtils.OnNetworkStart += b =>
            {
                // Specifically when using a KbmInteraction of some kind, this code needs to be deferred
                // So that the input system is initialized. I don't like it being here but it works for now...
                if (ReadyCompany.InputActions == null)
                {
                    ReadyCompany.InputActions = new ReadyInputs();
                    ReadyCompany.InputActions.ReadyInput.performed += ReadyInputPerformed;
                    ReadyCompany.InputActions.ReadyInput.started += context =>
                    {
                        InteractionBarUI.Instance.ReadyInteraction = context.interaction;
                    };
                    ReadyCompany.InputActions.ReadyInput.canceled += context =>
                    {
                        InteractionBarUI.Instance.ReadyInteraction = null;
                    };
                    ReadyCompany.InputActions.UnreadyInput.performed += UnreadyInputPerformed;
                    ReadyCompany.InputActions.UnreadyInput.started += context =>
                    {
                        InteractionBarUI.Instance.UnreadyInteraction = context.interaction;
                    };
                    ReadyCompany.InputActions.UnreadyInput.canceled += context =>
                    {
                        InteractionBarUI.Instance.UnreadyInteraction = null;
                    };
                    ReadyCompany.Config.UpdateBindingsBasedOnConfig();
                }
            };
            NewReadyStatus += HUDPatches.UpdateTextBasedOnStatus;
        }

        public static bool IsLobbyReady(ReadyMap map) => map.LobbySize > 0 && map.PlayersReady / map.LobbySize >=
            ReadyCompany.Config.PercentageForReady.Value / 100;

        // Don't really like this method of forcing an update even if nothing's changed
        // (noone ready, reset and verify = noone ready still)
        public static void ResetReadyUp()
        {
            if (NetworkManager.Singleton != null && LNetworkUtils.IsHostOrServer)
            {
                _playerReadyMap.Clear();
                UpdateReadyMap();
            }

            ReadyStatusChangedReal(ReadyStatus.Value);
            ReadyCompany.Logger.LogDebug("Resetting ready-up");
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
            
            ReadyStatusChangedReal(newValue);
        }

        internal static void ReadyStatusChangedReal(ReadyMap newValue)
        {
            PopupReadyStatus(newValue);
            NewReadyStatus?.Invoke(newValue);
        }

        internal static void PopupReadyStatus(ReadyMap map)
        {
            if (HUDManager.Instance == null || !StartOfRound.Instance.inShipPhase)
                return;

            HUDManager.Instance.DisplayTip("Ready Up!", GetBriefStatusDisplay(map), prefsKey: MyPluginInfo.PLUGIN_GUID + "_ReadyTip");
            //HUDManager.Instance.DisplaySpectatorTip($"{map.PlayersReady} / {map.LobbySize} Players are ready.");
            UpdateShipLever(map);
        }

        internal static string GetBriefStatusDisplay(ReadyMap map) =>
            $"{map.PlayersReady} / {map.LobbySize} Players are ready.\n" +
            (map.LocalPlayerReady ? $"{ReadyCompany.InputActions?.UnreadyInputName} to Unready!" : $"{ReadyCompany.InputActions?.ReadyInputName} to Ready Up!");

        private static void UpdateShipLever(ReadyMap map)
        {
            var lever = UnityEngine.Object.FindObjectOfType<StartMatchLever>();
            var lobbyReady = IsLobbyReady(map);
            if (ReadyCompany.Config.RequireReadyToStart.Value)
            {
                lever.triggerScript.disabledHoverTip = lobbyReady ? "" : LEVER_DISABLED_TIP;
                lever.triggerScript.hoverTip = lobbyReady ? LEVER_NORMAL_TIP : LEVER_WARNING_TIP;
                lever.triggerScript.interactable = lobbyReady || LNetworkUtils.IsHostOrServer;
            }
            else if (lever.triggerScript.disabledHoverTip == LEVER_DISABLED_TIP)
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
            VerifyReadyUpMap();
            
            ReadyStatus.Value = new(_playerReadyMap);
            ReadyStatus.MakeDirty();
        }

        private static void VerifyReadyUpMap()
        {
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
            if (!ReadyStatus.Value.LocalPlayerReady)
            {
                ReadyCompany.InputActions?.UnreadyInput.Disable();
                Task.Run(ReenableInputs);
            }
            
            readyUpMessage.SendServer(true);
        }

        private static void UnreadyInputPerformed(InputAction.CallbackContext context)
        {
            if (ReadyStatus.Value.LocalPlayerReady)
            {
                ReadyCompany.InputActions?.ReadyInput.Disable();
                Task.Run(ReenableInputs);
            }
            
            readyUpMessage.SendServer(false);
        }

        private static async Task ReenableInputs()
        {
            await Task.Delay(500);
            ReadyCompany.InputActions?.ReadyInput.Enable();
            ReadyCompany.InputActions?.UnreadyInput.Enable();
        }

        private static int TryGetPlayerIdFromClientId(ulong clientId)
        {
            return (StartOfRound.Instance?.ClientPlayerList.TryGetValue(clientId, out var playerId) ?? false) ? playerId : -1;
        }
    }
}