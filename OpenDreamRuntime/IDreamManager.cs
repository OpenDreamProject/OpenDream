using System.Threading.Tasks;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Procs;
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
        public Dictionary<DreamObject, string> ReferenceIDs { get; set; }
        public List<DreamObject> Mobs { get; set; }
        public List<DreamObject> Clients { get; set; }
        public List<DreamObject> Datums { get; set; }
        public Random Random { get; set; }
        public Dictionary<string, List<DreamObject>> Tags { get; set; }

        public void Initialize(string? testingJson);
        public void Shutdown();
        public bool LoadJson(string? jsonPath);
        public IPlayerSession GetSessionFromClient(DreamObject client);
        DreamConnection GetConnectionFromClient(DreamObject client);
        public DreamObject GetClientFromMob(DreamObject mob);
        DreamConnection GetConnectionFromMob(DreamObject mob);
        DreamConnection GetConnectionBySession(IPlayerSession session);
        public void Update();

        public void WriteWorldLog(string message, LogLevel level, string sawmill = "world.log");

        public virtual string CreateRef(DreamValue value)
        {
            // The first digit is the type, i.e. 1 for objects and 2 for strings

            if(value.TryGetValueAsDreamObject(out var refObject))
            {
                var id = CreateReferenceId();
                return string.Format("{0}{1}", (int)RefType.DreamObject, id);
            }
            if(value.TryGetValueAsString(out var refStr))
            {
                var idx = ObjectTree.Strings.IndexOf(refStr);
                if (idx != -1)
                {
                    return string.Format("{0}{1}", (int)RefType.String, idx);
                }

                ObjectTree.Strings.Add(refStr);
                var id = ObjectTree.Strings.Count - 1;
                return string.Format("{0}{1}", 2, id);
            }

            throw new NotImplementedException();

            string CreateReferenceId()
            {
                if(refObject.Deleted){
                        throw new Exception("Cannot create reference ID for an object that is deleted"); // i dont believe this will **ever** be called, but just to be sure, funky errors /might/ appear in the future if someone does a fucky wucky and calls this on a deleted object.
                }

                if (!ReferenceIDs.TryGetValue(refObject, out string? referenceId)) {
                    referenceId = ReferenceIDs.Count.ToString();
                    ReferenceIDs.Add(refObject, referenceId);
                }

                return referenceId;
            }
        }

        public DreamValue? LocateRef(string refString)
        {
            if (!int.TryParse(refString, out var refId)) return null;

            // The first digit is the type
            var typeId = (RefType)int.Parse(refString.Substring(0, 1));
            refId = int.Parse(refString.Substring(1));

            switch (typeId)
            {
                case RefType.DreamObject:
                {
                    var obj = DreamObject.GetFromReferenceID(this, refString);
                    return new DreamValue(obj);
                }
                case RefType.String:
                {
                    return ObjectTree.Strings.Count > refId ? new DreamValue(ObjectTree.Strings[refId]) : DreamValue.Null;
                }
                default:
                    throw new NotImplementedException($"Unsupported reference type for ref {refString}");
            }
        }

        IEnumerable<DreamConnection> Connections { get; }
    }

    public enum RefType : int
    {
        DreamObject = 1,
        String = 2
    }
}
