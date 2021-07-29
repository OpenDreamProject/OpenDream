using Content.Client.Rendering;
using OpenDreamClient.Rendering;
using Robust.Client;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using System.Collections.Generic;
using System.Linq;

namespace Content.Client.Input {
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
            if (args.Viewport is IViewportControl viewport) {
                MapCoordinates mapCoords = viewport.ScreenToMap(keyArgs.PointerLocation.Position);
                IList<IEntity> possibleEntities = GetEntitiesUnderMouse(mapCoords);

                entity = possibleEntities.Count > 0 ? possibleEntities[0].Uid : EntityUid.Invalid;
                coords = _mapManager.TryFindGridAt(mapCoords, out var grid) ? grid.MapToGrid(mapCoords) :
                    EntityCoordinates.FromMap(_mapManager, mapCoords);
            }

            var message = new FullInputCmdMessage(_gameTiming.CurTick, _gameTiming.TickFraction, inputFunction, keyArgs.State, coords, keyArgs.PointerLocation, entity);
            if (inputSystem.HandleInputCommand(_playerManager.LocalPlayer.Session, keyArgs.Function, message)) {
                keyArgs.Handle();
            }
        }

        private IList<IEntity> GetEntitiesUnderMouse(MapCoordinates coords) {
            IEnumerable<IEntity> entities = _entityLookup.GetEntitiesIntersecting(coords.MapId, Box2.CenteredAround(coords.Position, (1, 1)));

            var foundSprites = new List<DMISpriteComponent>();
            foreach (IEntity entity in entities) {
                if (entity.TryGetComponent<DMISpriteComponent>(out var sprite)
                    && entity.Transform.IsMapTransform
                    && sprite.IsMouseOver(coords.Position)) {
                    foundSprites.Add(sprite);
                }
            }

            if (foundSprites.Count == 0)
                return new List<IEntity>();

            //Sort by render order, and put top-most sprite at 0
            foundSprites.Sort(new RenderOrderComparer());
            foundSprites.Reverse();

            return foundSprites.Select(a => a.Owner).ToList();
        }
    }
}
