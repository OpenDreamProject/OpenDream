using System.Diagnostics;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Rendering;
using OpenDreamRuntime.Resources;

namespace OpenDreamRuntime.Objects;

/// <summary>
/// A centralized place for all your ref() and locate() needs
/// Holds collections of every alive DreamObject
/// </summary>
// TODO: This could probably be expanded to hold buckets for non-DreamObject values as well (strings, appearances, etc)
public sealed class DreamRefManager {
    public const uint RefTypeMask = 0xFF000000;
    public const uint RefIdMask = 0x00FFFFFF;

    public Dictionary<string, List<DreamObject>> Tags { get; } = new();

    [Dependency] private readonly DreamObjectTree _objectTree = default!;
    [Dependency] private readonly DreamResourceManager _resourceManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    private ServerAppearanceSystem? _appearanceSystem;

    private readonly Dictionary<RefType, Bucket> _buckets = new();

    /// <summary>
    /// Holds all DreamObjects of a certain <see cref="RefType"/>
    /// Attempts to performantly allow deletion & reuse of slots
    /// </summary>
    private sealed class Bucket {
        /// The amount of alive values in this bucket
        public int FilledCount { get; private set; }

        private readonly List<WeakReference<DreamObject>?> _values = new();
        private int _earliestEmptySlot;

        public int Add(DreamObject value) {
            FilledCount++;

            var refId = _earliestEmptySlot++;
            if (refId >= _values.Count) {
                _values.Add(new(value));
                return refId;
            }

            _values[refId] = new(value);

            // Find the next null value to update _earliestEmptySlot
            for (; _earliestEmptySlot < _values.Count; _earliestEmptySlot++) {
                if (_values[_earliestEmptySlot]?.TryGetTarget(out _) is true)
                    continue;

                _values[_earliestEmptySlot] = null;
                break;
            }

            return refId;
        }

        public DreamObject? Get(int refId) {
            var weakRef = _values[refId];
            DreamObject? value = null;

            weakRef?.TryGetTarget(out value);
            return value;
        }

        public void Remove(int refId) {
            if (refId >= _values.Count)
                return;
            if (_values[refId] != null)
                FilledCount--;

            _values[refId] = null;
            _earliestEmptySlot = Math.Min(_earliestEmptySlot, refId);
        }

        public IEnumerable<DreamObject> Enumerate() {
            // This cannot ever be a foreach!
            // world.contents allows modification during enumeration
            for (var i = 0; i < _values.Count; i++) {
                var weakRef = _values[i];
                if (weakRef is null)
                    continue;

                if (weakRef.TryGetTarget(out var value)) {
                    yield return value;
                } else {
                    // This isn't a common operation so we'll use this time to also do some pruning.
                    FilledCount--;
                    _values[i] = null;
                }
            }
        }
    }

    public void Initialize() {
        Tags.Clear();
        _buckets.Clear();

        ReadOnlySpan<RefType> bucketTypes = [
            RefType.DreamObjectDatum,
            RefType.DreamObjectTurf,
            RefType.DreamObjectMob,
            RefType.DreamObjectArea,
            RefType.DreamObjectClient,
            RefType.DreamObjectImage,
            RefType.DreamObjectFilter,
            RefType.DreamObjectMovable,
            RefType.DreamObjectList
        ];

        foreach (var type in bucketTypes) {
            _buckets[type] = new();
        }
    }

    /// <summary>
    /// Get the number representation of a DreamValue's ref()<br/>
    /// Use with <see cref="LocateRef(uint)"/> to later get the DreamValue again
    /// </summary>
    /// <remarks>
    /// This is a weak reference, so deleted objects may have their ref reused by something else
    /// </remarks>
    /// <param name="value">The DreamValue to get the ref of</param>
    public uint GetRef(DreamValue value) {
        if (value.TryGetValueAsDreamObject(out var refObject))
            return GetRef(refObject);
        if (value.TryGetValueAsString(out var refStr))
            return GetRef(refStr);
        if (value.TryGetValueAsType(out var type))
            return (uint)RefType.DreamType | (uint)type.Id;
        if (value.TryGetValueAsDreamResource(out var refRsc))
            return (uint)RefType.DreamResource | (uint)refRsc.Id;
        if (value.TryGetValueAsProc(out var proc))
            return (uint)RefType.Proc | (uint)proc.Id;

        if (value.TryGetValueAsAppearance(out var appearance)) {
            _appearanceSystem ??= _entitySystemManager.GetEntitySystem<ServerAppearanceSystem>();
            var appearanceId = _appearanceSystem.AddAppearance(appearance).MustGetId();

            return (uint)RefType.DreamAppearance | appearanceId;
        }

        // Yes, this combines with the refType and produces an invalid ref.
        // This is BYOND behavior (as of writing at least, on 516.1661).
        if (value.TryGetValueAsFloat(out var floatValue))
            return (uint)RefType.Number | BitConverter.SingleToUInt32Bits(floatValue);

        throw new NotImplementedException($"Ref for {value} is unimplemented");
    }

