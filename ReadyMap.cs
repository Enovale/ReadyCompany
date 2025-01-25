using System;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

namespace ReadyCompany
{
    [Serializable]
    public class ReadyMap(IDictionary<int, bool> readyMap)
        : Dictionary<int, bool>(readyMap.ToDictionary(k => k.Key, e => e.Value))
    {
        public int PlayersReady => this.Count(kvp => kvp.Value);
        public int LobbySize => Count;

        public bool LocalPlayerReady => ReadyHandler.LocalPlayerId.HasValue &&
                                        TryGetValue((int)ReadyHandler.LocalPlayerId, out var result) && result;

        // This is the only override needed to cause LethalNetworkAPI to trigger a change event
        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;

            var other = (ReadyMap)obj;

            return other.SequenceEqual(this);
        }

        public override string ToString()
        {
            return string.Join(", ", Keys.Select(k => $"{k}: {this[k]}"));
        }
    }
}