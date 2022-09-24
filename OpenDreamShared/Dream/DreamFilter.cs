using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;

namespace OpenDreamShared.Dream {
    [Serializable, NetSerializable]
    public sealed class DreamFilter : IEquatable<DreamFilter> {
        public int usageCount = 0; //this may not be the best way of doing this
        [ViewVariables] public string? filter_type = null;
        [ViewVariables] public float? filter_size = null; 
        [ViewVariables] public float? filter_flags = null; 
        [ViewVariables] public string? filter_color = null; 
        public DreamFilter() { }

        public override bool Equals(object obj) => obj is DreamFilter filter && Equals(filter);

        public bool Equals(DreamFilter filter) {
            if(filter.filter_type != filter_type) return false;

            return true;
        }

        public override int GetHashCode() {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            return $"[type: {filter_type}, size: {filter_size}, flags: {filter_flags}, color: {filter_color}]";
        }


    }

    
}