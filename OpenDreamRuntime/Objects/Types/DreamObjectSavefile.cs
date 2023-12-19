using System.IO;
using System.Linq;
using System.Text.Json;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectSavefile : DreamObject {

    public sealed class SavefileDirectory : Dictionary<string, DreamValue> {
    }

    public static readonly List<DreamObjectSavefile> Savefiles = new();
    //basically a global database of savefile contents, which each savefile datum points to - this preserves state between savefiles and reduces memory usage
    private static readonly Dictionary<string, Dictionary<string, SavefileDirectory>> SavefileDirectories = new();
    private static readonly HashSet<DreamObjectSavefile> _savefilesToFlush = new();

    public override bool ShouldCallNew => false;

    public DreamResource Resource;
    public Dictionary<string, SavefileDirectory> Directories => SavefileDirectories[Resource.ResourcePath ?? ""];
    public SavefileDirectory CurrentDir => Directories[_currentDirPath];

    private string _currentDirPath = "/";
    private bool _dirJustChanged = true;

    //Temporary savefiles should be deleted when the DreamObjectSavefile is deleted. Temporary savefiles can be created by creating a new savefile datum with a null filename or an entry in the world's resource cache
    private bool _isTemporary = false;

    private static ISawmill? _sawmill = null;
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

    public DreamObjectSavefile(DreamObjectDefinition objectDefinition) : base(objectDefinition) {

    }

    public override void Initialize(DreamProcArguments args) {
        base.Initialize(args);

        args.GetArgument(0).TryGetValueAsString(out string? filename);
        DreamValue timeout = args.GetArgument(1); //TODO: timeout

        if (string.IsNullOrEmpty(filename)) {
            _isTemporary = true;
            filename = Path.GetTempPath() + "tmp_opendream_savefile_" + System.DateTime.Now.Ticks.ToString();
        }

        Resource = DreamResourceManager.LoadResource(filename);

        if(!SavefileDirectories.ContainsKey(filename)) {
            //if the savefile hasn't already been loaded, load it or create it
            string? data = Resource.ReadAsString();

            if (!string.IsNullOrEmpty(data)) {
                SavefileDirectories.Add(filename, JsonSerializer.Deserialize<Dictionary<string, SavefileDirectory>>(data));
            } else {
                 SavefileDirectories.Add(filename, new() {
                    { "/", new SavefileDirectory() }
                });
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

    public void Flush() {
        Resource.Clear();
        Resource.Output(new DreamValue(JsonSerializer.Serialize(Directories)));
    }

    public void Close() {
        Flush();
        if (_isTemporary && Resource.ResourcePath != null) {
            File.Delete(Resource.ResourcePath);
        }
        //check to see if the file is still in use by another savefile datum
        if(Resource.ResourcePath != null) {
            bool fineToDelete = true;
            foreach (var savefile in Savefiles)
                if (savefile != this && savefile.Resource.ResourcePath == Resource.ResourcePath) {
                    fineToDelete = false;
                    break;
                }
            if (fineToDelete)
                SavefileDirectories.Remove(Resource.ResourcePath);
        }
        Savefiles.Remove(this);
    }

    protected override bool TryGetVar(string varName, out DreamValue value) {
        switch (varName) {
            case "cd":
                value = new DreamValue(_currentDirPath);
                return true;
            case "eof":
                value = new DreamValue(0); //TODO: What's a savefile buffer?
                return true;
            case "name":
                value = new DreamValue(Resource.ResourcePath ?? "[no path]");
                return true;
            case "dir":
                DreamList dirList = ObjectTree.CreateList();

                foreach (string dirPath in Directories.Keys) {
                    if (dirPath.StartsWith(_currentDirPath)) {
                        dirList.AddValue(new DreamValue(dirPath));
                    }
                }

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

                ChangeDirectory(cdTo);
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

        return CurrentDir.TryGetValue(entryName, out DreamValue entry) ? entry : DreamValue.Null;
    }

    /// <summary>
    /// Add or assign value on the targeted dir (or on the current dir)
    /// </summary>
    /// <param name="index">Index or empty for current dir</param>
    /// <param name="value"></param>
    public void AddOrAssignValue(string? index, DreamValue value) {
        if (_dirJustChanged) {
            // when the dir changes, keys gets nuked when we write something
            _dirJustChanged = false;
            foreach (var key in CurrentDir.Keys.Where(key => key.StartsWith("."))) {
                CurrentDir.Remove(key);
            }
        }

        // byond objects/datums
        if (value.TryGetValueAsDreamObject(out var dreamObject) && dreamObject != null) {
            var workingDir = index ?? $".{CurrentDir.Count-1}";
            if (index != null && index != ".") {
                workingDir += "/.0";
            }

            // ExportText should output IF no idx
            // . = object(.0)
            // .0
            //     type=/datum/mytype
            //     (other values written by Write)
            ChangeDirectory(workingDir);
            AddOrAssignValue("type", dreamObject.GetVariable("type"));
            dreamObject.GetProc("Write").Spawn(dreamObject, new DreamProcArguments(new DreamValue(this)));
            ChangeDirectory("../");
            return;
        }

        CurrentDir[index ?? $".{CurrentDir.Count-1}"] = value;
        _savefilesToFlush.Add(this); //mark this as needing flushing
    }

    public override void OperatorIndexAssign(DreamValue index, DreamValue value) {
        if (!index.TryGetValueAsString(out var entryName))
            throw new Exception($"Invalid savefile index {index}");

        AddOrAssignValue(entryName, value);
    }

    // << statement
    public override void OperatorOutput(DreamValue value) {
        AddOrAssignValue(null, value);
    }

    private void ChangeDirectory(string path) {
        _currentDirPath = new DreamPath(_currentDirPath).AddToPath(path).PathString;

        Directories.TryAdd(_currentDirPath, new SavefileDirectory());
        _dirJustChanged = true;
    }
}
