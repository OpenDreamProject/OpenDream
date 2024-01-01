using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Utility;

namespace OpenDreamClient.Rendering;

/// <summary>
/// Disables RobustToolbox's transform lerping and replaces it with our own gliding
/// </summary>
public sealed class AtomGlideSystem : EntitySystem {
    private sealed class Glide(TransformComponent transform) {
        public readonly TransformComponent Transform = transform;
        public Vector2 EndPos;
        public float MovementSpeed;
    }

    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    private EntityQuery<DMISpriteComponent> _spriteQuery;

    private readonly List<Glide> _currentGlides = new();

    /// <summary>
    /// Ignore MoveEvent when this is true.
    /// Prevents an infinite loop when setting the position within the event handler.
    /// </summary>
    private bool _ignoreMoveEvent;

    public override void Initialize() {
        _spriteQuery = _entityManager.GetEntityQuery<DMISpriteComponent>();

        SubscribeLocalEvent<TransformComponent, MoveEvent>(OnTransformMove);
    }

    public override void Shutdown() {
        _currentGlides.Clear();
    }

    public override void FrameUpdate(float frameTime) {
        _ignoreMoveEvent = false;

        for (int i = 0; i < _currentGlides.Count; i++) {
            var glide = _currentGlides[i];
            var currentPos = glide.Transform.LocalPosition;
            var newPos = currentPos;
            var movement = glide.MovementSpeed * frameTime;

            // Move X towards the end position at a constant speed
            if (!MathHelper.CloseTo(currentPos.X, glide.EndPos.X)) {
                if (currentPos.X < glide.EndPos.X)
                    newPos.X = Math.Min(glide.EndPos.X, newPos.X + movement);
                else if (currentPos.X > glide.EndPos.X)
                    newPos.X = Math.Max(glide.EndPos.X, newPos.X - movement);
            }

            // Move Y towards the end position at a constant speed
            if (!MathHelper.CloseTo(currentPos.Y, glide.EndPos.Y)) {
                if (currentPos.Y < glide.EndPos.Y)
                    newPos.Y = Math.Min(glide.EndPos.Y, newPos.Y + movement);
                else if (currentPos.Y > glide.EndPos.Y)
                    newPos.Y = Math.Max(glide.EndPos.Y, newPos.Y - movement);
            }

            if (newPos.EqualsApprox(glide.EndPos)) { // Glide is finished
                newPos = glide.EndPos;

                _currentGlides.RemoveSwap(i--);
            }

            _ignoreMoveEvent = true;
            _transformSystem.SetLocalPositionNoLerp(glide.Transform, newPos);
            _ignoreMoveEvent = false;
        }
    }

    // TODO: This kinda fights RT. Would be nice to modify RT to make it play nicer.
    /// <summary>
    /// Disables RT lerping and sets up the entity's glide
    /// </summary>
    private void OnTransformMove(EntityUid entity, TransformComponent transform, ref MoveEvent e) {
        if (_ignoreMoveEvent || e.ParentChanged)
            return;
        if (!_spriteQuery.TryGetComponent(entity, out var sprite) || sprite.Icon?.Appearance is null)
            return;

        _ignoreMoveEvent = true;

        // Look for any in-progress glides on this transform
        Glide? glide = null;
        foreach (var potentiallyThisTransform in _currentGlides) {
            if (potentiallyThisTransform.Transform != transform)
                continue;

            glide = potentiallyThisTransform;
            break;
        }

        var startingFrom = glide?.EndPos ?? e.OldPosition.Position;
        var glidingTo = e.NewPosition.Position;

        // Moving a greater distance than 2 tiles. Don't glide.
        // TODO: Support step_size values (I think that's what decides whether or not to glide?)
        if ((glidingTo - startingFrom).Length() > 2f) {
            // Stop the current glide if there is one
            if (glide != null)
                _currentGlides.Remove(glide);

            _ignoreMoveEvent = false;
            return;
        }

        if (glide == null) {
            glide = new(transform);
            _currentGlides.Add(glide);
        }

        // Move the transform to our starting point
        // Also serves the function of disabling RT's lerp
        _transformSystem.SetLocalPositionNoLerp(entity, startingFrom);

        glide.EndPos = glidingTo;
        glide.MovementSpeed = CalculateMovementSpeed(sprite.Icon.Appearance.GlideSize);
        _ignoreMoveEvent = false;
    }

    private static float CalculateMovementSpeed(byte glideSize) {
        if (glideSize == 0)
            glideSize = 4; // TODO: 0 gives us "automated control" over this value, not just setting it to 4

        // Assume a 20 TPS server
        // TODO: Support other TPS
        return (float)glideSize / EyeManager.PixelsPerMeter * 20f;
    }
}
