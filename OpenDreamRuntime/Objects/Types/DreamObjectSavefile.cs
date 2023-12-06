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

    public override bool ShouldCallNew => false;

    public DreamResource Resource;
    public Dictionary<string, SavefileDirectory> Directories;
    public SavefileDirectory CurrentDir => Directories[_currentDirPath];

    private string _currentDirPath = "/";
    private bool _dirJustChanged = true;

    public DreamObjectSavefile(DreamObjectDefinition objectDefinition) : base(objectDefinition) {

    }

    public override void Initialize(DreamProcArguments args) {
        base.Initialize(args);

        string filename = args.GetArgument(0).GetValueAsString();
        DreamValue timeout = args.GetArgument(1); //TODO: timeout

        Resource = DreamResourceManager.LoadResource(filename);

        string? data = Resource.ReadAsString();
        if (!string.IsNullOrEmpty(data)) {
            Directories = JsonSerializer.Deserialize<Dictionary<string, SavefileDirectory>>(data);
        } else {
            Directories = new() {
                { "/", new SavefileDirectory() }
            };
        }

        Savefiles.Add(this);
    }

    protected override void HandleDeletion() {
        Savefiles.Remove(this);

        base.HandleDeletion();
    }

    public void Flush() {
        Resource.Clear();
        Resource.Output(new DreamValue(JsonSerializer.Serialize(Directories)));
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
        if (value.TryGetValueAsDreamObject(out var ceralizableObject)) {
            // pcall here
            return;
        }
        // no idx means its a << op
        if (_dirJustChanged) {
            _dirJustChanged = false;
            foreach (var key in CurrentDir.Keys.Where(key => key.StartsWith("."))) {
                CurrentDir.Remove(key);
            }
        }
        CurrentDir[index ?? $".{CurrentDir.Count-1}"] = value;
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
