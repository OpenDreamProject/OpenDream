namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectTurf : IDreamMetaObject {
        public bool ShouldCallNew => true;
        public IDreamMetaObject? ParentType { get; set; }

        [Dependency] private readonly IDreamMapManager _dreamMapManager = default!;
        [Dependency] private readonly IAtomManager _atomManager = default!;

        public static readonly Dictionary<DreamObject, TurfContentsList> TurfContentsLists = new();

        public DreamMetaObjectTurf() {
            IoCManager.InjectDependencies(this);
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
                    return new(_atomManager.GetAtomPosition(dreamObject).X);
                case "y":
                    return new(_atomManager.GetAtomPosition(dreamObject).Y);
                case "z":
                    return new(_atomManager.GetAtomPosition(dreamObject).Z);
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
