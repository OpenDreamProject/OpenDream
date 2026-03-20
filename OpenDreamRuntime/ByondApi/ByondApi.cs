using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using OpenDreamRuntime.Map;
using OpenDreamRuntime.Objects;
using Robust.Shared.Utility;

namespace OpenDreamRuntime.ByondApi;

public static partial class ByondApi {
    private const int LastErrorMaxLength = 128;

    private static DreamManager? _dreamManager;
    private static DreamRefManager? _refManager;
    private static AtomManager? _atomManager;
    private static IDreamMapManager? _dreamMapManager;
    private static DreamObjectTree? _objectTree;

    private static readonly ConcurrentQueue<Action> ThreadSyncQueue = new();
    private static int _mainThreadId;

    /// <summary>
    /// A failed ByondApi call will set this string. It can be retrieved with <see cref="Byond_LastError"/>.
    /// </summary>
    private static string _lastError = string.Empty;

    private static IntPtr _lastErrorPtr = IntPtr.Zero;

    public static void Initialize(DreamManager dreamManager, DreamRefManager dreamRefManager, AtomManager atomManager, IDreamMapManager dreamMapManager, DreamObjectTree objectTree) {
        DebugTools.Assert(_dreamManager is null or { IsShutDown: true });

        _dreamManager = dreamManager;
        _refManager = dreamRefManager;
        _atomManager = atomManager;
        _dreamMapManager = dreamMapManager;
        _objectTree = objectTree;

        _mainThreadId = Environment.CurrentManagedThreadId;
        ThreadSyncQueue.Clear();

        _lastErrorPtr = Marshal.AllocHGlobal(LastErrorMaxLength);

        InitTrampoline();
    }

    public static void Shutdown() {
        Marshal.FreeHGlobal(_lastErrorPtr);
    }

    public static void ExecuteThreadSyncs() {
        while (ThreadSyncQueue.TryDequeue(out var task))
            task.Invoke();
    }

    /// <summary>
    /// Converts a CByondValue to a DreamValue
    /// </summary>
    /// <remarks>Must be run on the main thread</remarks>
    public static DreamValue ValueFromDreamApi(CByondValue value) {
        DebugTools.AssertEqual(Environment.CurrentManagedThreadId, _mainThreadId);

        var cdata = value.data;
        var ctype = value.type;

        switch (ctype) {
            default:
                throw new Exception($"Invalid reference type for type {ctype.ToString()}");

            case ByondValueType.Number:
                return new DreamValue(value.data.num);

            case ByondValueType.Null:
                return DreamValue.Null;

            case ByondValueType.Turf:
            case ByondValueType.Obj:
            case ByondValueType.Mob:
            case ByondValueType.Area:
            case ByondValueType.Client:
            case ByondValueType.Image:
            case ByondValueType.List:
            case ByondValueType.Datum:
            case ByondValueType.String:
            case ByondValueType.Resource:
            case ByondValueType.ObjTypePath:
            case ByondValueType.MobTypePath:
            case ByondValueType.TurfTypePath:
            case ByondValueType.DatumTypePath:
            case ByondValueType.AreaTypePath:
            case ByondValueType.Pointer:
            case ByondValueType.Appearance:
            case ByondValueType.World:
            case ByondValueType.Proc:
                return _refManager!.LocateRef(cdata.@ref);
        }
    }

    public static CByondValue ValueToByondApi(DreamValue value) {
        switch (value.Type) {
            case DreamValue.DreamValueType.Float:
                return new CByondValue {
                    data = new ByondValueData {
                        num = value.MustGetValueAsFloat()
                    },
                    type = ByondValueType.Number
                };

            case DreamValue.DreamValueType.DreamObject:
            case DreamValue.DreamValueType.String:
            case DreamValue.DreamValueType.DreamResource:
            case DreamValue.DreamValueType.DreamType:
            case DreamValue.DreamValueType.Appearance:
            case DreamValue.DreamValueType.DreamProc:
                var @ref = _refManager!.GetRef(value);
                var refType = (RefType)(@ref & DreamRefManager.RefTypeMask);
                var data = new ByondValueData { @ref = @ref };
                var type = refType switch {
                    RefType.Null => ByondValueType.Null,
                    RefType.DreamObjectTurf => ByondValueType.Turf,
                    RefType.DreamObjectMovable => ByondValueType.Obj,
                    RefType.DreamObjectMob => ByondValueType.Mob,
                    RefType.DreamObjectArea => ByondValueType.Area,
                    RefType.DreamObjectClient => ByondValueType.Client,
                    RefType.DreamObjectImage => ByondValueType.Image,
                    RefType.DreamObjectList => ByondValueType.List,
                    RefType.DreamObjectDatum => ByondValueType.Datum,
                    RefType.String => ByondValueType.String,
                    RefType.DreamResource => ByondValueType.Resource,
                    RefType.DreamType => ByondValueType.ObjTypePath, // just assume objtypepath for now?
                    RefType.DreamAppearance => ByondValueType.Appearance,
                    RefType.Proc => ByondValueType.Proc,
                    _ => throw new Exception($"Invalid reference type for type {refType}")
                };

                return new CByondValue {
                    data = data,
                    type = type
                };
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    /// Helper method that sets <see cref="_lastError"/> and returns an error code (false)
    /// </summary>
    private static byte SetLastError(string lastError) {
        _lastError = lastError;
        return 0;
    }

    [SuppressMessage("Usage", "RA0004:Risk of deadlock from accessing Task<T>.Result")]
    private static T RunOnMainThread<T>(Func<T> task) {
        if (Environment.CurrentManagedThreadId == _mainThreadId)
            return task.Invoke();

        var tcs = new TaskCompletionSource<T>();

        ThreadSyncQueue.Enqueue(() => {
            tcs.SetResult(task.Invoke());
        });

        return tcs.Task.Result;
    }

    private static void RunOnMainThreadNonBlocking(Action task) {
        if (Environment.CurrentManagedThreadId == _mainThreadId) {
            task();
            return;
        }

        ThreadSyncQueue.Enqueue(task);
    }
}
