using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;
using Robust.Shared.IoC;
using System.Linq;

namespace OpenDreamRuntime.Objects.MetaObjects {
    [Virtual]
    class DreamMetaObjectDatum : DreamMetaObjectRoot {
        public override bool ShouldCallNew => true;

        private IDreamManager _dreamManager = IoCManager.Resolve<IDreamManager>();

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            if (!dreamObject.IsSubtypeOf(DreamPath.Atom)) // Atoms are in world.contents
            {
                _dreamManager.Datums.Add(dreamObject);
            }

            base.OnObjectCreated(dreamObject, creationArguments);
        }

        public override void OnObjectDeleted(DreamObject dreamObject) {
            base.OnObjectDeleted(dreamObject);

            if (!dreamObject.IsSubtypeOf(DreamPath.Atom)) // Atoms are in world.contents
            {
                _dreamManager.Datums.Remove(dreamObject);
            }

            dreamObject.SpawnProc("Del");
        }

        public override DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue)
        {
            return variableName switch
            {
                "type" => new DreamValue(dreamObject.ObjectDefinition.Type),
                "parent_type" => new DreamValue(_dreamManager.ObjectTree.GetTreeEntry(dreamObject.ObjectDefinition.Type)
                    .ParentEntry.ObjectDefinition.Type),
                "vars" => new DreamValue(DreamListVars.Create(dreamObject)),
                _ => base.OnVariableGet(dreamObject, variableName, variableValue)
            };
        }

        public override void OnVariableSet(DreamObject dreamObject, string variableName, DreamValue variableValue,
            DreamValue oldVariableValue)
        {
            base.OnVariableSet(dreamObject, variableName, variableValue, oldVariableValue);

            if (variableName == "tag")
            {
                oldVariableValue.TryGetValueAsString(out var oldStr);
                variableValue.TryGetValueAsString(out var tagStr);

                // Even if we're setting it to the same string we still need to dequeue it
                if (!string.IsNullOrEmpty(oldStr))
                {
                    var queue = _dreamManager.Tags[oldStr];
                    if (queue.Count > 1)
                    {
                        if (queue.Peek() == dreamObject)
                        {
                            queue.Dequeue();
                        }
                        else
                        {
                            // We're working off of the assumption that the majority of tags are unique most of the time
                            // But when it isn't we have to do this grossness to remove the correct obj from the queue
                            var realList = queue.ToList();
                            realList.Remove(dreamObject);
                            var replacementQueue = new Queue<DreamObject>(realList);
                            _dreamManager.Tags.Add(tagStr, replacementQueue);
                        }
                    }
                    else
                    {
                        _dreamManager.Tags.Remove(oldStr);
                    }
                }

                // Now we queue it (if it's a string)
                if (!string.IsNullOrEmpty(tagStr))
                {
                    if (_dreamManager.Tags.ContainsKey(tagStr))
                    {
                        var currentQueue = _dreamManager.Tags[tagStr];
                        currentQueue.Enqueue(dreamObject);
                    }
                    else
                    {
                        var newQueue = new Queue<DreamObject>(new[] { dreamObject });
                        _dreamManager.Tags.Add(tagStr, newQueue);
                    }
                }
            }
        }
    }
}
