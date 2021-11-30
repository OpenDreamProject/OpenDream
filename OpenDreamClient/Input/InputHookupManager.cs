using OpenDreamClient.Interface.Controls;
using OpenDreamClient.Rendering;
using Robust.Client;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenDreamClient.Input {
    public class InputHookupManager : EntitySystem {
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IBaseClient _baseClient = default!;
        [Dependency] private readonly IEntityLookup _entityLookup = default!;

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
            IEntity entity = GetEntityOnScreen(mousePos, viewport);
            entity ??= GetEntityOnMap(coords);

            return entity?.Uid ?? EntityUid.Invalid;
        }

        private IEntity GetEntityOnScreen(Vector2 mousePos, ScalingViewport viewport) {
            ClientScreenOverlaySystem screenOverlay = EntitySystem.Get<ClientScreenOverlaySystem>();
            UIBox2 viewportDrawBox = viewport.GetDrawBox();
            Vector2 viewportScale = viewportDrawBox.Size / 480f; //TODO: Don't hardcode 480x480

            mousePos -= viewport.GlobalPixelPosition;
            mousePos /= viewportScale;

            var foundSprites = new List<DMISpriteComponent>();
            foreach (DMISpriteComponent sprite in screenOverlay.EnumerateScreenObjects()) {
                if (!sprite.IsVisible(checkWorld: false)) continue;

                Vector2 screenPos = sprite.ScreenLocation.GetScreenCoordinates(EyeManager.PixelsPerMeter);
                screenPos.Y += sprite.Icon?.DMI?.IconSize.Y ?? 0;
                screenPos.Y = 480 - screenPos.Y;

                if (sprite.CheckClickScreen(mousePos, screenPos)) {
                    foundSprites.Add(sprite);
                }
            }

            if (foundSprites.Count == 0)
                return null;

            foundSprites.Sort(new RenderOrderComparer());
            return foundSprites[^1].Owner;
        }

        private IEntity GetEntityOnMap(MapCoordinates coords) {
            IEnumerable<IEntity> entities = _entityLookup.GetEntitiesIntersecting(coords.MapId, Box2.CenteredAround(coords.Position, (0.1f, 0.1f)));

            var foundSprites = new List<DMISpriteComponent>();
            foreach (IEntity entity in entities) {
                if (entity.TryGetComponent<DMISpriteComponent>(out var sprite)
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
