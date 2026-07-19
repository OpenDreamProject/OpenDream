using OpenDreamRuntime.Rendering;

namespace OpenDreamRuntime.Objects.Types;

public sealed class ClientImagesList(DreamObjectTree objectTree, ServerClientImagesSystem? clientImagesSystem, DreamConnection connection) : BaseDreamList(objectTree.List.ObjectDefinition) {
    private readonly List<DreamValue> _imageObjects = new();

    public override DreamValue GetValue(DreamValue key) {
        if (!key.TryGetValueAsInteger(out var imageIndex) || imageIndex < 1 || imageIndex > _imageObjects.Count)
            throw new Exception($"Invalid index into client images list: {key}");

        var value = _imageObjects[imageIndex - 1];
        value.IncRef();
        return value;
    }

    public override List<DreamValue> GetValues() {
        return _imageObjects;
    }

    public override IEnumerable<DreamValue> EnumerateValues() {
        return _imageObjects;
    }

    public override void SetValue(DreamValue key, DreamValue value, bool allowGrowth = false) {
        throw new Exception("Cannot write to an index of a client images list");
    }

    public override void AddValue(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectImage>(out var image))
            return;

        clientImagesSystem?.AddImageObject(connection, image);
        _imageObjects.Add(value);
    }

    public override void RemoveValue(DreamValue value) {
        if (!value.TryGetValueAsDreamObject<DreamObjectImage>(out var image))
            return;

        clientImagesSystem?.RemoveImageObject(connection, image);
        _imageObjects.Remove(value);
    }

    public override int GetLength() {
        return _imageObjects.Count;
    }

    public override int FindValue(DreamValue value, int start = 1, int end = 0) {
        throw new NotImplementedException($".Find() is not yet implemented on {GetType()}");
    }

    public override bool ContainsValue(DreamValue value) => _imageObjects.Contains(value);

    public override void Resize(int size) {
        if(size > _imageObjects.Count)
            throw new InvalidOperationException("client images lists cannot grow, only shrink");

        Cut(end: 0);
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
}
