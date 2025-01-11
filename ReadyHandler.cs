using System.Collections.Generic;
using System.Linq;
using LethalNetworkAPI;
using LethalNetworkAPI.Utils;
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

        public static LNetworkVariable<ReadyStatus> ReadyStatus { get; } = LNetworkVariable<ReadyStatus>.Connect(identifier: READY_STATUS_SIG, onValueChanged: ReadyStatusChanged);

        private static readonly LNetworkMessage<bool> readyUpMessage = LNetworkMessage<bool>.Connect(identifier: READY_EVENT_SIG, ReadyUpFromClient, ReadyUpFromServer);

        private static readonly Dictionary<ulong, bool> _playerReadyMap = new();
        
        private static int ServerLobbySize => StartOfRound.Instance == null ? -1 : StartOfRound.Instance.fullyLoadedPlayers.Count;

        internal static void InitializeEvents()
        {
            ReadyStatus.Value = new(0, 0);
            LNetworkUtils.OnNetworkStart += b =>
            {
                UpdateReadyMap();
                
                // Specifically when using a KbmInteraction of some kind, this code needs to be deferred
                // So that the input system is initialized. I don't like it being here but it works for now...
                ReadyCompany.InputActions = new ReadyInputs();
                ReadyCompany.InputActions.ReadyInput.performed += ReadyInputPerformed;
                ReadyCompany.InputActions.UnreadyInput.performed += UnreadyInputPerformed;
            };
        }

        public static bool IsLobbyReady(ReadyStatus status) => status.PlayersReady / status.LobbySize >=
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

        private static void ReadyStatusChanged(ReadyStatus oldValue, ReadyStatus newValue)
        {
            PopupReadyStatus(newValue);
            ReadyCompany.Logger.LogDebug($"Ready status changed: {newValue.PlayersReady}, {newValue.LobbySize}");
        }

        private static void PopupReadyStatus(ReadyStatus status)
        {
            if (HUDManager.Instance == null || !StartOfRound.Instance.inShipPhase)
                return;
            
            HUDManager.Instance.DisplayTip("Ready Up!", $"{status.PlayersReady} / {status.LobbySize} Players are ready.", prefsKey: MyPluginInfo.PLUGIN_GUID + "_ReadyTip");
            HUDManager.Instance.DisplaySpectatorTip($"{status.PlayersReady} / {status.LobbySize} Players are ready.");
            UpdateShipLever(status);
        }

        private static void UpdateShipLever(ReadyStatus status)
        {
            var lever = UnityEngine.Object.FindObjectOfType<StartMatchLever>();
            var lobbyReady = IsLobbyReady(status);
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
            VerifyReadyUpMap();

            ReadyStatus.Value = new(_playerReadyMap.Count(kvp => kvp.Value), ServerLobbySize);
            ReadyStatus.MakeDirty();
        }

        private static void VerifyReadyUpMap()
        {
            foreach (var (clientid, _) in _playerReadyMap.Where(kvp => !StartOfRound.Instance.ClientPlayerList.ContainsKey(kvp.Key)))
            {
                _playerReadyMap.Remove(clientid);
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