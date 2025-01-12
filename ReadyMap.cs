using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.Netcode;
using UnityEngine.InputSystem.Utilities;

namespace ReadyCompany
{
    [Serializable]
    public class ReadyMap
    {
        public int PlayersReady;
        public int LobbySize;
        public ulong[] ClientIds = null!;
        public bool[] ReadyStates = null!;

        public bool LocalPlayerReady
        {
            get
            {
                var clientIndex = ClientIds.IndexOf(NetworkManager.Singleton.LocalClientId);
                return ClientIds.Length > 0 && ReadyStates.Length > clientIndex && ReadyStates[clientIndex];
            }
        }

        public ReadyMap()
        {
            
        }

        public ReadyMap(IDictionary<ulong, bool> readyMap)
        {
            PlayersReady = readyMap.Count(kvp => kvp.Value);
            LobbySize = readyMap.Count;
            ClientIds = readyMap.Keys.ToArray();
            ReadyStates = readyMap.Values.ToArray();
        }

        public override bool Equals(object? obj)
        {
            return obj is IDictionary<ulong, bool> d ? d.Keys.SequenceEqual(ClientIds) && d.Values.SequenceEqual(ReadyStates) : base.Equals(obj);
        }
    }
}