using OpenDreamShared.Dream;
using Robust.Client.GameObjects;
using Robust.Shared.Graphics;
using Robust.Shared.Map;

namespace OpenDreamClient.Rendering;

public sealed class DreamClientEye: IEye {
    private readonly ClientObjectReference _eyeRef;
    private readonly IEntityManager _entityManager;
    private readonly TransformSystem _transformSystem;

    public DreamClientEye(IEye baseEye, ClientObjectReference eyeRef, IEntityManager entityManager, TransformSystem transformSystem) {
        Rotation = baseEye.Rotation;
        Scale = baseEye.Scale;
        DrawFov = baseEye.DrawFov;
        DrawLight = baseEye.DrawLight;

        _eyeRef = eyeRef;
        _entityManager = entityManager;
        _transformSystem = transformSystem;
    }

    [ViewVariables(VVAccess.ReadOnly)]
    public MapCoordinates Position {
        get {
            switch (_eyeRef.Type) {
                case ClientObjectReference.RefType.Entity:
                    var ent = _entityManager.GetEntity(_eyeRef.Entity);
                    if (_entityManager.TryGetComponent<TransformComponent>(ent, out var pos)) {
                        return _transformSystem.GetMapCoordinates(pos);
                    }

                    break;

                case ClientObjectReference.RefType.Turf:
                    return new(new(_eyeRef.TurfX, _eyeRef.TurfY), new(_eyeRef.TurfZ));
            }

            return MapCoordinates.Nullspace;
        }
    }

    public void GetViewMatrix(out Matrix3x2 viewMatrix, Vector2 renderScale) {
        var pos = Position;
        viewMatrix = Matrix3Helpers.CreateInverseTransform(
            pos.X + Offset.X,
            pos.Y + Offset.Y,
            (float)-Rotation.Theta,
            1 / (Scale.X * renderScale.X),
            1 / (Scale.Y * renderScale.Y));
    }

    public void GetViewMatrixNoOffset(out Matrix3x2 viewMatrix, Vector2 renderScale) {
        var pos = Position;
        viewMatrix = Matrix3Helpers.CreateInverseTransform(
            pos.X,
            pos.Y,
            (float)-Rotation.Theta,
            1 / (Scale.X * renderScale.X),
            1 / (Scale.Y * renderScale.Y));
    }

    public void GetViewMatrixInv(out Matrix3x2 viewMatrixInv, Vector2 renderScale) {
        var pos = Position;
        viewMatrixInv = Matrix3Helpers.CreateTransform(
            pos.X + Offset.X,
            pos.Y + Offset.Y,
            (float)-Rotation.Theta,
            1 / (Scale.X * renderScale.X),
            1 / (Scale.Y * renderScale.Y));
    }

    private Vector2 _scale = Vector2.One / 2f;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool DrawFov { get; set; }

    [ViewVariables]
    public bool DrawLight { get; set; }

    [ViewVariables(VVAccess.ReadWrite)]
    public Vector2 Offset { get; set; }

    [ViewVariables(VVAccess.ReadWrite)]
    public Angle Rotation { get; set; }

    [ViewVariables(VVAccess.ReadWrite)]
    public Vector2 Zoom {
        get => new(1 / _scale.X, 1 / _scale.Y);
        set => _scale = new Vector2(1 / value.X, 1 / value.Y);
    }

    [ViewVariables(VVAccess.ReadWrite)]
    public Vector2 Scale {
        get => _scale;
        set => _scale = value;
    }
}
