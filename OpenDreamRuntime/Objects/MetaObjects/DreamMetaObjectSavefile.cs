using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using System.Text.Json;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectSavefile : IDreamMetaObject {
        private readonly DreamResourceManager _resourceManager = IoCManager.Resolve<DreamResourceManager>();

        public IDreamMetaObject? ParentType { get; set; }
        public bool ShouldCallNew => false;
        public sealed class Savefile {
            public readonly DreamResource Resource;
            public readonly Dictionary<string, SavefileDirectory> Directories;
            public string CurrentDirPath = "/";
            public SavefileDirectory CurrentDir => Directories[CurrentDirPath];

            public Savefile(DreamResource resource) {
                Resource = resource;

                string data = resource.ReadAsString();
                if (!String.IsNullOrEmpty(data)) {
                    Directories = JsonSerializer.Deserialize<Dictionary<string, SavefileDirectory>>(data);
                } else {
                    Directories = new() {
                        { "/", new SavefileDirectory() }
                    };
                }
            }

            public void ChangeDirectory(string path) {
                CurrentDirPath = new DreamPath(CurrentDirPath).AddToPath(path).PathString;

                if (!Directories.ContainsKey(CurrentDirPath)) {
                    Directories.Add(CurrentDirPath, new SavefileDirectory());
                }
            }

            public void Flush() {
                Resource.Clear();
                Resource.Output(new DreamValue(JsonSerializer.Serialize(Directories)));
            }
        }

        public class SavefileDirectory : Dictionary<string, DreamValue> { }

        public static Dictionary<DreamObject, Savefile> ObjectToSavefile = new();

        public void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            string filename = creationArguments.GetArgument(0, "filename").GetValueAsString();
            DreamValue timeout = creationArguments.GetArgument(1, "timeout"); //TODO: timeout

            DreamResource resource = _resourceManager.LoadResource(filename);
            Savefile savefile = new Savefile(resource);
            ObjectToSavefile.Add(dreamObject, savefile);

            ParentType?.OnObjectCreated(dreamObject, creationArguments);
        }

        public void OnObjectDeleted(DreamObject dreamObject) {
            ObjectToSavefile.Remove(dreamObject);

            ParentType?.OnObjectDeleted(dreamObject);
        }

        public void OnVariableSet(DreamObject dreamObject, string variableName, DreamValue variableValue, DreamValue oldVariableValue) {
            ParentType?.OnVariableSet(dreamObject, variableName, variableValue, oldVariableValue);

            Savefile savefile = ObjectToSavefile[dreamObject];

            switch (variableName) {
                case "cd": savefile.ChangeDirectory(variableValue.GetValueAsString()); break;
                case "eof": break; //TODO: What's a savefile buffer?
            }
        }

        public DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue) {
            Savefile savefile = ObjectToSavefile[dreamObject];

            switch (variableName) {
                case "cd": return new DreamValue(savefile.CurrentDirPath);
                case "eof": return new DreamValue(0); //TODO: What's a savefile buffer?
                case "name": return new DreamValue(savefile.Resource.ResourcePath);
                case "dir": {
                    DreamList dirList = DreamList.Create();

                    foreach (string dirPath in savefile.Directories.Keys) {
                        if (dirPath.StartsWith(savefile.CurrentDirPath)) {
                            dirList.AddValue(new DreamValue(dirPath));
                        }
                    }

                    //TODO: dirList.Add(), dirList.Remove() should affect the directories in a savefile

                    return new DreamValue(dirList);
                }
                default: return ParentType?.OnVariableGet(dreamObject, variableName, variableValue) ?? variableValue;
            }
        }

        public ProcStatus? OperatorIndex(DreamValue a, DreamValue index, DMProcState state) {
            if(!a.TryGetValueAsDreamObject(out DreamObject dreamObject))
                throw new Exception("SaveFile is not a DreamObject???");
            Savefile savefile = ObjectToSavefile[dreamObject];

            if (!index.TryGetValueAsString(out string? entryName)) throw new Exception($"Invalid savefile index {index}");

            if (savefile.CurrentDir.TryGetValue(entryName, out DreamValue entry)) {
                state.Push(entry);
                return null;
            } else {
                state.Push(DreamValue.Null);
                return null;
            }
        }

        public ProcStatus? OperatorIndexAssign(DreamValue a, DreamValue index, DreamValue value, DMProcState state) {
            if(!a.TryGetValueAsDreamObject(out DreamObject dreamObject))
                throw new Exception("SaveFile is not a DreamObject???");
            Savefile savefile = ObjectToSavefile[dreamObject];

            if (!index.TryGetValueAsString(out string? entryName)) throw new Exception($"Invalid savefile index {index}");

            savefile.CurrentDir[entryName] = value;

            savefile.Flush(); //TODO: Don't flush after every change
            state.Push(DreamValue.Null);
            return null;
        }
    }
}
