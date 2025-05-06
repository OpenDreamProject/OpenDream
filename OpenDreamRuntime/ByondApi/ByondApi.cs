﻿using OpenDreamRuntime.Map;
using OpenDreamRuntime.Objects;
using Robust.Shared.Utility;

namespace OpenDreamRuntime.ByondApi;

public static partial class ByondApi {
    private static DreamManager? _dreamManager;
    private static AtomManager? _atomManager;
    private static IDreamMapManager? _dreamMapManager;
    private static DreamObjectTree? _objectTree;

    public static void Initialize(DreamManager dreamManager, AtomManager atomManager, IDreamMapManager dreamMapManager, DreamObjectTree objectTree) {
        DebugTools.Assert(_dreamManager is null or { IsShutDown: true });

        _dreamManager = dreamManager;
        _atomManager = atomManager;
        _dreamMapManager = dreamMapManager;
        _objectTree = objectTree;

        InitTrampoline();
    }

    public static DreamValue ValueFromDreamApi(CByondValue value) {
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
                int refId = (int)cdata.@ref;
                return _dreamManager!.RefIdToValue(refId);
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
                var refid = _dreamManager!.CreateRefInt(value, out RefType refType);
                ByondValueType type;
                ByondValueData data = new ByondValueData { @ref = refid };

                switch (refType) {
                    default:
                        throw new Exception($"Invalid reference type for type {refType}");
                    case RefType.Null:
                        type = ByondValueType.Null;
                        break;
                    case RefType.DreamObjectTurf:
                        type = ByondValueType.Turf;
                        break;
                    case RefType.DreamObject:
                        type = ByondValueType.Obj;
                        break;
                    case RefType.DreamObjectMob:
                        type = ByondValueType.Mob;
                        break;
                    case RefType.DreamObjectArea:
                        type = ByondValueType.Area;
                        break;
                    case RefType.DreamObjectClient:
                        type = ByondValueType.Client;
                        break;
                    case RefType.DreamObjectImage:
                        type = ByondValueType.Image;
                        break;
                    case RefType.DreamObjectList:
                        type = ByondValueType.List;
                        break;
                    case RefType.DreamObjectDatum:
                        type = ByondValueType.Datum;
                        break;
                    case RefType.String:
                        type = ByondValueType.String;
                        break;
                    case RefType.DreamResource:
                        type = ByondValueType.Resource;
                        break;
                    case RefType.DreamType:
                        // just assume objtypepath for now?
                        type = ByondValueType.ObjTypePath;
                        break;
                    case RefType.DreamAppearance:
                        type = ByondValueType.Appearance;
                        break;
                    case RefType.Proc:
                        type = ByondValueType.Proc;
                        break;
                }

                return new CByondValue {
                    data = data,
                    type = type
                };
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
