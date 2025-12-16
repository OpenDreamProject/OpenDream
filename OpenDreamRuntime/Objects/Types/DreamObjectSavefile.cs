using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using DMCompiler;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Resources;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectSavefile : DreamObject {
    public override bool ShouldCallNew => false;

    /// <summary>
    /// Cache list for all savefiles, used to keep track for datums using it
    /// </summary>
    public static readonly List<DreamObjectSavefile> Savefiles = new();

    /// <summary>
    /// Real savefile location on the host OS
    /// </summary>
    public DreamResource? Resource;

    /// <summary>
    /// The savefile's current working dir. This could be a generic primitive.
    /// </summary>
    public SfDreamJsonValue CurrentDir;

    /// <summary>
    /// The current path, set this to change the Currentdir value
    /// </summary>
    public string CurrentPath {
        get => _currentPath;
        set {
            var tempDir = SeekTo(value, true);
            if (tempDir == CurrentDir)
                return;

            CurrentDir = tempDir;
            _eof = false;
            if (value.StartsWith('/')) //absolute path
                _currentPath = value;
            else //relative path
                _currentPath = new DreamPath(_currentPath).AddToPath(value).PathString;
        }
    }

    /// <summary>
    /// Savefiles that have been modified since the last flush, processed at the end of the tick
    /// </summary>
    private static readonly HashSet<DreamObjectSavefile> SavefilesToFlush = new();

    private static ISawmill? _sawmill;

    /// <summary>
    /// Temporary savefiles should be deleted when the DreamObjectSavefile is deleted. Temporary savefiles can be created by creating a new savefile datum with a null filename or an entry in the world's resource cache
    /// </summary>
    private bool _isTemporary;

    /// <summary>
    /// basically a global database of savefile contents, which each savefile datum points to - this preserves state between savefiles and reduces memory usage
    /// </summary>
    private static readonly Dictionary<string, SfDreamJsonValue> SavefileDirectories = new();

    /// <summary>
    /// The current savefile data holder - the root of the savefile tree
    /// </summary>
    private SfDreamJsonValue _rootNode;

    /// <summary>
    /// It's not *super* clear what this is supposed to indicate other than "this directory has been read with the >> operator"
    /// It is reset when cd is set. When savefile.eof is set to -1, it deletes the current dir and sets eof to 0
    /// </summary>
    private bool _eof;

    private string _currentPath = "/";

    public DreamObjectSavefile(DreamObjectDefinition objectDefinition) : base(objectDefinition) {
        CurrentDir = _rootNode = new SfDreamDir();
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

        if (!SavefileDirectories.TryGetValue(filename, out var directory)) {
            //if the savefile hasn't already been loaded, load it or create it
            var data = Resource.ReadAsString();

            if (!string.IsNullOrEmpty(data)) {
                try{
                    CurrentDir = _rootNode = JsonSerializer.Deserialize<SfDreamJsonValue>(data)!;
                } catch (JsonException e) { //only catch JSON exceptions, other exceptions probably mean something else happened
                     //fail-safe, make this null if something goes super fucky. Prevents accidentally overwrite of non-savefile files.
                    Resource = null;
                    throw new InvalidDataException($"Error parsing savefile {filename}: Is the savefile corrupted or using a BYOND version? BYOND savefiles are not compatible with OpenDream. Details: {e}");
                }

                SavefileDirectories.Add(filename, _rootNode);
            } else {
                //_rootNode is created in constructor
                SavefileDirectories.Add(filename, _rootNode);

                //create the file immediately
                Flush();
            }
        } else {
            //if the savefile has already been loaded, just point to it
            CurrentDir = _rootNode = directory;
        }

        Savefiles.Add(this);
    }

    protected override void HandleDeletion(bool possiblyThreaded) {
        // SAFETY: Close() is not threadsafe and doesn't have reason to be.
        if (possiblyThreaded) {
            EnterIntoDelQueue();
            return;
        }

        if (Deleted)
            return;

        if (Resource is null)
            return; // Seemingly a long-standing issue, I don't know how to fix this, and it only now appears due to the fact objects always Del() now.
                    // Why we can get here? who knows lol

        Close();
        base.HandleDeletion(possiblyThreaded);
    }

    protected override bool TryGetVar(string varName, out DreamValue value) {
        switch (varName) {
            case "cd":
                value = new DreamValue(CurrentPath);
                return true;
            case "eof":
                value = _eof ? DreamValue.True : DreamValue.False;
                return true;
            case "name":
                value = new DreamValue(Resource?.ResourcePath ?? "[no path]");
                return true;
            case "dir":
                value = new DreamValue(new SavefileDirList(ObjectTree.List.ObjectDefinition, this));
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
            case "eof":
                if (value.TryGetValueAsInteger(out int intValue) && intValue != 0) {
                    if (intValue == -1) {
                        if (CurrentDir != _rootNode) {
                            SfDreamJsonValue parentDir = SeekTo("..");

                            //wipe the value of the current dir but keep any subdirs
                            SfDreamDir newCurrentDir = new SfDreamDir();
                            foreach (var key in CurrentDir.Keys) {
                                newCurrentDir[key] = CurrentDir[key];
                            }

                            CurrentDir.Clear();
                            parentDir[CurrentPath.Split("/").Last()] = newCurrentDir;
                        } else {
                            CurrentDir.Clear();
                        }
                    }

                    _eof = true;
                } else {
                    _eof = false;
                }

                break;
            default:
                throw new Exception($"Cannot set var \"{varName}\" on savefiles");
        }
    }

    public override DreamValue OperatorIndex(DreamValue index, DMProcState state) {
        if (!index.TryGetValueAsString(out var entryName))
            throw new Exception($"Invalid savefile index {index}");

        return GetSavefileValue(entryName);
    }

    public override void OperatorIndexAssign(DreamValue index, DMProcState state, DreamValue value) {
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

    public DreamValue OperatorInput() {
        _eof = true;
        return GetSavefileValue(null);
    }

    /// <summary>
    /// Flushes all savefiles that have been marked as needing flushing. Basically just used to call Flush() between ticks instead of on every write.
    /// </summary>
    public static void FlushAllUpdates() {
        _sawmill ??= Logger.GetSawmill("opendream.res");
        foreach (DreamObjectSavefile savefile in SavefilesToFlush) {
            try {
                // We need to avoid Flush() recreating a nonexistent file
                if (File.Exists(savefile.Resource!.ResourcePath)) savefile.Flush();
            } catch (Exception e) {
                _sawmill.Error($"Error flushing savefile {savefile.Resource!.ResourcePath}: {e}");
            }
        }

        SavefilesToFlush.Clear();
    }

    public void Close() {
        // We need to avoid Flush() recreating a nonexistent file
        if (File.Exists(Resource!.ResourcePath)) Flush();
        if (_isTemporary && Resource?.ResourcePath != null) {
            File.Delete(Resource.ResourcePath);
        }

        //check to see if the file is still in use by another savefile datum
        if (Resource?.ResourcePath != null) {
            var fineToDelete = true;
            foreach (var savefile in Savefiles) {
                if (savefile == this || savefile.Resource!.ResourcePath != Resource.ResourcePath) continue;
                fineToDelete = false;
                break;
            }

            if (fineToDelete)
                SavefileDirectories.Remove(Resource.ResourcePath);
        }

        Savefiles.Remove(this);
        SavefilesToFlush.Remove(this);
    }

    public void Flush() {
        Resource!.Clear();
        Resource!.Output(new DreamValue(JsonSerializer.Serialize(_rootNode)));
    }

    /// <summary>
    /// Attempts to go to said path relative to CurrentPath (you still have to set CurrentDir)
    /// </summary>
    private SfDreamJsonValue SeekTo(string to, bool createPath=false) {
        SfDreamJsonValue tempDir = _rootNode;

        var searchPath = new DreamPath(_currentPath).AddToPath(to).PathString; //relative path
        if (to.StartsWith('/')) //absolute path
            searchPath = to;

        foreach (var path in searchPath.Split("/")) {
            if (path == string.Empty)
                continue;
            if (!tempDir.TryGetValue(path, out var newDir)) {
                if (createPath)
                    newDir = tempDir[path] = new SfDreamDir();
                else
                    return tempDir;
            }

            tempDir = newDir;
        }

        return tempDir;
    }

    public DreamValue GetSavefileValue(string? index) {
        if (index == null) {
            return DeserializeJsonValue(CurrentDir);
        }

        return DeserializeJsonValue(SeekTo(index, true)); //should create the path if it doesn't exist
    }

    public void RemoveSavefileValue(string index) {
        if (CurrentDir.Remove(index)) {
            SavefilesToFlush.Add(this);
        }
    }

    public void RenameAndNullSavefileValue(string index, string newIndex) {
        if (CurrentDir.TryGetValue(index, out var value)) {
            CurrentDir.Remove(index);
            SfDreamDir newDir = new SfDreamDir();
            foreach (var key in value.Keys) {
                newDir[key] = value[key];
            }

            CurrentDir[newIndex] = newDir;
            SavefilesToFlush.Add(this);
        }
    }

    public void AddSavefileDir(string index) {
        SeekTo(index, true);
    }

    public void SetSavefileValue(string? index, DreamValue value) {
        if (index == null) {
            SfDreamJsonValue newCurrentDir = SerializeDreamValue(value);
            foreach (var key in CurrentDir.Keys) {
                if (newCurrentDir.ContainsKey(key)) //if the new dir has a key that overwrites the old one, skip it
                    continue;

                newCurrentDir[key] = CurrentDir[key];
            }

            if (CurrentDir != _rootNode) {
                SfDreamJsonValue parentDir = SeekTo("..");
                parentDir[CurrentPath.Split("/").Last()] = newCurrentDir;
            } else {
                CurrentDir = _rootNode = newCurrentDir;
            }

            SavefilesToFlush.Add(this);
            return;
        }

        var pathArray = index.Split("/");
        if (pathArray.Length == 1) {
            var newValue = SerializeDreamValue(value);
            if (CurrentDir.TryGetValue(index, out var oldValue)) {
                foreach (var key in oldValue.Keys) {
                    if (newValue.ContainsKey(key)) //if the new dir has a key that overwrites the old one, skip it
                        continue;
                    newValue[key] = oldValue[key];
                }
            }

            CurrentDir[index] = newValue;
        } else {
            string oldPath = CurrentPath;
            CurrentPath = new DreamPath(index).AddToPath("../").PathString; //get the parent of the target path
            SetSavefileValue(pathArray[^1], value);
            CurrentPath = oldPath;
        }

        SavefilesToFlush.Add(this);
    }

    /// <summary>
    /// Turn the json magic value into real DM values
    /// </summary>
    public DreamValue DeserializeJsonValue(SfDreamJsonValue value) {
        switch (value) {
            case SfDreamFileValue sfDreamFileValue:
                return new DreamValue(DreamResourceManager.CreateResource(Convert.FromBase64String(sfDreamFileValue.Data)));
            case SfDreamListValue sfDreamListValue:
                var l = ObjectTree.CreateList();
                for (int i=0; i < sfDreamListValue.AssocKeys.Count; i++) {
                    if (sfDreamListValue.AssocData?[i] != null) //note that null != DreamValue.Null
                        l.SetValue(DeserializeJsonValue(sfDreamListValue.AssocKeys[i]), DeserializeJsonValue(sfDreamListValue.AssocData[i]!));
                    else
                        l.AddValue(DeserializeJsonValue(sfDreamListValue.AssocKeys[i]));
                }

                return new DreamValue(l);
            case SfDreamObjectPathValue sfDreamObjectPath:
                SfDreamJsonValue storedObjectVars = sfDreamObjectPath;
                SfDreamJsonValue searchDir = sfDreamObjectPath;
                while(searchDir != _rootNode)
                    if (!searchDir.TryGetValue(sfDreamObjectPath.Path, out storedObjectVars!))
                        searchDir = SeekTo("..");
                    else
                        break;

                if (storedObjectVars.TryGetValue("type", out SfDreamJsonValue? storedObjectTypeJson) && DeserializeJsonValue(storedObjectTypeJson).TryGetValueAsType(out TreeEntry? objectTypeActual)) {
                    DreamObject resultObj = ObjectTree.CreateObject(objectTypeActual);
                    foreach (string key in storedObjectVars.Keys) {
                        if (key == "type" || storedObjectVars[key] is SfDreamDir) //is type or a non-valued dir
                            continue;
                        resultObj.SetVariable(key, DeserializeJsonValue(storedObjectVars[key]));
                    }

                    resultObj.InitSpawn(new DreamProcArguments());
                    resultObj.SpawnProc("Read", null, new DreamValue(this));
                    return new DreamValue(resultObj);
                }

                throw new InvalidDataException("Unable to deserialize object in savefile: " + ((storedObjectTypeJson as SfDreamType) is null ? "no type specified (corrupted savefile?)" : "invalid type "+((SfDreamType)storedObjectTypeJson).TypePath));
            case SfDreamType sfDreamTypeValue:
                if (ObjectTree.TryGetTreeEntry(sfDreamTypeValue.TypePath, out var type)) {
                    return new DreamValue(type);
                }

                break;
            case SfDreamPrimitive sfDreamPrimitive:
                return sfDreamPrimitive.Value;
        }

        return DreamValue.Null;
    }

    /// <summary>
    /// Serialize DM values/objects into savefile data
    /// </summary>
    public SfDreamJsonValue SerializeDreamValue(DreamValue val, int objectCount = 0) {
        switch (val.Type) {
            case DreamValue.DreamValueType.String:
            case DreamValue.DreamValueType.Float:
                return new SfDreamPrimitive { Value = val };
            case DreamValue.DreamValueType.DreamType:
                return new SfDreamType { TypePath = val.MustGetValueAsType().Path };
            case DreamValue.DreamValueType.DreamResource:
                var dreamResource = val.MustGetValueAsDreamResource();
                return new SfDreamFileValue {
                    Name = dreamResource.ResourcePath,
                    Ext = dreamResource.ResourcePath!.Split('.').Last(),
                    Length = dreamResource.ResourceData!.Length,
                    Crc32 = CalculateCrc32(dreamResource.ResourceData),
                    Data = Convert.ToBase64String(dreamResource.ResourceData)
                };
            case DreamValue.DreamValueType.DreamObject:
                if (val.TryGetValueAsDreamList(out var dreamList)) {
                    SfDreamListValue jsonEncodedList = new SfDreamListValue();
                    int thisObjectCount = objectCount;
                    if (dreamList.IsAssociative)
                        jsonEncodedList.AssocData = new List<SfDreamJsonValue?>(dreamList.GetLength()); //only init the list if it's needed

                    foreach (var keyValue in dreamList.EnumerateValues()) { //get all normal values and keys
                        if (keyValue.TryGetValueAsDreamObject(out _) && !keyValue.IsNull) {
                            SfDreamJsonValue jsonEncodedObject = SerializeDreamValue(keyValue, thisObjectCount);
                            //merge the object subdirectories into the list parent directory
                            foreach (var key in jsonEncodedObject.Keys) {
                                jsonEncodedList[key] = jsonEncodedObject[key];
                            }

                            //we already merged the nodes into the parent, so clear them from the child
                            jsonEncodedObject.Clear();
                            //add the object path to the list
                            jsonEncodedList.AssocKeys.Add(jsonEncodedObject);
                            thisObjectCount++;
                        } else {
                            jsonEncodedList.AssocKeys.Add(SerializeDreamValue(keyValue));
                        }

                        if (dreamList.IsAssociative) { //if it's an assoc list, check if this value is a key
                            if (!dreamList.ContainsKey(keyValue)) {
                                jsonEncodedList.AssocData!.Add(null); //store an actual null if this key does not have an associated value - this is distinct from storing DreamValue.Null
                            } else {
                                var assocValue = dreamList.GetValue(keyValue);
                                if (assocValue.TryGetValueAsDreamObject(out _) && !assocValue.IsNull) {
                                    SfDreamJsonValue jsonEncodedObject = SerializeDreamValue(assocValue, thisObjectCount);
                                    //merge the object subdirectories into the list parent directory
                                    foreach (var key in jsonEncodedObject.Keys) {
                                        jsonEncodedList[key] = jsonEncodedObject[key];
                                    }

                                    //we already merged the nodes into the parent, so clear them from the child
                                    jsonEncodedObject.Clear();

                                    //add the object path to the list
                                    jsonEncodedList.AssocData!.Add(jsonEncodedObject);
                                    thisObjectCount++;
                                } else {
                                    jsonEncodedList.AssocData!.Add(SerializeDreamValue(assocValue));
                                }
                            }
                        }
                    }

                    return jsonEncodedList;
                }

                if (val.TryGetValueAsDreamObject(out var dreamObject) && !(dreamObject is null)) { //dreamobject can be null if it's disposed
                    if (val.TryGetValueAsDreamObject<DreamObjectSavefile>(out var savefile)) {
                        //if this is a savefile, just return a filedata object with it encoded
                        savefile.Flush(); //flush the savefile to make sure the backing resource is up to date
                        return new SfDreamFileValue {
                            Name = savefile.Resource!.ResourcePath,
                            Ext = ".sav",
                            Length = savefile.Resource.ResourceData!.Length,
                            Crc32 = CalculateCrc32(savefile.Resource.ResourceData),
                            Data = Convert.ToBase64String(savefile.Resource.ResourceData)
                        };
                    }

                    SfDreamObjectPathValue jsonEncodedObject = new SfDreamObjectPathValue { Path = $".{objectCount}" };
                    SfDreamDir objectVars = new SfDreamDir {
                        //special handling for type, because it's const but we need to save it anyway
                        ["type"] = SerializeDreamValue(dreamObject.GetVariable("type"))
                    };

                    foreach (var key in dreamObject.ObjectDefinition.Variables.Keys) {
                        if((dreamObject.ObjectDefinition.ConstVariables is not null && dreamObject.ObjectDefinition.ConstVariables.Contains(key)) || (dreamObject.ObjectDefinition.TmpVariables is not null && dreamObject.ObjectDefinition.TmpVariables.Contains(key)))
                            continue; //skip const & tmp variables (they're not saved)

                        DreamValue objectVarVal = dreamObject.GetVariable(key);
                        if (dreamObject.ObjectDefinition.Variables[key] == objectVarVal || (objectVarVal.TryGetValueAsDreamObject(out DreamObject? equivTestObject) && equivTestObject != null && equivTestObject.OperatorEquivalent(dreamObject.ObjectDefinition.Variables[key]).IsTruthy()))
                            continue; //skip default values - equivalence check used for lists and objects

                        objectVars[key] = SerializeDreamValue(objectVarVal);
                    }

                    //special handling for /icon since the icon var doesn't actually contain the icon data
                    if (DreamResourceManager.TryLoadIcon(val, out var iconResource)) {
                        objectVars["icon"] = new SfDreamFileValue {
                            Ext=".dmi",
                            Length = iconResource.ResourceData!.Length,
                            Crc32 = CalculateCrc32(iconResource.ResourceData),
                            Data = Convert.ToBase64String(iconResource.ResourceData)
                        };
                    }

                    //Call the Write proc on the object - note that this is a weird one, it does not need to call parent to the native function to save the object
                    dreamObject.SpawnProc("Write", null, new DreamValue(this));
                    jsonEncodedObject[jsonEncodedObject.Path] = objectVars;
                    return jsonEncodedObject;
                }

                break;
            // noop
            case DreamValue.DreamValueType.DreamProc:
            case DreamValue.DreamValueType.Appearance:
                break;
        }

        return new SfDreamPrimitive();
    }

    private uint CalculateCrc32(byte[] data) {
        const uint polynomial = 0xEDB88320;
        uint crc = 0xFFFFFFFF;

        foreach (var value in data) {
            crc ^= value;

            for (int j = 0; j < 8; j++) {
                if ((crc & 1) == 1)
                    crc = (crc >> 1) ^ polynomial;
                else
                    crc >>= 1;
            }
        }

        return ~crc;
    }

    #region JSON Savefile Types

    /// <summary>
    /// Dumb structs for savefile
    /// </summary>
    [JsonPolymorphic]
    [JsonDerivedType(typeof(SfDreamDir),  typeDiscriminator: "dir")]
    [JsonDerivedType(typeof(SfDreamPrimitive), typeDiscriminator: "primitive")]
    [JsonDerivedType(typeof(SfDreamType), typeDiscriminator: "typepath")]
    [JsonDerivedType(typeof(SfDreamListValue), typeDiscriminator: "list")]
    [JsonDerivedType(typeof(SfDreamObjectPathValue), typeDiscriminator: "objectpath")]
    [JsonDerivedType(typeof(SfDreamFileValue), typeDiscriminator: "file")]
    public abstract class SfDreamJsonValue {
        [JsonIgnore] public Dictionary<string, SfDreamJsonValue>.KeyCollection Keys => _nodes.Keys;
        [JsonIgnore] public int Count => _nodes.Count;

        //because dictionary implements its own serialization, we basically just store a dict internally and wrap the functions we need instead of inheriting from it
        [JsonInclude] private Dictionary<string, SfDreamJsonValue> _nodes = new();

        [JsonIgnore]
        public SfDreamJsonValue this[string key] {
            get => _nodes[key];
            set => _nodes[key] = value;
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out SfDreamJsonValue value) => _nodes.TryGetValue(key, out value);
        public void Clear() => _nodes.Clear();
        public bool Remove(string key) => _nodes.Remove(key);
        public bool ContainsKey(string key) => _nodes.ContainsKey(key);
    }

    /// <summary>
    /// Dummy type for directories
    /// </summary>
    public sealed class SfDreamDir : SfDreamJsonValue;

    /// <summary>
    /// Standard DM types except objects and type paths
    /// </summary>
    public sealed class SfDreamPrimitive : SfDreamJsonValue {
        [JsonInclude] public DreamValue Value = DreamValue.Null;
    }

    /// <summary>
    /// Standard DM type paths
    /// </summary>
    public sealed class SfDreamType : SfDreamJsonValue {
        [JsonInclude] public string TypePath = "";
    }

    /// <summary>
    /// List type, with support for associative lists
    /// </summary>
    public sealed class SfDreamListValue : SfDreamJsonValue {
        [JsonInclude] public List<SfDreamJsonValue> AssocKeys = new();
        [JsonInclude] public List<SfDreamJsonValue?>? AssocData;
    }

    /// <summary>
    /// Dummy type for objects (it shows up as `object(relative-path-to-vars-dir)`)
    /// </summary>
    public sealed class SfDreamObjectPathValue : SfDreamJsonValue {
        [JsonInclude] public required string Path;
    }

    /// <summary>
    /// DreamResource holder, encodes said file into base64
    /// </summary>
    public sealed class SfDreamFileValue : SfDreamJsonValue {
        [JsonInclude] public string? Name;
        [JsonInclude] public string? Ext;
        [JsonInclude] public required int Length;
        [JsonInclude] public uint Crc32;
        [JsonInclude] public string Encoding = "base64";
        [JsonInclude] public required string Data;
    }

    #endregion
}
