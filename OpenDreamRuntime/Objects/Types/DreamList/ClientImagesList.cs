
using OpenDreamRuntime.Rendering;
namespace OpenDreamRuntime.Objects.Types;

// client.images list
public sealed class ClientImagesList(DreamObjectTree objectTree, ServerClientImagesSystem? clientImagesSystem, DreamConnection connection) : DreamList(objectTree.List.ObjectDefinition, 0) {
    private readonly List<DreamValue> _imageObjects = new();

    public override DreamValue GetElement(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var imageIndex) || imageIndex < 1 || imageIndex > _imageObjects.Count)
            throw new DMException($"Invalid index into client images list: {key}");

        var value = _imageObjects[imageIndex - 1];
        value.IncRef();
        return value;
    }

    public override List<DreamValue> GetKeys() {
        return _imageObjects;
    }

    public override IEnumerable<DreamValue> EnumerateKeys() {
        return _imageObjects;
    }

    public override void SetKeyValuePair(DreamValue key, DreamValue value, bool allowGrowth = false) {
        throw new DMException("Cannot write to an index of a client images list");
    }

    public override void AddKey(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectImage>(out var image))
            return;

        clientImagesSystem?.AddImageObject(connection, image);
        _imageObjects.Add(value);
    }

    public override void RemoveKey(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectImage>(out var image))
            return;

        clientImagesSystem?.RemoveImageObject(connection, image);
        _imageObjects.Remove(value);
    }

    public override void Cut(int start = 1, int end = 0) {
        if (end == 0 || end > _imageObjects.Count + 1) end = _imageObjects.Count + 1;

        for (int i = start - 1; i < end - 1; i++) {
            if (!_imageObjects[i].TryGetValueAsDreamObject<DreamObjectImage>(out var image))
                continue;

            clientImagesSystem?.RemoveImageObject(connection, image);
        }

        _imageObjects.RemoveRange(start - 1, end - start);
    }

    public override int GetLength() {
        return _imageObjects.Count;
    }

    public override int FindKeyIndex(DreamValue value, int start = 1, int end = 0) {
        throw new NotImplementedException($".Find() is not yet implemented on {GetType()}");
    }

    public override bool ContainsKey(DreamValue value) => _imageObjects.Contains(value);
}
