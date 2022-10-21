using System.Linq;
using OpenDreamRuntime.Objects;
using Robust.Server.Player;

namespace OpenDreamRuntime {
    public interface IDreamManager {
        public DreamObjectTree ObjectTree { get; }
        public DreamObject WorldInstance { get; }

        /// <summary>
        /// A black box (as in, on an airplane) variable currently only used by the test suite to help harvest runtime error info.
        /// </summary>
        public Exception? LastDMException { get; set; }

        public List<DreamValue> Globals { get; set; }
        public DreamList WorldContentsList { get; set; }
        public Dictionary<DreamObject, DreamList> AreaContents { get; set; }
        public Dictionary<DreamObject, int> ReferenceIDs { get; set; }
        public List<DreamObject> Mobs { get; set; }
        public List<DreamObject> Clients { get; set; }
        public List<DreamObject> Datums { get; set; }
        public Random Random { get; set; }
        public Dictionary<string, List<DreamObject>> Tags { get; set; }

        public void Initialize(string? testingJson);
        public void Shutdown();
        public bool LoadJson(string? jsonPath);
        public IPlayerSession GetSessionFromClient(DreamObject client);
        DreamConnection? GetConnectionFromClient(DreamObject client);
        public DreamObject GetClientFromMob(DreamObject mob);
        DreamConnection GetConnectionFromMob(DreamObject mob);
        DreamConnection GetConnectionBySession(IPlayerSession session);
        public void Update();

        public void WriteWorldLog(string message, LogLevel level, string sawmill = "world.log");

        public virtual string CreateRef(DreamValue value) {
            RefType refType;
            int idx;

            if (value.TryGetValueAsDreamObject(out var refObject)) {
                if (refObject == null) {
                    refType = RefType.Null;
                    idx = 0;
                } else {
                    if(refObject.Deleted) {
                        // i dont believe this will **ever** be called, but just to be sure, funky errors /might/ appear in the future if someone does a fucky wucky and calls this on a deleted object.
                        throw new Exception("Cannot create reference ID for an object that is deleted");
                    }

                    refType = RefType.DreamObject;
                    if (!ReferenceIDs.TryGetValue(refObject, out idx)) {
                        idx = ReferenceIDs.Count;
                        ReferenceIDs.Add(refObject, idx);
                    }
                }
            } else if (value.TryGetValueAsString(out var refStr)) {
                refType = RefType.String;
                idx = ObjectTree.Strings.IndexOf(refStr);

                if (idx == -1) {
                    ObjectTree.Strings.Add(refStr);
                    idx = ObjectTree.Strings.Count - 1;
                }
            } else if (value.TryGetValueAsPath(out var refPath)) {
                var treeEntry = ObjectTree.GetTreeEntry(refPath);

                refType = RefType.DreamPath;
                idx = treeEntry.Id;
            } else {
                throw new NotImplementedException($"Ref for {value} is unimplemented");
            }

            // The first digit is the type, i.e. 1 for objects and 2 for strings
            return $"{(int) refType}{idx}";
        }

        public DreamValue LocateRef(string refString) {
            if (!int.TryParse(refString, out var refId)) {
                // If the ref is not an integer, it may be a tag
                if (Tags.TryGetValue(refString, out var tagList)) {
                    return new DreamValue(tagList.First());
                }

                return DreamValue.Null;
            }

            // The first digit is the type
            var typeId = (RefType) int.Parse(refString.Substring(0, 1));
            var untypedRefString = refString.Substring(1); // The ref minus its ref type prefix
            refId = int.Parse(untypedRefString);

            switch (typeId) {
                case RefType.Null:
                    return DreamValue.Null;;
                case RefType.DreamObject:
                    foreach (KeyValuePair<DreamObject, int> referenceIdPair in ReferenceIDs) {
                        if (referenceIdPair.Value == refId) return new DreamValue(referenceIdPair.Key);
                    }

                    return DreamValue.Null;
                case RefType.String:
                    return ObjectTree.Strings.Count > refId
                        ? new DreamValue(ObjectTree.Strings[refId])
                        : DreamValue.Null;
                case RefType.DreamPath:
                    return ObjectTree.Types.Length > refId
                        ? new DreamValue(ObjectTree.Types[refId].Path)
                        : DreamValue.Null;
                default:
                    throw new Exception($"Invalid reference type for ref {refString}");
            }
        }

        IEnumerable<DreamConnection> Connections { get; }
    }

    public enum RefType : int {
        Null = 0,
        DreamObject = 1,
        String = 2,
        DreamPath = 3
    }
}
