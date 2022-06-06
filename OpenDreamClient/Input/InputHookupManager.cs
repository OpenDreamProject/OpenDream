using OpenDreamClient.Interface.Controls;
using OpenDreamClient.Rendering;
using Robust.Client;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace OpenDreamClient.Input {
    public sealed class InputHookupManager : EntitySystem {
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IBaseClient _baseClient = default!;
        [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;

        public override void Initialize() {
            _inputManager.KeyBindStateChanged += OnKeyBindStateChanged;
        }

        public override void Shutdown() {
            _inputManager.KeyBindStateChanged -= OnKeyBindStateChanged;
        }

        private void OnKeyBindStateChanged(ViewportBoundKeyEventArgs args) {
            if (_baseClient.RunLevel != ClientRunLevel.InGame)
                return;

            if (!_entitySystemManager.TryGetEntitySystem<InputSystem>(out var inputSystem))
                return;

            var keyArgs = args.KeyEventArgs;
            var inputFunction = _inputManager.NetworkBindMap.KeyFunctionID(keyArgs.Function);

            EntityCoordinates coords = EntityCoordinates.Invalid;
            EntityUid entity = EntityUid.Invalid;
            if (args.Viewport is ScalingViewport viewport) {
                MapCoordinates mapCoords = viewport.ScreenToMap(keyArgs.PointerLocation.Position);

                entity = GetEntityUnderMouse(viewport, keyArgs.PointerLocation.Position, mapCoords);
                coords = _mapManager.TryFindGridAt(mapCoords, out var grid) ? grid.MapToGrid(mapCoords) :
                    EntityCoordinates.FromMap(_mapManager, mapCoords);
            }

            var message = new FullInputCmdMessage(_gameTiming.CurTick, _gameTiming.TickFraction, inputFunction, keyArgs.State, coords, keyArgs.PointerLocation, entity);
            if (inputSystem.HandleInputCommand(_playerManager.LocalPlayer.Session, keyArgs.Function, message)) {
                keyArgs.Handle();
            }
        }

        private EntityUid GetEntityUnderMouse(ScalingViewport viewport, Vector2 mousePos, MapCoordinates coords) {
            EntityUid? entity = GetEntityOnScreen(mousePos, viewport);
            entity ??= GetEntityOnMap(coords);

            return entity ?? EntityUid.Invalid;
        }

        private EntityUid? GetEntityOnScreen(Vector2 mousePos, ScalingViewport viewport) {
            ClientScreenOverlaySystem screenOverlay = EntitySystem.Get<ClientScreenOverlaySystem>();
            EntityUid? eye = _playerManager.LocalPlayer.Session.AttachedEntity;
            if (eye == null || !_entityManager.TryGetComponent<TransformComponent>(eye.Value, out var eyeTransform)) {
                return null;
            }

            Vector2 viewOffset = eyeTransform.WorldPosition - 7.5f; //TODO: Don't hardcode a 15x15 view
            MapCoordinates coords = viewport.ScreenToMap(mousePos);

            var foundSprites = new List<DMISpriteComponent>();
            foreach (DMISpriteComponent sprite in screenOverlay.EnumerateScreenObjects()) {
                Vector2 position = sprite.ScreenLocation.GetViewPosition(viewOffset, EyeManager.PixelsPerMeter);

                if (sprite.CheckClickScreen(position, coords.Position)) {
                    foundSprites.Add(sprite);
                }
            }

            if (foundSprites.Count == 0)
                return null;

            foundSprites.Sort(new RenderOrderComparer());
            return foundSprites[^1].Owner;
        }

        private EntityUid? GetEntityOnMap(MapCoordinates coords) {
            IEnumerable<EntityUid> entities = _lookupSystem.GetEntitiesIntersecting(coords.MapId, Box2.CenteredAround(coords.Position, (0.1f, 0.1f)));

            var foundSprites = new List<DMISpriteComponent>();
            foreach (EntityUid entity in entities) {
                if (_entityManager.TryGetComponent<DMISpriteComponent>(entity, out var sprite)
                    && sprite.CheckClickWorld(coords.Position)) {
                    foundSprites.Add(sprite);
                }
            }

            if (foundSprites.Count == 0)
                return null;

            foundSprites.Sort(new RenderOrderComparer());
            return foundSprites[^1].Owner;
        }
    }
}