    /// <summary>
    /// Get the number representation of a DreamObject's ref()
    /// </summary>
    /// <remarks>
    /// This is a weak reference, so deleted objects may have their ref reused by something else
    /// </remarks>
    /// <param name="dreamObject">The DreamObject to get the ref of</param>
    public uint GetRef(DreamObject? dreamObject) {
        if (dreamObject == null)
            return (uint)RefType.Null;

        if (dreamObject.Deleted) {
            // i dont believe this will **ever** be called, but just to be sure, funky errors /might/ appear in the future if someone does a fucky wucky and calls this on a deleted object.
            throw new Exception("Cannot create reference ID for an object that is deleted");
        }

        // This DreamObject's RefId hasn't been initialized yet
        // Presumably this method is being called in order to initialize it
        if (dreamObject.RefId == default) {
            return dreamObject switch {
                DreamObjectTurf turf => CreateRef(RefType.DreamObjectTurf, turf),
                DreamObjectMob mob => CreateRef(RefType.DreamObjectMob, mob),
                DreamObjectArea area => CreateRef(RefType.DreamObjectArea, area),
                DreamObjectClient client => CreateRef(RefType.DreamObjectClient, client),
                DreamObjectImage image => CreateRef(RefType.DreamObjectImage, image),
                DreamObjectFilter filter => CreateRef(RefType.DreamObjectFilter, filter),
                DreamObjectMovable => CreateRef(RefType.DreamObjectMovable, dreamObject),
                DreamList list when list.GetType() == typeof(DreamList) => CreateRef(RefType.DreamObjectList, list),
                _ => CreateRef(RefType.DreamObjectDatum, dreamObject)
            };
        }

        return dreamObject.RefId;
    }

    /// <summary>
    /// Get the number representation of a string's ref()
    /// </summary>
    /// <param name="value">The string to get the ref of</param>
    public uint GetRef(string value) {
        return (uint)RefType.String | FindOrAddString(value);
    }

    /// <summary>
    /// Get the string representation of a DreamValue's ref()<br/>
    /// Use with <see cref="LocateRef(string)"/> to later get the DreamValue again
    /// </summary>
    /// <returns>The ref in [0xaabbccdd] format</returns>
    /// <remarks>
    /// This is a weak reference, so deleted objects may have their ref reused by something else
    /// </remarks>
    /// <param name="value">The DreamValue to get the ref of</param>
    public string GetRefString(DreamValue value) {
        var @ref = GetRef(value);

        return $"[0x{@ref:x}]";
    }

    /// <summary>
    /// Gets the DreamValue referred to by a ref
    /// </summary>
    /// <remarks>
    /// May not return the same object that was passed to <see cref="GetRef(DreamValue)"/> if it was deleted and its ID reused
    /// </remarks>
    /// <param name="ref">The number representation of a ref</param>
    public DreamValue LocateRef(uint @ref) {
        var refType = (RefType)(@ref & RefTypeMask);
        var refId = @ref & RefIdMask;

        switch (refType) {
            case RefType.Null:
                Debug.Assert(refId == 0);
                return DreamValue.Null;

            case RefType.DreamObjectArea:
            case RefType.DreamObjectClient:
            case RefType.DreamObjectDatum:
            case RefType.DreamObjectImage:
            case RefType.DreamObjectFilter:
            case RefType.DreamObjectList:
            case RefType.DreamObjectMob:
            case RefType.DreamObjectTurf:
            case RefType.DreamObjectMovable:
                return new(GetFromBucket(@ref));

            case RefType.String:
                return _objectTree.Strings.Count > refId
                    ? new DreamValue(_objectTree.Strings[(int)refId])
                    : DreamValue.Null;
            case RefType.DreamType:
                return _objectTree.Types.Length > refId
                    ? new DreamValue(_objectTree.Types[refId])
                    : DreamValue.Null;
            case RefType.DreamResourceIcon: // Alias of DreamResource for now. TODO: Does this *only* contain icon resources?
            case RefType.DreamResource:
                if (!_resourceManager.TryLoadResource((int)refId, out var resource))
                    return DreamValue.Null;

                return new DreamValue(resource);
            case RefType.DreamAppearance:
                _appearanceSystem ??= _entitySystemManager.GetEntitySystem<ServerAppearanceSystem>();
                return _appearanceSystem.TryGetAppearanceById(refId, out var appearance)
                    ? new DreamValue(appearance.ToMutable())
                    : DreamValue.Null;
            case RefType.Proc:
                return _objectTree.Procs.Count > refId
                    ? new DreamValue(_objectTree.Procs[(int)refId])
                    : DreamValue.Null;
            case RefType.Number: // For the oh so few numbers this works with (most numbers clobber the ref type)
                return new(BitConverter.UInt32BitsToSingle(refId));
            default:
                throw new Exception($"Invalid reference type for ref [0x{refId:x}]");
        }
    }

