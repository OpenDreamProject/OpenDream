using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Rendering;
using OpenDreamShared.Dream;
using Robust.Shared.Map;

namespace OpenDreamRuntime.Objects.MetaObjects {
    [Virtual]
    class DreamMetaObjectMovable : IDreamMetaObject {
        public bool ShouldCallNew => true;
        public IDreamMetaObject? ParentType { get; set; }

        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IDreamMapManager _dreamMapManager = default!;
        [Dependency] private readonly IAtomManager _atomManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public DreamMetaObjectMovable() {
            IoCManager.InjectDependencies(this);
        }

        public void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            ParentType?.OnObjectCreated(dreamObject, creationArguments);

            DreamValue screenLocationValue = dreamObject.GetVariable("screen_loc");
            if (screenLocationValue != DreamValue.Null) UpdateScreenLocation(dreamObject, screenLocationValue);
        }

        public void OnObjectDeleted(DreamObject dreamObject) {
            if (dreamObject.GetVariable("loc").TryGetValueAsDreamObjectOfType(DreamPath.Atom, out DreamObject loc)) {
                DreamList contents = loc.GetVariable("contents").GetValueAsDreamList();

                contents.RemoveValue(new DreamValue(dreamObject));
            }

            ParentType?.OnObjectDeleted(dreamObject);
        }

        public void OnVariableSet(DreamObject dreamObject, string varName, DreamValue value, DreamValue oldValue) {
            ParentType?.OnVariableSet(dreamObject, varName, value, oldValue);

            switch (varName) {
                case "x":
                case "y":
                case "z": {
                    int x = (varName == "x") ? value.GetValueAsInteger() : dreamObject.GetVariable("x").GetValueAsInteger();
                    int y = (varName == "y") ? value.GetValueAsInteger() : dreamObject.GetVariable("y").GetValueAsInteger();
                    int z = (varName == "z") ? value.GetValueAsInteger() : dreamObject.GetVariable("z").GetValueAsInteger();
                    DreamObject newLocation = _dreamMapManager.GetTurf(x, y, z);

                    dreamObject.SetVariable("loc", new DreamValue(newLocation));
                    break;
                }
                case "loc": {
                    EntityUid entity = _atomManager.GetAtomEntity(dreamObject);
                    if (!_entityManager.TryGetComponent<TransformComponent>(entity, out var transform))
                        return;

                    if (value.TryGetValueAsDreamObjectOfType(DreamPath.Atom, out DreamObject loc)) {
                        EntityUid locEntity = _atomManager.GetAtomEntity(loc);

                        transform.AttachParent(locEntity);
                        transform.LocalPosition = Vector2.Zero;
                    } else {
                        transform.AttachParent(_mapManager.GetMapEntityId(MapId.Nullspace));
                    }

                    break;
                }
                case "screen_loc":
                    UpdateScreenLocation(dreamObject, value);
                    break;
            }
        }

        private void UpdateScreenLocation(DreamObject movable, DreamValue screenLocationValue) {
            if (!_entityManager.TryGetComponent<DMISpriteComponent>(_atomManager.GetAtomEntity(movable), out var sprite))
                return;

            ScreenLocation screenLocation;
            if (screenLocationValue.TryGetValueAsString(out string screenLocationString)) {
                screenLocation = new ScreenLocation(screenLocationString);
            } else {
                screenLocation = new ScreenLocation(0, 0, 0, 0);
            }

            sprite.ScreenLocation = screenLocation;
        }
    }
}
