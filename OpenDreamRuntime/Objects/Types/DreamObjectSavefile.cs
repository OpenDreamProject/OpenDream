using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using DMCompiler;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Resources;

namespace OpenDreamRuntime.Objects.Types;


public sealed class DreamObjectSavefile : DreamObject {

    public DreamObjectSavefile(DreamObjectDefinition objectDefinition) : base(objectDefinition) { }

    #region JSON Savefile Types

    /// <summary>
    /// Dumb structs for savefile
    /// </summary>
    [JsonPolymorphic]
    [JsonDerivedType(typeof(DreamDir),  typeDiscriminator: "dir")]
    [JsonDerivedType(typeof(DreamPrimitive), typeDiscriminator: "primitive")]
    [JsonDerivedType(typeof(DreamObjectValue), typeDiscriminator: "object")]
    [JsonDerivedType(typeof(DreamListValue), typeDiscriminator: "list")]
    [JsonDerivedType(typeof(DreamPathValue), typeDiscriminator: "path")]
    [JsonDerivedType(typeof(DreamFileValue), typeDiscriminator: "file")]
    public abstract class DreamJsonValue {
        //because dictionary implements its own serialization, we basically just store a dict internally and wrap the functions we need instead of inheriting from it
        [JsonInclude]
        private Dictionary<string, DreamJsonValue> nodes = new();

        [JsonIgnore]
        public DreamJsonValue this[string key] {
            get => nodes[key];
            set => nodes[key] = value;
        }
        public bool TryGetValue(string key, [MaybeNullWhen(false)] out DreamJsonValue value) => nodes.TryGetValue(key, out value);
        [JsonIgnore]
        public Dictionary<string, DreamJsonValue>.KeyCollection Keys => nodes.Keys;
        [JsonIgnore]
        public int Count => nodes.Count;

    }

    /// <summary>
    /// Dummy type for directories
    /// </summary>
    public sealed class DreamDir : DreamJsonValue { }
    /// <summary>
    /// Standard byond types except objects
    /// </summary>
    public sealed class DreamPrimitive : DreamJsonValue {
        [JsonInclude]
        public DreamValue Value = DreamValue.Null;
    }

    /// <summary>
    /// Unique type for Objects
    /// </summary>
    public sealed class DreamObjectValue : DreamJsonValue { }

    public sealed class DreamListValue : DreamJsonValue {
        [JsonInclude]
        public List<DreamValue> Data;
        [JsonInclude]
        public Dictionary<DreamValue, DreamValue>? AssocData;
    }

    /// <summary>
    /// Dummy type for objects that reference itself (it shows up as `object(..)`)
    /// </summary>
    public sealed class DreamPathValue : DreamJsonValue {
        [JsonInclude]
        public required string Path;
    }

    /// <summary>
    /// DreamResource holder, encodes said file into base64
    /// </summary>
    public sealed class DreamFileValue : DreamJsonValue {
        [JsonInclude]
        public string? Name;
        [JsonInclude]
        public string? Ext;
        [JsonInclude]
        public required int Length;
        [JsonInclude]
        public int Crc32 = 0x00000000;
        [JsonInclude]
        public string Encoding = "base64";
        [JsonInclude]
        public required string Data;
    }

    #endregion

    public override bool ShouldCallNew => false;

    /// <summary>
    /// Cache list for all savefiles, used to keep track for datums using it
    /// </summary>
    public static readonly List<DreamObjectSavefile> Savefiles = new();

    private static readonly HashSet<DreamObjectSavefile> _savefilesToFlush = new();

    private static ISawmill? _sawmill;

    /// Temporary savefiles should be deleted when the DreamObjectSavefile is deleted. Temporary savefiles can be created by creating a new savefile datum with a null filename or an entry in the world's resource cache
    private bool _isTemporary;

    //basically a global database of savefile contents, which each savefile datum points to - this preserves state between savefiles and reduces memory usage
    private static readonly Dictionary<string, DreamJsonValue> SavefileDirectories = new();

    /// <summary>
    /// Real savefile location on the host OS
    /// </summary>
    public DreamResource Resource = default!;

    /// <summary>
    /// The current savefile data holder - the root of the savefile tree
    /// </summary>
    private DreamJsonValue _rootNode = default!;

    /// <summary>
    /// The current savefile' working dir. This could be a generic primitive
    /// </summary>
    public DreamJsonValue CurrentDir;

    private string _currentPath = "/";

    /// <summary>
    /// The current path, set this to change the Currentdir value
    /// </summary>
    public string CurrentPath {
        get => _currentPath;
        set {
            var tempDir = SeekTo(value);
            if (tempDir != CurrentDir) {
                CurrentDir = tempDir;
                if(value.StartsWith("/")) //absolute path
                    _currentPath = value;
                else //relative path
                    _currentPath = new DreamPath(_currentPath).AddToPath(value).PathString;

            }
        }
    }

