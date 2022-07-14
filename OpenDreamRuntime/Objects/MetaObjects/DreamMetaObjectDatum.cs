using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;
using Robust.Shared.IoC;
using System.Linq;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectDatum : IDreamMetaObject {
        public bool ShouldCallNew => true;
        public IDreamMetaObject? ParentType { get; set; }

        private readonly IDreamManager _dreamManager = IoCManager.Resolve<IDreamManager>();

        public void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            if (!dreamObject.IsSubtypeOf(DreamPath.Atom)) // Atoms are in world.contents
            {
                _dreamManager.Datums.Add(dreamObject);
            }

            ParentType?.OnObjectCreated(dreamObject, creationArguments);
        }

        public void OnObjectDeleted(DreamObject dreamObject) {
            ParentType?.OnObjectDeleted(dreamObject);

            if (!dreamObject.IsSubtypeOf(DreamPath.Atom)) // Atoms are in world.contents
            {
                _dreamManager.Datums.Remove(dreamObject);
            }

            dreamObject.SetVariable("tag", DreamValue.Null);

            dreamObject.SpawnProc("Del");
        }

        public DreamValue OnVariableGet(DreamObject dreamObject, string varName, DreamValue value) {
            return varName switch
            {
                "type" => new DreamValue(dreamObject.ObjectDefinition.Type),
                "parent_type" => new DreamValue(_dreamManager.ObjectTree.GetTreeEntry(dreamObject.ObjectDefinition.Type)
                    .ParentEntry.ObjectDefinition.Type),
                "vars" => new DreamValue(DreamListVars.Create(dreamObject)),
                _ => ParentType?.OnVariableGet(dreamObject, varName, value) ?? value
            };
        }

        public void OnVariableSet(DreamObject dreamObject, string varName, DreamValue value, DreamValue oldValue) {
            ParentType?.OnVariableSet(dreamObject, varName, value, oldValue);

            if (varName == "tag")
            {
                oldValue.TryGetValueAsString(out var oldStr);
                value.TryGetValueAsString(out var tagStr);

                // Even if we're setting it to the same string we still need to remove it
                if (!string.IsNullOrEmpty(oldStr))
                {
                    var list = _dreamManager.Tags[oldStr];
                    if (list.Count > 1)
                    {
                        list.Remove(dreamObject);
                    }
                    else
                    {
                        _dreamManager.Tags.Remove(oldStr);
                    }
                }

                // Now we add it (if it's a string)
                if (!string.IsNullOrEmpty(tagStr))
                {
                    if (_dreamManager.Tags.TryGetValue(tagStr, out var list))
                    {
                        list.Add(dreamObject);
                    }
                    else
                    {
                        var newList = new List<DreamObject>(new[] { dreamObject });
                        _dreamManager.Tags.Add(tagStr, newList);
                    }
                }
            }
        }
    }
}
