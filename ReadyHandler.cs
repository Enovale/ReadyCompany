using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LethalNetworkAPI;
using LethalNetworkAPI.Utils;
using ReadyCompany.Patches;
using Unity.Netcode;
using UnityEngine.InputSystem;

namespace ReadyCompany
{
    public class ReadyHandler
    {
        private const string READY_STATUS_SIG = MyPluginInfo.PLUGIN_GUID + "_readyStatusVar";
        private const string READY_EVENT_SIG = MyPluginInfo.PLUGIN_GUID + "_playerReadyEvent";
        
        internal const string LEVER_DISABLED_TIP = "[ Lobby must be ready to start ]";
        internal const string LEVER_WARNING_TIP = "[ WARNING: Lobby Not Ready ]";
        internal const string LEVER_NORMAL_TIP = "Start game : [LMB]";

        public static LNetworkVariable<ReadyMap> ReadyStatus { get; } = LNetworkVariable<ReadyMap>.Connect(identifier: READY_STATUS_SIG, onValueChanged: ReadyStatusChanged);

        private static readonly LNetworkMessage<bool> readyUpMessage = LNetworkMessage<bool>.Connect(identifier: READY_EVENT_SIG, ReadyUpFromClient, ReadyUpFromServer);

        private static readonly Dictionary<ulong, bool> _playerReadyMap = new();
        
        public static event Action<ReadyMap>? NewReadyStatus;

        internal static void InitializeEvents()
        {
            ReadyStatus.Value = new(_playerReadyMap);
            LNetworkUtils.OnNetworkStart += b =>
            {
                UpdateReadyMap();
                
                // Specifically when using a KbmInteraction of some kind, this code needs to be deferred
                // So that the input system is initialized. I don't like it being here but it works for now...
                ReadyCompany.InputActions = new ReadyInputs();
                ReadyCompany.InputActions.ReadyInput.performed += ReadyInputPerformed;
                ReadyCompany.InputActions.UnreadyInput.performed += UnreadyInputPerformed;
            };
            NewReadyStatus += map => { if (HUDPatches.ReadyStatusTextMesh != null) HUDPatches.ReadyStatusTextMesh.text = GetBriefStatusDisplay(map); };
        }

        public static bool IsLobbyReady(ReadyMap map) => map.LobbySize > 0 && map.PlayersReady / map.LobbySize >=
            ReadyCompany.Config.PercentageForReady.Value / 100;

        public static void ResetReadyUp()
        {
            _playerReadyMap.Clear();
            UpdateReadyMap();
        }

        internal static void OnClientConnected()
        {
            UpdateReadyMap();
        }

        internal static void OnClientDisconnected()
        {
            UpdateReadyMap();
        }

        private static void ReadyUpFromServer(bool isReady)
        {
            ReadyCompany.Logger.LogDebug($"Ready up from server: {isReady}");
        }

        private static void ReadyStatusChanged(ReadyMap oldValue, ReadyMap? newValue)
        {
            if (newValue == null || newValue.Empty)
                return;
            
            PopupReadyStatus(newValue);
            NewReadyStatus?.Invoke(newValue);
            ReadyCompany.Logger.LogDebug($"Ready status changed: {newValue.PlayersReady}, {newValue.LobbySize}");
        }

        internal static void PopupReadyStatus(ReadyMap map)
        {
            if (HUDPatches.ReadyStatusTextMesh != null && HUDPatches.ReadyStatusTextMesh.enabled)
                HUDPatches.ReadyStatusTextMesh.text = GetBriefStatusDisplay(map);
            
            if (HUDManager.Instance == null || !StartOfRound.Instance.inShipPhase)
            {
                if (HUDPatches.ReadyStatusTextMesh != null)
                    HUDPatches.ReadyStatusTextMesh.enabled = false;
                return;
            }

            ReadyCompany.Logger.LogDebug($"Test map: {map}");
            ReadyCompany.Logger.LogDebug($"Test map PlayersReady: {map.PlayersReady}");
            ReadyCompany.Logger.LogDebug($"Test map LobbySize: {map.LobbySize}");
            ReadyCompany.Logger.LogDebug($"Test map ReadyStates: {map.ReadyStates}");
            ReadyCompany.Logger.LogDebug($"Test map ClientIds: {map.ClientIds}");
            HUDManager.Instance.DisplayTip("Ready Up!", GetBriefStatusDisplay(map), prefsKey: MyPluginInfo.PLUGIN_GUID + "_ReadyTip");
            HUDManager.Instance.DisplaySpectatorTip($"{map.PlayersReady} / {map.LobbySize} Players are ready.");
            UpdateShipLever(map);
        }

        internal static string GetBriefStatusDisplay(ReadyMap map) =>
            $"{map.PlayersReady} / {map.LobbySize} Players are ready.\n" +
            (map.LocalPlayerReady ? $"Triple tap your Ready bind to Unready!" : $"Hold your Ready bind to Ready Up!");

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
            _playerReadyMap[clientId] = isReady;

            UpdateReadyMap();
            ReadyCompany.Logger.LogDebug($"Ready up from client: {ReadyStatus.Value} and {_playerReadyMap[clientId]}");
        }

        public static void UpdateReadyMap()
        {
            ReadyCompany.Logger.LogDebug($"Ready Map State before verify: {string.Join(", ", _playerReadyMap.Keys)} | {string.Join(", ", _playerReadyMap.Values)}");
            VerifyReadyUpMap();
            ReadyCompany.Logger.LogDebug($"Ready Map State after verify: {string.Join(", ", _playerReadyMap.Keys)} | {string.Join(", ", _playerReadyMap.Values)}");
            
            ReadyStatus.Value = new(_playerReadyMap);
            ReadyStatus.MakeDirty();
        }

        private static void VerifyReadyUpMap()
        {
            foreach (var (clientid, _) in _playerReadyMap.Where(kvp => !StartOfRound.Instance.ClientPlayerList.ContainsKey(kvp.Key)).ToList())
            {
                _playerReadyMap.Remove(clientid);
            }

            if (StartOfRound.Instance != null)
            {
                foreach (var clientId in StartOfRound.Instance.ClientPlayerList.Keys)
                {
                    _playerReadyMap.TryAdd(clientId, false);
                }
            }
            
            if (NetworkManager.Singleton != null)
            {
                _playerReadyMap.TryAdd(NetworkManager.Singleton.LocalClientId, false);
            }
        }

        public static void ReadyInputPerformed(InputAction.CallbackContext context)
        {
            readyUpMessage.SendServer(true);
        }

        private static void UnreadyInputPerformed(InputAction.CallbackContext context)
        {
            readyUpMessage.SendServer(false);
        }
    }
}