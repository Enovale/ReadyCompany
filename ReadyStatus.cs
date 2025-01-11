using System;

namespace ReadyCompany
{
    [Serializable]
    public struct ReadyStatus(int playersReady, int lobbySize)
    {
        public int PlayersReady = playersReady;
        public int LobbySize = lobbySize;
    }
}