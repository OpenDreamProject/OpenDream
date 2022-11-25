using OpenDreamShared.Dream;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectFilter : IDreamMetaObject {
        public bool ShouldCallNew => false;
        public IDreamMetaObject? ParentType { get; set; }

        [Dependency] private readonly ISerializationManager _serializationManager = default!;

        public static readonly Dictionary<DreamObject, DreamFilter> DreamObjectToFilter = new();
        public static readonly Dictionary<DreamFilter, DreamFilterList> FilterAttachedTo = new();

        public DreamMetaObjectFilter() {
            IoCManager.InjectDependencies(this);
        }

        public void OnVariableSet(DreamObject dreamObject, string varName, DreamValue value, DreamValue oldValue) {
            ParentType?.OnVariableSet(dreamObject, varName, value, oldValue);

            DreamFilter filter = DreamObjectToFilter[dreamObject];

            if (FilterAttachedTo.TryGetValue(filter, out var attachedTo)) {
                int index = attachedTo.GetIndexOfFilter(filter);
                Type filterType = filter.GetType();

                // Create a new mapping with the modified value and replace the DreamFilter with it
                MappingDataNode mapping = (MappingDataNode)_serializationManager.WriteValue(filterType, filter);
                mapping.Remove(varName);
                mapping.Add(varName, new DreamValueDataNode(value));
                if (_serializationManager.Read(filterType, mapping) is not DreamFilter newFilter)
                    return;
                if (newFilter.Equals(filter)) // No change
                    return;

                DreamObjectToFilter[dreamObject] = newFilter;
                attachedTo.SetFilter(index, newFilter);
            }
        }

        public void OnObjectDeleted(DreamObject dreamObject) {
            ParentType?.OnObjectDeleted(dreamObject);
            FilterAttachedTo.Remove(DreamObjectToFilter[dreamObject]);
            DreamObjectToFilter.Remove(dreamObject);
        }
    }
}