    /// <summary>
    /// Parses the number or tag in a ref() string and returns the DreamValue it refers to
    /// </summary>
    /// <remarks>
    /// May not return the same object that was passed to <see cref="GetRefString(DreamValue)"/> if it was deleted and its ID reused
    /// </remarks>
    /// <param name="refStr">The string representation of a ref, or a datum's tag</param>
    public DreamValue LocateRef(string refStr) {
        if (refStr.StartsWith('[') && refStr.EndsWith(']')) {
            // Strip the surrounding []
            refStr = refStr.Substring(1, refStr.Length - 2);

            // This ref could possibly be a "pointer" (the hex number made up of an id and an index)
            var canBePointer = refStr.StartsWith("0x");

            if (canBePointer && uint.TryParse(refStr.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out var @ref)) {
                return LocateRef(@ref);
            }
        }

        // Search for an object with this ref as its tag
        // Note that surrounding [] are stripped out at this point, this is intentional
        // Doing locate("[abc]") is the same as locate("abc")
        if (Tags.TryGetValue(refStr, out var tagList) && tagList.Count > 0) {
            return new DreamValue(tagList[0]);
        }

        // Nothing found
        return DreamValue.Null;
    }

    /// <summary>
    /// Frees up an object's ID for reuse
    /// </summary>
    /// <remarks>
    /// This does not delete an object, use <see cref="DreamObject.Delete"/> for that
    /// </remarks>
    /// <param name="ref">The ref for the object to free</param>
    public void DeleteRef(uint @ref) {
        var refType = (RefType)(@ref & RefTypeMask);
        var bucket = _buckets[refType];

        bucket.Remove((int)(@ref & RefIdMask));
    }

    /// <summary>
    /// Enumerate every alive DreamObject of a certain <see cref="RefType"/>
    /// </summary>
    public IEnumerable<DreamObject> EnumerateType(RefType refType) {
        var bucket = _buckets[refType];

        return bucket.Enumerate();
    }

    /// <summary>
    /// Find a string's ID if it has one
    /// </summary>
    /// <remarks>
    /// This doesn't include <see cref="RefType.String"/> in the return value,
    /// so it is not usable with <see cref="LocateRef(uint)"/> as-is
    /// </remarks>
    /// <returns>A string's ID, or null if it hasn't been added to the DM string list</returns>
    public uint? FindStringId(string str) {
        int idx = _objectTree.Strings.IndexOf(str);

        if (idx < 0) {
            return null;
        }

        return (uint)idx;
    }

    /// <summary>
    /// Get the amount of alive DreamObjects of type <see cref="RefType"/>
    /// </summary>
    public int GetCountOf(RefType refType) {
        var bucket = _buckets[refType];

        return bucket.FilledCount;
    }

    /// <summary>
    /// Grabs a DreamObject from its bucket using its ref
    /// </summary>
    private DreamObject? GetFromBucket(uint @ref) {
        var refType = (RefType)(@ref & RefTypeMask);
        var refId = (int)(@ref & RefIdMask);
        var bucket = _buckets[refType];

        return bucket.Get(refId);
    }

    /// <summary>
    /// Get a string's ID, adding it to the DM string list if it's not already in there
    /// </summary>
    private uint FindOrAddString(string str) {
        var idx = FindStringId(str);
        if (idx == null) {
            _objectTree.Strings.Add(str);
            idx = (uint)(_objectTree.Strings.Count - 1);
        }

        return idx.Value;
    }

    /// <summary>
    /// Add a DreamObject to its relevant bucket, creating a ref for it
    /// </summary>
    /// <param name="refType">The DreamObject's RefType, deciding which bucket it goes in</param>
    /// <param name="value">The DreamObject to create a ref for</param>
    /// <returns>The DreamObject's new ref</returns>
    private uint CreateRef(RefType refType, DreamObject value) {
        var bucket = _buckets[refType];

        return (uint)refType | (uint)bucket.Add(value);
    }
}

public enum RefType : uint {
    Null = 0x0,
    DreamObjectTurf = 0x1000000,
    DreamObjectMovable = 0x2000000,
    DreamObjectMob = 0x3000000,
    DreamObjectArea = 0x4000000,
    DreamObjectClient = 0x5000000,
    DreamResourceIcon = 0xC000000,
    DreamObjectImage = 0xD000000,
    DreamObjectList = 0xF000000,
    DreamObjectDatum = 0x21000000,
    String = 0x6000000,
    DreamType = 0x9000000, //in byond type is from 0x8 to 0xb, but fuck that
    DreamResource = 0x27000000, //Equivalent to file
    DreamAppearance = 0x3A000000,
    Proc = 0x26000000,
    Number = 0x2A000000,
    DreamObjectFilter = 0x53000000
}
