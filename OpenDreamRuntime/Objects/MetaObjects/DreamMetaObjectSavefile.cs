using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace OpenDreamRuntime.Objects.MetaObjects {
    class DreamMetaObjectSavefile : DreamMetaObjectRoot {
        public class Savefile {
            public DreamResource Resource;
            public string CurrentDirPath = "/";
            public Dictionary<string, SavefileDirectory> Directories;
            public SavefileDirectory CurrentDir { get => Directories[CurrentDirPath]; }

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

        public DreamMetaObjectSavefile(DreamRuntime runtime)
            : base(runtime) { }

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            string filename = creationArguments.GetArgument(0, "filename").GetValueAsString();
            DreamValue timeout = creationArguments.GetArgument(1, "timeout"); //TODO: timeout

            DreamResource resource = Runtime.ResourceManager.LoadResource(filename);
            Savefile savefile = new Savefile(resource);
            ObjectToSavefile.Add(dreamObject, savefile);

            base.OnObjectCreated(dreamObject, creationArguments);
        }

        public override void OnObjectDeleted(DreamObject dreamObject) {
            ObjectToSavefile.Remove(dreamObject);

            base.OnObjectDeleted(dreamObject);
        }

        public override void OnVariableSet(DreamObject dreamObject, string variableName, DreamValue variableValue, DreamValue oldVariableValue) {
            base.OnVariableSet(dreamObject, variableName, variableValue, oldVariableValue);

            Savefile savefile = ObjectToSavefile[dreamObject];

            switch (variableName) {
                case "cd": savefile.ChangeDirectory(variableValue.GetValueAsString()); break;
                case "eof": break; //TODO: What's a savefile buffer?
            }
        }

        public override DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue) {
            Savefile savefile = ObjectToSavefile[dreamObject];

            switch (variableName) {
                case "cd": return new DreamValue(savefile.CurrentDirPath);
                case "eof": return new DreamValue(0); //TODO: What's a savefile buffer?
                case "name": return new DreamValue(savefile.Resource.ResourcePath);
                case "dir": {
                    DreamList dirList = DreamList.Create(Runtime);

                    foreach (string dirPath in savefile.Directories.Keys) {
                        if (dirPath.StartsWith(savefile.CurrentDirPath)) {
                            dirList.AddValue(new DreamValue(dirPath));
                        }
                    }

                    //TODO: dirList.Add(), dirList.Remove() should affect the directories in a savefile

                    return new DreamValue(dirList);
                }
                default: return base.OnVariableGet(dreamObject, variableName, variableValue);
            }
        }

        public override DreamValue OperatorIndex(DreamObject dreamObject, DreamValue index) {
            Savefile savefile = ObjectToSavefile[dreamObject];

            if (!index.TryGetValueAsString(out string entryName)) throw new Exception("Invalid savefile index " + index);

            if (savefile.CurrentDir.TryGetValue(entryName, out DreamValue entry)) {
                return entry;
            } else {
                return DreamValue.Null;
            }
        }

        public override void OperatorIndexAssign(DreamObject dreamObject, DreamValue index, DreamValue value) {
            Savefile savefile = ObjectToSavefile[dreamObject];

            if (!index.TryGetValueAsString(out string entryName)) throw new Exception("Invalid savefile index " + index);

            savefile.CurrentDir[entryName] = value;
            savefile.Flush(); //TODO: Don't flush after every change
        }
    }
}
