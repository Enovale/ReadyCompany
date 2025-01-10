using System.Collections.Generic;
using System.Linq;
using LethalNetworkAPI;
using LethalNetworkAPI.Utils;
using UnityEngine.InputSystem;

namespace ReadyCompany
{
    public class ReadyHandler
    {
        private const string PLAYERS_READY_SIG = MyPluginInfo.PLUGIN_GUID + "_playersReadyVar";
        private const string PLAYERS_CONNECTED_SIG = MyPluginInfo.PLUGIN_GUID + "_playersConnectedVar";
        private const string READY_EVENT_SIG = MyPluginInfo.PLUGIN_GUID + "_playerReadyEvent";

        public static LNetworkVariable<int> PlayersReady { get; } = LNetworkVariable<int>.Connect(identifier: PLAYERS_READY_SIG, onValueChanged: PlayersReadyChanged);
        public static LNetworkVariable<int> ConnectedPlayers { get; } = LNetworkVariable<int>.Connect(identifier: PLAYERS_CONNECTED_SIG, onValueChanged: PlayersConnectedChanged);

        private static readonly LNetworkMessage<bool> readyUpMessage = LNetworkMessage<bool>.Connect(identifier: READY_EVENT_SIG, ReadyUpFromClient, ReadyUpFromServer);

        private static readonly Dictionary<ulong, bool> _playerReadyMap = new();
        
        private static int ServerLobbySize => StartOfRound.Instance == null ? -1 : StartOfRound.Instance.fullyLoadedPlayers.Count;

        internal static void InitializeEvents()
        {
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

        public static void ResetReadyUp()
        {
            _playerReadyMap.Clear();
        }

        internal static void OnClientConnected()
        {
            ConnectedPlayers.Value = ServerLobbySize;
            UpdateReadyMap();
        }

        internal static void OnClientDisconnected()
        {
            ConnectedPlayers.Value = ServerLobbySize;
            UpdateReadyMap();
        }

        private static void ReadyUpFromServer(bool isReady)
        {
            ReadyCompany.Logger.LogDebug($"Ready up from server: {isReady}");
        }

        private static void PlayersReadyChanged(int oldValue, int newValue)
        {
            PopupReadyStatus(newValue, ConnectedPlayers.Value);
        }

        private static void PlayersConnectedChanged(int oldValue, int newValue)
        {
            PopupReadyStatus(PlayersReady.Value, newValue);
        }

        private static void PopupReadyStatus(int playersReady, int lobbySize)
        {
            if (HUDManager.Instance == null)
                return;
            
            HUDManager.Instance.DisplayTip("Ready Up!", $"{playersReady} / {lobbySize} Players are ready.", prefsKey: MyPluginInfo.PLUGIN_GUID + "_ReadyTip");
            HUDManager.Instance.DisplaySpectatorTip($"{playersReady} / {lobbySize} Players are ready.");
        }

        private static void ReadyUpFromClient(bool isReady, ulong clientId)
        {
            if (!StartOfRound.Instance.inShipPhase)
                return;
            
            _playerReadyMap[clientId] = isReady;

            UpdateReadyMap();
            ReadyCompany.Logger.LogDebug($"Ready up from client: {PlayersReady.Value} and {_playerReadyMap[clientId]}");
        }

        private static void UpdateReadyMap()
        {
            VerifyReadyUpMap();

            PlayersReady.Value = _playerReadyMap.Count(kvp => kvp.Value);
            ConnectedPlayers.Value = ServerLobbySize;
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