    public override void Initialize(DreamProcArguments args) {
        base.Initialize(args);

        args.GetArgument(0).TryGetValueAsString(out var filename);
        DreamValue timeout = args.GetArgument(1); //TODO: timeout

        if (string.IsNullOrEmpty(filename)) {
            _isTemporary = true;
            filename = Path.GetTempPath() + "tmp_opendream_savefile_" + DateTime.Now.Ticks;
        }

        Resource = DreamResourceManager.LoadResource(filename);

        if(!SavefileDirectories.ContainsKey(filename)) {
            //if the savefile hasn't already been loaded, load it or create it
            var data = Resource.ReadAsString();

            if (!string.IsNullOrEmpty(data)) {
                CurrentDir = _rootNode = JsonSerializer.Deserialize<DreamJsonValue>(data)!;
                SavefileDirectories.Add(filename, _rootNode);
            } else {
                CurrentDir = _rootNode = new DreamDir();
                SavefileDirectories.Add(filename, _rootNode);
                //create the file immediately
                Flush();
            }
        }

        Savefiles.Add(this);
    }

    protected override void HandleDeletion() {
        Close();
        base.HandleDeletion();
    }

    protected override bool TryGetVar(string varName, out DreamValue value) {
        switch (varName) {
            case "cd":
                value = new DreamValue(CurrentPath);
                return true;
            case "eof":
                value = new DreamValue(0); //TODO: What's a savefile buffer?
                return true;
            case "name":
                value = new DreamValue(Resource.ResourcePath ?? "[no path]");
                return true;
            case "dir":
                DreamList dirList = ObjectTree.CreateList();
                // TODO reimplement
                // foreach (var dirPath in Directories.Keys) {
                //     if (dirPath.StartsWith(_currentDirPath)) {
                //         dirList.AddValue(new DreamValue(dirPath));
                //     }
                // }

                //TODO: dirList.Add(), dirList.Remove() should affect the directories in a savefile
                value = new DreamValue(dirList);
                return true;
            default:
                return base.TryGetVar(varName, out value);
        }
    }

    protected override void SetVar(string varName, DreamValue value) {
        switch (varName) {
            case "cd":
                if (!value.TryGetValueAsString(out var cdTo))
                    throw new Exception($"Cannot change directory to {value}");

                CurrentPath = cdTo;
                break;
            case "eof": // TODO: What's a savefile buffer?
                break;
            default:
                throw new Exception($"Cannot set var \"{varName}\" on savefiles");
        }
    }

    public override DreamValue OperatorIndex(DreamValue index) {
        if (!index.TryGetValueAsString(out var entryName))
            throw new Exception($"Invalid savefile index {index}");

        return GetSavefileValue(entryName);
    }

    public override void OperatorIndexAssign(DreamValue index, DreamValue value) {
        if (!index.TryGetValueAsString(out var entryName))
            throw new Exception($"Invalid savefile index {index}");

        if (entryName == ".") {
            SetSavefileValue(null, value);
            return;
        }

        SetSavefileValue(entryName, value);
    }

    public override void OperatorOutput(DreamValue value) {
        SetSavefileValue(null, value);
    }

    /// <summary>
    /// Flushes all savefiles that have been marked as needing flushing. Basically just used to call Flush() between ticks instead of on every write.
    /// </summary>
    public static void FlushAllUpdates() {
        _sawmill ??= Logger.GetSawmill("opendream.res");
        foreach (DreamObjectSavefile savefile in _savefilesToFlush) {
            try {
                savefile.Flush();
            } catch (Exception e) {
                _sawmill.Error($"Error flushing savefile {savefile.Resource.ResourcePath}: {e}");
            }
        }
        _savefilesToFlush.Clear();
    }

    public void Close() {
        Flush();
        if (_isTemporary && Resource.ResourcePath != null) {
            File.Delete(Resource.ResourcePath);
        }
        //check to see if the file is still in use by another savefile datum
        if(Resource.ResourcePath != null) {
            var fineToDelete = true;
            foreach (var savefile in Savefiles) {
                if (savefile == this || savefile.Resource.ResourcePath != Resource.ResourcePath) continue;
                fineToDelete = false;
                break;
            }

            if (fineToDelete)
                SavefileDirectories.Remove(Resource.ResourcePath);
        }
        Savefiles.Remove(this);
    }

    public void Flush() {
        Resource.Clear();
        Resource.Output(new DreamValue(JsonSerializer.Serialize<DreamJsonValue>(_rootNode)));
    }

    /// <summary>
    /// Attempts to go to said path relative to CurrentPath (you still have to set CurrentDir)
    /// </summary>
    private DreamJsonValue SeekTo(string to) {
        DreamJsonValue tempDir = _rootNode;

        var searchPath = new DreamPath(_currentPath).AddToPath(to).PathString; //relative path
        if(to.StartsWith("/")) //absolute path
            searchPath = to;

        foreach (var path in searchPath.Split("/")) {
            if(path == string.Empty)
                continue;
            if (!tempDir.TryGetValue(path, out var newDir)) {
                newDir = tempDir[path] = new DreamDir();
            }
            tempDir = newDir;
        }
        return tempDir;
    }

