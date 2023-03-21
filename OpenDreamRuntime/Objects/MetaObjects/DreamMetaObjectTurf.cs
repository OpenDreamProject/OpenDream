using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectTurf : IDreamMetaObject {
        public bool ShouldCallNew => true;
        public IDreamMetaObject? ParentType { get; set; }

        [Dependency] private readonly IAtomManager _atomManager = default!;
        [Dependency] private readonly IDreamMapManager _dreamMapManager = default!;

        public static readonly Dictionary<DreamObject, TurfContentsList> TurfContentsLists = new();

        public DreamMetaObjectTurf() {
            IoCManager.InjectDependencies(this);
        }

        public void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            ParentType?.OnObjectCreated(dreamObject, creationArguments);

            IconAppearance turfAppearance = _atomManager.CreateAppearanceFromAtom(dreamObject);
            _dreamMapManager.SetTurfAppearance(dreamObject, turfAppearance);
        }

        public void OnVariableSet(DreamObject dreamObject, string varName, DreamValue value, DreamValue oldValue) {
            ParentType?.OnVariableSet(dreamObject, varName, value, oldValue);

            switch (varName) {
                case "contents": {
                    TurfContentsList contentsList = TurfContentsLists[dreamObject];

                    contentsList.Cut();

                    if (value.TryGetValueAsDreamList(out var valueList)) {
                        foreach (DreamValue contentValue in valueList.GetValues()) {
                            contentsList.AddValue(contentValue);
                        }
                    }

                    break;
                }
            }
        }

        public DreamValue OnVariableGet(DreamObject dreamObject, string varName, DreamValue value) {
            switch (varName) {
                case "x":
                case "y":
                case "z": {
                    (Vector2i pos, IDreamMapManager.Level level) = _dreamMapManager.GetTurfPosition(dreamObject);

                    int coord = varName == "x" ? pos.X :
                                varName == "y" ? pos.Y :
                                level.Z;
                    return new(coord);
                }
                case "loc":
                    return new(_dreamMapManager.GetAreaAt(dreamObject));
                case "contents":
                    return new(TurfContentsLists[dreamObject]);
                default:
                    return ParentType?.OnVariableGet(dreamObject, varName, value) ?? value;
            }
        }
    }
}
