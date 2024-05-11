using Robust.Shared.Utility;

namespace OpenDreamRuntime;

public static partial class ByondApi {
    private static DreamManager? _dreamManager;

    public static void Initialize(DreamManager dreamManager) {
        DebugTools.Assert(_dreamManager is null or { IsShutDown: true });

        _dreamManager = dreamManager;

        InitTrampoline();
    }

    public static DreamValue ValueFromDreamApi(CByondValue value) {
        switch (value.type) {
            case ByondValueType.Number:
                return new DreamValue(value.data.num);

            case ByondValueType.Null:
            default:
                return DreamValue.Null;
        }
    }
}