    public DreamValue GetSavefileValue(string? index) {
        if (index == null) {
            return RealizeJsonValue(CurrentDir);
        }

        return RealizeJsonValue(index.Split("/").Length == 1 ? CurrentDir[index] : SeekTo(index));
    }

    public void SetSavefileValue(string? index, DreamValue value) {
        // TODO reimplement nulling values when cd
        if (index == null) {
            CurrentDir[$".{CurrentDir.Count}"] = SerializeDreamValue(value);
            _savefilesToFlush.Add(this);
            return;
        }

        var pathArray = index.Split("/");
        if (pathArray.Length == 1) {
            var newValue = SerializeDreamValue(value);
            if(CurrentDir.TryGetValue(index, out var oldValue)) {
                foreach(var key in oldValue.Keys) {
                    newValue[key] = oldValue[key];
                }
            }
            CurrentDir[index] = newValue;
        } else {
            string oldPath = CurrentPath;
            CurrentPath = new DreamPath(index).AddToPath("../").PathString; //get the parent of the target path
            SetSavefileValue(pathArray[pathArray.Length - 1], value);
            CurrentPath = oldPath;
        }
        _savefilesToFlush.Add(this);
    }

    /// <summary>
    /// Turn the json magic value into real byond values
    /// </summary>
    public DreamValue RealizeJsonValue(DreamJsonValue value) {
        switch (value) {
            case DreamFileValue dreamFileValue:
                return new DreamValue(DreamResourceManager.CreateResource(Convert.FromBase64String(dreamFileValue.Data)));
            case DreamListValue dreamListValue:
                var l = ObjectTree.CreateList();
                if (dreamListValue.AssocData != null) {
                    foreach (var kv in dreamListValue.AssocData)
                    {
                        l.SetValue(kv.Key, kv.Value);
                    }
                } else {
                    for (var i = 0; i < dreamListValue.Data.Count; i++) {
                        l.SetValue(new DreamValue(i+1), dreamListValue.Data[i], true);
                    }
                }
                return new DreamValue(l);
            case DreamObjectValue dreamObjectValue:
                // todo DOV should store WHERE is the actual path for data (normaly its ../'.0')
                if (!dreamObjectValue.TryGetValue(".0", out var saveData))
                    break;

                if (saveData.TryGetValue("type", out var unserialType) && unserialType is DreamPrimitive primtype) {
                    primtype.Value.MustGetValueAsType();
                    var newObj = GetProc("New").Spawn(this, new DreamProcArguments(primtype.Value));
                    var dObj = newObj.MustGetValueAsDreamObject()!;

                    foreach (var key in dObj.ObjectDefinition.Variables.Keys) {
                        DreamValue val = DreamValue.Null;
                        if (saveData.TryGetValue(key, out var dreamObjVal)) {
                            val = (dreamObjVal is DreamPathValue) ? newObj : RealizeJsonValue(dreamObjVal);
                        }
                        dObj.SetVariable(key, val);
                    }

                    return newObj;
                }
                break;
            case DreamPrimitive dreamPrimitive:
                return dreamPrimitive.Value;
        }
        return DreamValue.Null;
    }

    /// <summary>
    /// Serialize byond values/objects into savefile data
    /// </summary>
    public DreamJsonValue SerializeDreamValue(DreamValue val) {
        switch (val.Type) {
            case DreamValue.DreamValueType.String:
            case DreamValue.DreamValueType.Float:
            case DreamValue.DreamValueType.DreamType:
                return new DreamPrimitive { Value = val };
            case DreamValue.DreamValueType.DreamResource:
                var dreamResource = val.MustGetValueAsDreamResource();
                return new DreamFileValue {
                    Length = dreamResource.ResourceData!.Length,
                    // Crc32 =  new System.IO.Hashing.Crc32().
                    Data = Convert.ToBase64String(dreamResource.ResourceData)
                };
            case DreamValue.DreamValueType.DreamObject:
                if (val.TryGetValueAsDreamList(out var dreamList)) {
                    return new DreamListValue {
                        Data = dreamList.GetValues(),
                        AssocData = dreamList.IsAssociative ? dreamList.GetAssociativeValues() : null
                    };
                }

                // TODO stub code here, for some reason type isnt saving or something
                // TryGetVar("type", out var tgv);
                // var tmpSavefile = GetProc("New").Spawn(this, new DreamProcArguments(tgv));
                // tmpSavefile.TryGetValueAsDreamObject<DreamObjectSavefile>(out var tmpSavefileReal);
                // Debug.Assert(tmpSavefileReal != null);
                //
                // var theObj = val.MustGetValueAsDreamObject()!;
                // theObj.GetProc("Write").Spawn(theObj, new DreamProcArguments(tmpSavefile));
                // tmpSavefileReal!.SetSavefileValue("type", theObj.GetVariable("type"));
                // tmpSavefileReal.Delete();

                // return new DreamObjectValue {
                //     [".0"] = new DreamPrimitive() //tmpSavefileReal.Savefile
                // };
                break;
            // noop
            case DreamValue.DreamValueType.DreamProc:
            case DreamValue.DreamValueType.Appearance:
                break;
        }

        return new DreamPrimitive();
    }

}
