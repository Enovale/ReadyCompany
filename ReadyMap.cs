using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

namespace ReadyCompany
{
    [Serializable]
    public class ReadyMap : Dictionary<int, bool>
    {
        public int PlayersReady => this.Count(kvp => kvp.Value);
        public int LobbySize => Count;

        public bool LocalPlayerReady => ReadyHandler.LocalPlayerId.HasValue &&
                                        TryGetValue((int)ReadyHandler.LocalPlayerId, out var result) && result;

        public float Timestamp = Time.time;
        
        protected ReadyMap(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            Timestamp = info.GetSingle(nameof(Timestamp));
        }

        public ReadyMap(IDictionary<int, bool> readyMap) : base(readyMap.ToDictionary(k => k.Key, e => e.Value))
        {
        }

        // This is the only override needed to cause LethalNetworkAPI to trigger a change event
        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;

            var other = (ReadyMap)obj;

            return Mathf.Approximately(other.Timestamp, Timestamp) && other.SequenceEqual(this);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Timestamp), Timestamp);
            base.GetObjectData(info, context);
        }

        public override string ToString()
        {
            return $"ReadyMap ({Timestamp}) - " + string.Join(", ", Keys.Select(k => $"{k}: {this[k]}"));
        }
    }
}