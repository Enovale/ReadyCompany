using System.Collections.Generic;
using System.Linq;
using LethalNetworkAPI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace ReadyCompany
{
    public class ReadyHandler
    {
        private const string PLAYERS_READY_SIG = MyPluginInfo.PLUGIN_GUID + "_playersReadyVar";
        private const string READY_EVENT_SIG = MyPluginInfo.PLUGIN_GUID + "_playerReadyEvent";

        public static LethalNetworkVariable<int> PlayersReady { get; } = new(identifier: PLAYERS_READY_SIG);
        
        private static LethalServerMessage<bool> readyUpServerMessage = new(identifier: READY_EVENT_SIG);
        private static LethalClientMessage<bool> readyUpClientMessage = new(identifier: READY_EVENT_SIG);

        private static Dictionary<ulong, bool> _playerReadyMap = new();

        private static int LobbySize => StartOfRound.Instance.connectedPlayersAmount + 1;

        internal static void InitializeEvents()
        {
            ReadyCompany.InputActions.ReadyKey.performed += ReadyKeyPerformed;
            readyUpServerMessage.OnReceived += ReadyUpFromClient;
            readyUpClientMessage.OnReceived += ReadyUpFromServer;
            PlayersReady.OnValueChanged += PlayersReadyChanged;
            PlayersReady.Value = 0;
        }

        private static void ReadyUpFromServer(bool isReady)
        {
            ReadyCompany.Logger.LogDebug("Ready up from server!");
        }

        private static void PlayersReadyChanged(int playersReady)
        {
            if (HUDManager.Instance == null)
                return;
            
            HUDManager.Instance.DisplayTip("Ready Up!", $"{PlayersReady.Value} / {LobbySize} Players are ready.", prefsKey: MyPluginInfo.PLUGIN_GUID + "_ReadyTip");
            HUDManager.Instance.DisplaySpectatorTip($"{PlayersReady.Value} / {LobbySize} Players are ready.");
        }

        // Sloppy, doesn't tell clients about any errors caught but I don't wanna cause extra tip noises
        // Unless something has actually changed, maybe use the server message data that is currently unused
        // To encode if something actually changed and only play a sound if that's true
        private static void ReadyUpFromClient(bool isReady, ulong clientId)
        {
            _playerReadyMap[clientId] = isReady;

            VerifyReadyUpMap();

            PlayersReady.Value = _playerReadyMap.Count(kvp => kvp.Value);
            ReadyCompany.Logger.LogDebug($"Ready up from client: {PlayersReady.Value} and {_playerReadyMap[clientId]}");
        }

        private static void VerifyReadyUpMap()
        {
            foreach (var (clientid, _) in _playerReadyMap.Where(kvp => !StartOfRound.Instance.ClientPlayerList.ContainsKey(kvp.Key)))
            {
                _playerReadyMap.Remove(clientid);
            }
        }

        public static void ReadyKeyPerformed(InputAction.CallbackContext context)
        {
            readyUpClientMessage.SendServer(true);
        }
    }
}