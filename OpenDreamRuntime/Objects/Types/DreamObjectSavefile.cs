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
    private readonly DreamObjectTree _objectTree;

    /// <summary>
    /// Cache list for all savefiles, used to keep track for datums using it
    /// </summary>
    public static readonly List<DreamObjectSavefile> Savefiles = new();

    /// <summary>
    /// Savefiles that have been modified since the last flush, processed at the end of the tick
    /// </summary>
    private static readonly HashSet<DreamObjectSavefile> _savefilesToFlush = new();

    private static ISawmill? _sawmill;


    public override bool ShouldCallNew => false;
    /// <summary>
    /// Temporary savefiles should be deleted when the DreamObjectSavefile is deleted. Temporary savefiles can be created by creating a new savefile datum with a null filename or an entry in the world's resource cache
    /// </summary>
    private bool _isTemporary;

    /// <summary>
    /// basically a global database of savefile contents, which each savefile datum points to - this preserves state between savefiles and reduces memory usage
    /// </summary>
    private static readonly Dictionary<string, SFDreamJsonValue> SavefileDirectories = new();

    /// <summary>
    /// Real savefile location on the host OS
    /// </summary>
    public DreamResource Resource = default!;

    /// <summary>
    /// The current savefile data holder - the root of the savefile tree
    /// </summary>
    private SFDreamJsonValue _rootNode = default!;

    /// <summary>
    /// The current savefile' working dir. This could be a generic primitive
    /// </summary>
    public SFDreamJsonValue CurrentDir;

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


    public DreamObjectSavefile(DreamObjectDefinition objectDefinition) : base(objectDefinition) {
        CurrentDir = _rootNode = new SFDreamDir();
        _objectTree ??= objectDefinition.ObjectTree;
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
                CurrentDir = _rootNode = JsonSerializer.Deserialize<SFDreamJsonValue>(data)!;
                SavefileDirectories.Add(filename, _rootNode);
            } else {
                //_rootNode is created in constructor
                SavefileDirectories.Add(filename, _rootNode);
                //create the file immediately
                Flush();
            }
        } else {
            //if the savefile has already been loaded, just point to it
            CurrentDir = _rootNode = SavefileDirectories[filename];
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
        Resource.Output(new DreamValue(JsonSerializer.Serialize<SFDreamJsonValue>(_rootNode)));
    }

    /// <summary>
    /// Attempts to go to said path relative to CurrentPath (you still have to set CurrentDir)
    /// </summary>
    private SFDreamJsonValue SeekTo(string to) {
        SFDreamJsonValue tempDir = _rootNode;

        var searchPath = new DreamPath(_currentPath).AddToPath(to).PathString; //relative path
        if(to.StartsWith("/")) //absolute path
            searchPath = to;

        foreach (var path in searchPath.Split("/")) {
            if(path == string.Empty)
                continue;
            if (!tempDir.TryGetValue(path, out var newDir)) {
                newDir = tempDir[path] = new SFDreamDir();
            }
            tempDir = newDir;
        }
        return tempDir;
    }

    public DreamValue GetSavefileValue(string? index) {
        if (index == null) {
            return DeserializeJsonValue(CurrentDir);
        }

        return DeserializeJsonValue(SeekTo(index));
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
    public DreamValue DeserializeJsonValue(SFDreamJsonValue value) {
        switch (value) {
            case SFDreamFileValue SFDreamFileValue:
                return new DreamValue(DreamResourceManager.CreateResource(Convert.FromBase64String(SFDreamFileValue.Data)));
            case SFDreamListValue SFDreamListValue:
                var l = ObjectTree.CreateList();
                if(SFDreamListValue.Data != null) {
                    for (var i = 0; i < SFDreamListValue.Data.Count; i++) {
                        l.AddValue(DeserializeJsonValue(SFDreamListValue.Data[i]));
                    }
                }
                if (SFDreamListValue.AssocKeys != null && SFDreamListValue.AssocData != null) {
                    for(int i=0; i < SFDreamListValue.AssocKeys.Count; i++) {
                        l.SetValue(DeserializeJsonValue(SFDreamListValue.AssocKeys[i]), DeserializeJsonValue(SFDreamListValue.AssocData[i]));
                    }
                }
                return new DreamValue(l);
            case SFDreamObjectPathValue SFDreamObjectValue:
                // todo DOV should store WHERE is the actual path for data (normaly its ../'.0')
                if (!SFDreamObjectValue.TryGetValue(".0", out var saveData))
                    break;

                if (saveData.TryGetValue("type", out var unserialType) && unserialType is SFDreamPrimitive primtype) {
                    primtype.Value.MustGetValueAsType();
                    var newObj = GetProc("New").Spawn(this, new DreamProcArguments(primtype.Value));
                    var dObj = newObj.MustGetValueAsDreamObject()!;

                    foreach (var key in dObj.ObjectDefinition.Variables.Keys) {
                        DreamValue val = DreamValue.Null;
                        if (saveData.TryGetValue(key, out var dreamObjVal)) {
                            val = (dreamObjVal is SFDreamObjectPathValue) ? newObj : DeserializeJsonValue(dreamObjVal);
                        }
                        dObj.SetVariable(key, val);
                    }

                    return newObj;
                }
                break;
            case SFDreamType SFDreamTypeValue:
                if(_objectTree.TryGetTreeEntry(SFDreamTypeValue.TypePath, out var type)) {
                    return new DreamValue(type);
                } else {
                    return DreamValue.Null;
                }
            case SFDreamPrimitive SFDreamPrimitive:
                return SFDreamPrimitive.Value;
        }
        return DreamValue.Null;
    }

    /// <summary>
    /// Serialize byond values/objects into savefile data
    /// </summary>
    public SFDreamJsonValue SerializeDreamValue(DreamValue val, int objectCount = 0) {
        switch (val.Type) {
            case DreamValue.DreamValueType.String:
            case DreamValue.DreamValueType.Float:
                return new SFDreamPrimitive { Value = val };
            case DreamValue.DreamValueType.DreamType:
                return new SFDreamType { TypePath = val.MustGetValueAsType().Path };
            case DreamValue.DreamValueType.DreamResource:
                var dreamResource = val.MustGetValueAsDreamResource();
                return new SFDreamFileValue {
                    Length = dreamResource.ResourceData!.Length,
                    //Crc32 =  new System.IO.Hashing.Crc32().
                    Data = Convert.ToBase64String(dreamResource.ResourceData)
                };
            case DreamValue.DreamValueType.DreamObject:
                if (val.TryGetValueAsDreamList(out var dreamList)) {
                    SFDreamListValue jsonEncodedList = new SFDreamListValue();
                    int thisObjectCount = 0;
                    foreach (var value in dreamList.GetValues()) {
                        jsonEncodedList.Data ??= new List<SFDreamJsonValue>(dreamList.GetLength()); //only init the list if it's needed
                        if(value.TryGetValueAsDreamObject(out var _) && !value.IsNull) {
                            SFDreamObjectPathValue jsonEncodedObject = (SFDreamObjectPathValue)SerializeDreamValue(value, thisObjectCount);
                            //merge the object subdirectories into the list parent directory
                            foreach(var key in jsonEncodedObject.Keys) {
                                jsonEncodedList[key] = jsonEncodedObject[key];
                            }
                            //add the object path to the list
                            jsonEncodedList.Data.Add(new SFDreamObjectPathValue(){Path = jsonEncodedObject.Path});
                            thisObjectCount++;
                        } else {
                            jsonEncodedList.Data.Add(SerializeDreamValue(value));
                        }
                    }
                    if(dreamList.IsAssociative) {
                        jsonEncodedList.AssocData = new List<SFDreamJsonValue>();
                        jsonEncodedList.AssocKeys = new List<SFDreamJsonValue>();
                        foreach (var (key, value) in dreamList.GetAssociativeValues()) {
                            if(key.TryGetValueAsDreamObject(out var _) && !key.IsNull) {
                                SFDreamObjectPathValue jsonEncodedObject = (SFDreamObjectPathValue)SerializeDreamValue(key, thisObjectCount);
                                //merge the object subdirectories into the list parent directory
                                foreach(var subkey in jsonEncodedObject.Keys) {
                                    jsonEncodedList[subkey] = jsonEncodedObject[subkey];
                                }
                                //add the object path to the list
                                jsonEncodedList.AssocKeys.Add(new SFDreamObjectPathValue(){Path = jsonEncodedObject.Path});
                                thisObjectCount++;
                            } else {
                                jsonEncodedList.AssocKeys.Add(SerializeDreamValue(key));
                            }

                            if(value.TryGetValueAsDreamObject(out var _) && !value.IsNull) {
                                SFDreamObjectPathValue jsonEncodedObject = (SFDreamObjectPathValue)SerializeDreamValue(value, thisObjectCount);
                                //merge the object subdirectories into the list parent directory
                                foreach(var subkey in jsonEncodedObject.Keys) {
                                    jsonEncodedList[subkey] = jsonEncodedObject[subkey];
                                }
                                //add the object path to the list
                                jsonEncodedList.AssocData.Add(new SFDreamObjectPathValue(){Path = jsonEncodedObject.Path});
                                thisObjectCount++;
                            } else {
                                jsonEncodedList.AssocData.Add(SerializeDreamValue(value));
                            }

                        }
                    }
                    return jsonEncodedList;
                } else if( val.TryGetValueAsDreamObject(out var dreamObject) && !(dreamObject is null)) { //dreamobject can be null if it's disposed
                    SFDreamObjectPathValue jsonEncodedObject = new SFDreamObjectPathValue(){Path = $".{objectCount}"};
                    SFDreamDir objectVars = new SFDreamDir();
                    //special handling for type, because it's const but we need to save it anyway
                    objectVars["type"] = SerializeDreamValue(dreamObject.GetVariable("type"));
                    foreach (var key in dreamObject.ObjectDefinition.Variables.Keys) {
                        if((dreamObject.ObjectDefinition.ConstVariables is not null && dreamObject.ObjectDefinition.ConstVariables.Contains(key)) || (dreamObject.ObjectDefinition.TmpVariables is not null && dreamObject.ObjectDefinition.TmpVariables.Contains(key)))
                            continue; //skip const & tmp variables (they're not saved)
                        DreamValue objectVarVal = dreamObject.GetVariable(key);
                        if(dreamObject.ObjectDefinition.Variables[key] == objectVarVal || (objectVarVal.TryGetValueAsDreamObject(out DreamObject? equivTestObject) && equivTestObject != null && equivTestObject.OperatorEquivalent(dreamObject.ObjectDefinition.Variables[key]).IsTruthy()))
                            continue; //skip default values - equivalence check used for lists and objects
                        objectVars[key] = SerializeDreamValue(objectVarVal);
                    }
                    //Call the Write proc on the object - note that this is a weird one, it does not need to call parent to the native function to save the object
                    //dreamObject.SpawnProc("Write", null, [new DreamValue(this)]);
                    jsonEncodedObject[jsonEncodedObject.Path] = objectVars;
                    return jsonEncodedObject;
                }
                break;
            // noop
            case DreamValue.DreamValueType.DreamProc:
            case DreamValue.DreamValueType.Appearance:
                break;
        }

        return new SFDreamPrimitive();
    }



    #region JSON Savefile Types

    /// <summary>
    /// Dumb structs for savefile
    /// </summary>
    [JsonPolymorphic]
    [JsonDerivedType(typeof(SFDreamDir),  typeDiscriminator: "dir")]
    [JsonDerivedType(typeof(SFDreamPrimitive), typeDiscriminator: "primitive")]
    [JsonDerivedType(typeof(SFDreamType), typeDiscriminator: "typepath")]
    [JsonDerivedType(typeof(SFDreamListValue), typeDiscriminator: "list")]
    [JsonDerivedType(typeof(SFDreamObjectPathValue), typeDiscriminator: "objectpath")]
    [JsonDerivedType(typeof(SFDreamFileValue), typeDiscriminator: "file")]
    public abstract class SFDreamJsonValue {
        //because dictionary implements its own serialization, we basically just store a dict internally and wrap the functions we need instead of inheriting from it
        [JsonInclude]
        private Dictionary<string, SFDreamJsonValue> nodes = new();

        [JsonIgnore]
        public SFDreamJsonValue this[string key] {
            get => nodes[key];
            set => nodes[key] = value;
        }
        public bool TryGetValue(string key, [MaybeNullWhen(false)] out SFDreamJsonValue value) => nodes.TryGetValue(key, out value);
        [JsonIgnore]
        public Dictionary<string, SFDreamJsonValue>.KeyCollection Keys => nodes.Keys;
        [JsonIgnore]
        public int Count => nodes.Count;

    }

    /// <summary>
    /// Dummy type for directories
    /// </summary>
    public sealed class SFDreamDir : SFDreamJsonValue { }
    /// <summary>
    /// Standard byond types except objects and type paths
    /// </summary>
    public sealed class SFDreamPrimitive : SFDreamJsonValue {
        [JsonInclude]
        public DreamValue Value = DreamValue.Null;
    }
    /// <summary>
    /// Standard byond type paths
    /// </summary>
    public sealed class SFDreamType : SFDreamJsonValue {
        [JsonInclude]
        public string TypePath = "";
    }

    public sealed class SFDreamListValue : SFDreamJsonValue {
        [JsonInclude]
        public List<SFDreamJsonValue>? Data;
        [JsonInclude]
        public List<SFDreamJsonValue>? AssocKeys;
        [JsonInclude]
        public List<SFDreamJsonValue>? AssocData;
    }

    /// <summary>
    /// Dummy type for objects that reference itself (it shows up as `object(..)`)
    /// </summary>
    public sealed class SFDreamObjectPathValue : SFDreamJsonValue {
        [JsonInclude]
        public required string Path;
    }

    /// <summary>
    /// DreamResource holder, encodes said file into base64
    /// </summary>
    public sealed class SFDreamFileValue : SFDreamJsonValue {
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

}
