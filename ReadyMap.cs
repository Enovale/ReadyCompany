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

        public bool Empty => ClientIds.Length <= 0 || ReadyStates.Length <= 0;

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
            ReadyCompany.Logger.LogDebug($"Equals triggered - this: {string.Join(", ", this.ClientIds)} | {string.Join(", ", ReadyStates)}");
            if (obj is ReadyMap d)
            {
                ReadyCompany.Logger.LogDebug(
                    $"Equals triggered - obj: {string.Join(", ", d.ClientIds)} | {string.Join(", ", d.ReadyStates)}");
                return d.ClientIds.SequenceEqual(ClientIds) && d.ReadyStates.SequenceEqual(ReadyStates);
            }

            return base.Equals(obj);
        }

        public static bool operator ==(ReadyMap obj1, ReadyMap obj2)
        {
            return obj1.Equals(obj2);
        }

        public static bool operator !=(ReadyMap obj1, ReadyMap obj2)
        {
            return !(obj1 == obj2);
        }
    }
}