using OpenDreamShared.Dream;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectFilter : DreamObject {
    public static readonly Dictionary<DreamFilter, DreamFilterList> FilterAttachedTo = new();

    public override bool ShouldCallNew => false;

    public DreamFilter Filter;

    public DreamObjectFilter(DreamObjectDefinition objectDefinition) : base(objectDefinition) {

    }

    protected override void HandleDeletion(bool possiblyThreaded) {
        // SAFETY: Attachment dictionary is not threadsafe, no reason to change this.
        if (possiblyThreaded) {
            EnterIntoDelQueue();
            return;
        }

        base.HandleDeletion(possiblyThreaded);

        FilterAttachedTo.Remove(Filter);
    }

    // TODO: Variable getting

    protected override void SetVar(string varName, DreamValue value) {
        if (FilterAttachedTo.TryGetValue(Filter, out var attachedTo)) {
            int index = attachedTo.GetIndexOfFilter(Filter);
            Type filterType = Filter.GetType();

            // Create a new mapping with the modified value and replace the DreamFilter with it
            MappingDataNode mapping = (MappingDataNode)SerializationManager.WriteValue(filterType, Filter);
            mapping.Remove(varName);
            mapping.Add(varName, new DreamValueDataNode(value));
            if (SerializationManager.Read(filterType, mapping) is not DreamFilter newFilter)
                return;
            if (newFilter.Equals(Filter)) // No change
                return;

            Filter = newFilter;
            attachedTo.SetFilter(index, newFilter);
        }
    }
}
