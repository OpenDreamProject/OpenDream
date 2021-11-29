using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Rendering;
using OpenDreamShared.Dream;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace OpenDreamRuntime.Objects.MetaObjects {
    class DreamMetaObjectMovable : DreamMetaObjectAtom {
        private IMapManager _mapManager = IoCManager.Resolve<IMapManager>();
        private IDreamMapManager _dreamMapManager = IoCManager.Resolve<IDreamMapManager>();
        private IAtomManager _atomManager = IoCManager.Resolve<IAtomManager>();

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            base.OnObjectCreated(dreamObject, creationArguments);

            DreamValue screenLocationValue = dreamObject.GetVariable("screen_loc");
            if (screenLocationValue.Value != null)UpdateScreenLocation(dreamObject, screenLocationValue);
        }

        public override void OnObjectDeleted(DreamObject dreamObject) {
            if (dreamObject.GetVariable("loc").TryGetValueAsDreamObjectOfType(DreamPath.Atom, out DreamObject loc)) {
                DreamList contents = loc.GetVariable("contents").GetValueAsDreamList();

                contents.RemoveValue(new DreamValue(dreamObject));
            }

            base.OnObjectDeleted(dreamObject);
        }

        public override void OnVariableSet(DreamObject dreamObject, string variableName, DreamValue variableValue, DreamValue oldVariableValue) {
            base.OnVariableSet(dreamObject, variableName, variableValue, oldVariableValue);

            switch (variableName) {
                case "x":
                case "y":
                case "z": {
                    int x = (variableName == "x") ? variableValue.GetValueAsInteger() : dreamObject.GetVariable("x").GetValueAsInteger();
                    int y = (variableName == "y") ? variableValue.GetValueAsInteger() : dreamObject.GetVariable("y").GetValueAsInteger();
                    int z = (variableName == "z") ? variableValue.GetValueAsInteger() : dreamObject.GetVariable("z").GetValueAsInteger();
                    DreamObject newLocation = _dreamMapManager.GetTurf(x, y, z);

                    dreamObject.SetVariable("loc", new DreamValue(newLocation));
                    break;
                }
                case "loc": {
                    IEntity entity = _atomManager.GetAtomEntity(dreamObject);

                    if (variableValue.TryGetValueAsDreamObjectOfType(DreamPath.Atom, out DreamObject loc)) {
                        IEntity locEntity = _atomManager.GetAtomEntity(loc);

                        entity.Transform.AttachParent(locEntity);
                        entity.Transform.LocalPosition = Vector2.Zero;
                    } else {
                        entity.Transform.AttachParent(_mapManager.GetMapEntity(MapId.Nullspace));
                    }

                    break;
                }
                case "screen_loc":
                    UpdateScreenLocation(dreamObject, variableValue);
                    break;
            }
        }

        private void UpdateScreenLocation(DreamObject movable, DreamValue screenLocationValue) {
            ScreenLocation screenLocation;
            if (screenLocationValue.TryGetValueAsString(out string screenLocationString)) {
                screenLocation = new ScreenLocation(screenLocationString);
            } else {
                screenLocation = new ScreenLocation(0, 0, 0, 0);
            }

            _atomManager.GetAtomEntity(movable).GetComponent<DMISpriteComponent>().ScreenLocation = screenLocation;
        }
    }
}
