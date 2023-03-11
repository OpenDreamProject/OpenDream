using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.MetaObjects;
using OpenDreamRuntime.Procs.Native;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;

namespace OpenDreamRuntime.Procs {
    static class DMOpcodeHandlers {
        #region Values
        public static ProcStatus? PushReferenceValue(DMProcState state) {
            DMReference reference = state.ReadReference();

            state.Push(state.GetReferenceValue(reference));
            return null;
        }

        public static ProcStatus? Assign(DMProcState state) {
            DMReference reference = state.ReadReference();
            DreamValue value = state.Pop();

            state.AssignReference(reference, value);
            state.Push(value);
            return null;
        }

        public static ProcStatus? CreateList(DMProcState state) {
            int size = state.ReadInt();
            var list = state.Proc.ObjectTree.CreateList(size);

            foreach (DreamValue value in state.PopCount(size)) {
                list.AddValue(value);
            }

            state.Push(new DreamValue(list));
            return null;
        }

        public static ProcStatus? CreateAssociativeList(DMProcState state) {
            int size = state.ReadInt();
            var list = state.Proc.ObjectTree.CreateList(size);

            ReadOnlySpan<DreamValue> popped = state.PopCount(size * 2);
            for (int i = 0; i < popped.Length; i += 2) {
                DreamValue key = popped[i];

                if (key == DreamValue.Null) {
                    list.AddValue(popped[i + 1]);
                } else {
                    list.SetValue(key, popped[i + 1], allowGrowth: true);
                }
            }

            state.Push(new DreamValue(list));
            return null;
        }

        private static IDreamValueEnumerator GetContentsEnumerator(IDreamObjectTree objectTree, IDreamMapManager mapManager, DreamValue value, IDreamObjectTree.TreeEntry? filterType) {
            if (!value.TryGetValueAsDreamList(out var list)) {
                if (value.TryGetValueAsDreamObject(out var dreamObject)) {
                    if (dreamObject == null)
                        return new DreamValueArrayEnumerator(Array.Empty<DreamValue>());

                    if (dreamObject.IsSubtypeOf(objectTree.Atom)) {
                        list = dreamObject.GetVariable("contents").GetValueAsDreamList();
                    } else if (dreamObject.IsSubtypeOf(objectTree.World)) {
                        return new WorldContentsEnumerator(mapManager, filterType);
                    }
                }
            }

            if (list != null) {
                // world.contents has its own special enumerator to prevent the huge copy
                if (list is WorldContentsList)
                    return new WorldContentsEnumerator(mapManager, filterType);

                var values = list.GetValues().ToArray();

                return filterType == null
                    ? new DreamValueArrayEnumerator(values)
                    : new FilteredDreamValueArrayEnumerator(values, filterType);
            }

            throw new Exception($"Value {value} is not a {objectTree.List}, {objectTree.Atom}, {objectTree.World}, or null");
        }

        public static ProcStatus? CreateListEnumerator(DMProcState state) {
            var enumerator = GetContentsEnumerator(state.Proc.ObjectTree, state.Proc.DreamMapManager, state.Pop(), null);

            state.EnumeratorStack.Push(enumerator);
            return null;
        }

        public static ProcStatus? CreateFilteredListEnumerator(DMProcState state) {
            var filterTypeId = state.ReadInt();
            var filterType = state.Proc.ObjectTree.GetTreeEntry(filterTypeId);
            var enumerator = GetContentsEnumerator(state.Proc.ObjectTree, state.Proc.DreamMapManager, state.Pop(), filterType);

            state.EnumeratorStack.Push(enumerator);
            return null;
        }

        public static ProcStatus? CreateTypeEnumerator(DMProcState state) {
            DreamValue typeValue = state.Pop();
            if (!typeValue.TryGetValueAsType(out var type)) {
                throw new Exception($"Cannot create a type enumerator with type {typeValue}");
            }

            if (type == state.Proc.ObjectTree.Client) {
                state.EnumeratorStack.Push(new DreamObjectEnumerator(state.DreamManager.Clients));
                return null;
            }

            if (type.ObjectDefinition.IsSubtypeOf(state.Proc.ObjectTree.Atom)) {
                state.EnumeratorStack.Push(new WorldContentsEnumerator(state.Proc.DreamMapManager, type));
                return null;
            }

            if (type.ObjectDefinition.IsSubtypeOf(state.Proc.ObjectTree.Datum)) {
                state.EnumeratorStack.Push(new DreamObjectEnumerator(state.DreamManager.Datums));
                return null;
            }

            throw new Exception($"Type enumeration of {type} is not supported");
        }

        public static ProcStatus? CreateRangeEnumerator(DMProcState state) {
            float step = state.Pop().GetValueAsFloat();
            float rangeEnd = state.Pop().GetValueAsFloat();
            float rangeStart = state.Pop().GetValueAsFloat();

            state.EnumeratorStack.Push(new DreamValueRangeEnumerator(rangeStart, rangeEnd, step));
            return null;
        }

        public static ProcStatus? CreateObject(DMProcState state) {
            DreamProcArguments arguments = state.PopArguments();
            var val = state.Pop();
            if (!val.TryGetValueAsType(out var objectType)) {
                if (val.TryGetValueAsString(out var pathString)) {
                    if (!state.Proc.ObjectTree.TryGetTreeEntry(new DreamPath(pathString), out objectType)) {
                        throw new Exception($"Cannot create unknown object {val}");
                    }
                } else {
                    throw new Exception($"Cannot create object from invalid type {val}");
                }
            }

            DreamObjectDefinition objectDef = objectType.ObjectDefinition;
            if (objectDef.IsSubtypeOf(state.Proc.ObjectTree.Turf)) {
                // Turfs are special. They're never created outside of map initialization
                // So instead this will replace an existing turf's type and return that same turf
                DreamValue loc = arguments.GetArgument(0, "loc");
                if (!loc.TryGetValueAsDreamObjectOfType(state.Proc.ObjectTree.Turf, out var turf))
                    throw new Exception($"Invalid turf loc {loc}");

                state.Proc.DreamMapManager.SetTurf(turf, objectDef, arguments);

                state.Push(loc);
                return null;
            }

            DreamObject newObject = state.Proc.ObjectTree.CreateObject(objectType);
            state.Thread.PushProcState(newObject.InitProc(state.Thread, state.Usr, arguments));
            return ProcStatus.Called;
        }

        public static ProcStatus? DestroyEnumerator(DMProcState state) {
            state.EnumeratorStack.Pop();
            return null;
        }

        public static ProcStatus? Enumerate(DMProcState state) {
            IDreamValueEnumerator enumerator = state.EnumeratorStack.Peek();
            DMReference reference = state.ReadReference();
            int jumpToIfFailure = state.ReadInt();

            if (!enumerator.Enumerate(state, reference))
                state.Jump(jumpToIfFailure);

            return null;
        }

        /// <summary>
        /// Helper function of <see cref="FormatString"/> to handle text macros that are "suffix" (coming after the noun) pronouns
        /// </summary>
        /// <param name="pronouns">This should be in MALE,FEMALE,PLURAL,NEUTER order.</param>
        private static void HandleSuffixPronoun(ref StringBuilder formattedString, ReadOnlySpan<DreamValue> interps, int prevInterpIndex, string[] pronouns)
        {
            DreamObject? dreamObject;
            if (prevInterpIndex == -1 || prevInterpIndex >= interps.Length) // We should probably be throwing here
            {
                return;
            }
            interps[prevInterpIndex].TryGetValueAsDreamObject(out dreamObject);
            if (dreamObject == null)
            {
                return;
            }
            bool hasGender = dreamObject.TryGetVariable("gender", out var objectGender); // NOTE: in DM, this has to be a native property.
            if (!hasGender)
            {
                return;
            }
            if (!objectGender.TryGetValueAsString(out var genderStr))
                return;

            switch(genderStr)
            {
                case "male":
                    formattedString.Append(pronouns[0]);
                    return;
                case "female":
                    formattedString.Append(pronouns[1]);
                    return;
                case "plural":
                    formattedString.Append(pronouns[2]);
                    return;
                case "neuter":
                    formattedString.Append(pronouns[3]);
                    return;
                default:
                    return;
            }

        }
        public static ProcStatus? FormatString(DMProcState state) {
            string unformattedString = state.ReadString();
            StringBuilder formattedString = new StringBuilder();

            int interpCount = state.ReadInt();

            ReadOnlySpan<DreamValue> interps = state.PopCount(interpCount);
            int nextInterpIndex = 0; // If we find a prefix macro, this is what it points to
            int prevInterpIndex = -1; // If we find a suffix macro, this is what it points to (treating -1 as a 'null' state here)

            foreach(char c in unformattedString)
            {
                if (!StringFormatEncoder.Decode(c, out var formatType)) {
                    formattedString.Append(c);
                    continue;
                }
                switch (formatType) {
                    //Interp values
                    case StringFormatEncoder.FormatSuffix.StringifyWithArticle:{
                        formattedString.Append(interps[nextInterpIndex].Stringify());
                        prevInterpIndex = nextInterpIndex;
                        nextInterpIndex++;
                        continue;
                    }
                    case StringFormatEncoder.FormatSuffix.ReferenceOfValue: {
                        formattedString.Append(state.DreamManager.CreateRef(interps[nextInterpIndex]));
                        //suffix macro marker is not updated because suffixes do not point to \ref[] interpolations
                        nextInterpIndex++;
                        continue;
                    }
                    case StringFormatEncoder.FormatSuffix.StringifyNoArticle:
                    {
                        if (interps[nextInterpIndex].TryGetValueAsDreamObject(out var dreamObject) && dreamObject != null) {
                            formattedString.Append(dreamObject.GetNameUnformatted());
                        }
                        //Things that aren't objects just print nothing in this case
                        prevInterpIndex = nextInterpIndex;
                        nextInterpIndex++;
                        continue;
                    }
                    //Macro values//
                    //Prefix macros
                    case StringFormatEncoder.FormatSuffix.UpperDefiniteArticle:
                    case StringFormatEncoder.FormatSuffix.LowerDefiniteArticle:
                    {
                        if (interps[nextInterpIndex].TryGetValueAsDreamObject(out var dreamObject) && dreamObject != null)
                        {
                            bool hasName = dreamObject.TryGetVariable("name", out var objectName);
                            if (!hasName) continue;
                            string nameStr = objectName.Stringify();
                            if (!DreamObject.StringIsProper(nameStr))
                            {
                                formattedString.Append(formatType == StringFormatEncoder.FormatSuffix.UpperDefiniteArticle ? "The " : "the ");
                            }
                        }
                        continue;
                    }
                    case StringFormatEncoder.FormatSuffix.UpperIndefiniteArticle:
                    case StringFormatEncoder.FormatSuffix.LowerIndefiniteArticle:
                    {
                        bool wasCapital = formatType == StringFormatEncoder.FormatSuffix.UpperIndefiniteArticle; // saves some wordiness with the ternaries below
                        if (interps[nextInterpIndex].TryGetValueAsDreamObject(out var dreamObject) && dreamObject != null)
                        {
                            bool hasName = dreamObject.TryGetVariable("name", out var objectName);
                            string nameStr = objectName.Stringify();
                            if (!hasName) continue; // datums that lack a name var don't use articles
                            if (DreamObject.StringIsProper(nameStr)) continue; // Proper nouns don't need articles, I guess.

                            if (dreamObject.TryGetVariable("gender", out var gender)) // Aayy babe whats ya pronouns
                            {
                                if (gender.TryGetValueAsString(out var str) && str == "plural") // NOTE: In Byond, this part does not work if var/gender is not a native property of this object.
                                {
                                    formattedString.Append(wasCapital ? "Some" : "some");
                                    continue;
                                }
                            }
                            if (DreamObject.StringStartsWithVowel(nameStr))
                            {
                                formattedString.Append(wasCapital ? "An " : "an ");
                                continue;
                            }
                            formattedString.Append(wasCapital ? "A " : "a ");
                            continue;
                        }
                        continue;
                    }
                    //Suffix macros
                    case StringFormatEncoder.FormatSuffix.UpperSubjectPronoun:
                        HandleSuffixPronoun(ref formattedString, interps, prevInterpIndex, new string[] { "He", "She", "They", "Tt" });
                        break;
                    case StringFormatEncoder.FormatSuffix.LowerSubjectPronoun:
                        HandleSuffixPronoun(ref formattedString, interps, prevInterpIndex, new string[] { "he", "she", "they", "it" });
                        break;
                    case StringFormatEncoder.FormatSuffix.UpperPossessiveAdjective:
                        HandleSuffixPronoun(ref formattedString, interps, prevInterpIndex, new string[] { "His", "Her", "Their", "Its" });
                        break;
                    case StringFormatEncoder.FormatSuffix.LowerPossessiveAdjective:
                        HandleSuffixPronoun(ref formattedString, interps, prevInterpIndex, new string[] { "his", "her", "their", "its" });
                        break;
                    case StringFormatEncoder.FormatSuffix.ObjectPronoun:
                        HandleSuffixPronoun(ref formattedString, interps, prevInterpIndex, new string[] { "him", "her", "them", "it" });
                        break;
                    case StringFormatEncoder.FormatSuffix.ReflexivePronoun:
                        HandleSuffixPronoun(ref formattedString, interps, prevInterpIndex, new string[] { "himself", "herself", "themself", "itself" });
                        break;
                    case StringFormatEncoder.FormatSuffix.UpperPossessivePronoun:
                        HandleSuffixPronoun(ref formattedString, interps, prevInterpIndex, new string[] { "His", "Hers", "Theirs", "Its" });
                        break;
                    case StringFormatEncoder.FormatSuffix.LowerPossessivePronoun:
                        HandleSuffixPronoun(ref formattedString, interps, prevInterpIndex, new string[] { "his", "hers", "theirs", "its" });
                        break;
                    case StringFormatEncoder.FormatSuffix.PluralSuffix:
                        if (interps[prevInterpIndex].TryGetValueAsFloat(out var pluralNumber) && pluralNumber == 1)
                        {
                            continue;
                        }
                        formattedString.Append("s");
                        continue;
                    case StringFormatEncoder.FormatSuffix.OrdinalIndicator:
                        // TODO: if the preceding expression value is not a float, it should be replaced with 0 (0th)
                        if (interps[prevInterpIndex].TryGetValueAsFloat(out var ordinalNumber)) {
                            switch (ordinalNumber) {
                                case 1:
                                    formattedString.Append("st");
                                    break;
                                case 2:
                                    formattedString.Append("nd");
                                    break;
                                case 3:
                                    formattedString.Append("rd");
                                    break;
                                default:
                                    formattedString.Append("th");
                                    break;
                            }
                        } else {
                            formattedString.Append("th");
                        }
                        continue;
                    default:
                        if (Enum.IsDefined(typeof(StringFormatEncoder.FormatSuffix), formatType)) {
                            //Likely an unimplemented text macro, ignore it
                            break;
                        }

                        throw new Exception("Invalid special character");
                }
            }

            state.Push(new DreamValue(formattedString.ToString()));
            return null;
        }

        public static ProcStatus? Initial(DMProcState state) {
            DreamValue key = state.Pop();
            DreamValue owner = state.Pop();
            if (!key.TryGetValueAsString(out string property)) {
                throw new Exception("Invalid var for initial() call: " + key);
            }

            DreamObjectDefinition objectDefinition;
            if (owner.TryGetValueAsDreamObject(out DreamObject dreamObject)) {
                objectDefinition = dreamObject.ObjectDefinition;
            } else if (owner.TryGetValueAsType(out var ownerType)) {
                objectDefinition = ownerType.ObjectDefinition;
            } else {
                throw new Exception($"Invalid owner for initial() call {owner}");
            }

            state.Push(objectDefinition.Variables[property]);
            return null;
        }

        public static ProcStatus? IsNull(DMProcState state) {
            DreamValue value = state.Pop();

            state.Push(new DreamValue((value == DreamValue.Null) ? 1 : 0));
            return null;
        }

        public static ProcStatus? IsInList(DMProcState state) {
            DreamValue listValue = state.Pop();
            DreamValue value = state.Pop();

            if (listValue.TryGetValueAsDreamObject(out var listObject) && listObject != null) {
                DreamList? list = listObject as DreamList;

                if (list == null) {
                    if (listObject.IsSubtypeOf(state.Proc.ObjectTree.Atom) || listObject.IsSubtypeOf(state.Proc.ObjectTree.World)) {
                        list = listObject.GetVariable("contents").GetValueAsDreamList();
                    } else {
                        throw new Exception($"Value {listObject} is not a {state.Proc.ObjectTree.List}, {state.Proc.ObjectTree.Atom}, or {state.Proc.ObjectTree.World}");
                    }
                }

                state.Push(new DreamValue(list.ContainsValue(value) ? 1 : 0));
            } else {
                state.Push(new DreamValue(0));
            }

            return null;
        }

        public static ProcStatus? Pop(DMProcState state) {
            state.Pop();
            return null;
        }

        public static ProcStatus? PushArgumentList(DMProcState state) {
            if (state.Pop().TryGetValueAsDreamList(out var argList)) {
                List<DreamValue> ordered = new();
                Dictionary<string, DreamValue> named = new();
                foreach (DreamValue value in argList.GetValues()) {
                    if (argList.ContainsKey(value)) { //Named argument
                        if (value.TryGetValueAsString(out string name)) {
                            named.Add(name, argList.GetValue(value));
                        } else {
                            throw new Exception("List contains a non-string key, and cannot be used as an arglist");
                        }
                    } else { //Ordered argument
                        ordered.Add(value);
                    }
                }
                state.Push(new DreamProcArguments(ordered, named));
            } else {
                state.Push(new DreamProcArguments());
            }

            return null;
        }

        public static ProcStatus? PushArguments(DMProcState state) {
            int argumentCount = state.ReadInt();
            int namedCount = state.ReadInt();
            int unnamedCount = argumentCount - namedCount;
            DreamProcArguments arguments = new DreamProcArguments(unnamedCount > 0 ? new List<DreamValue>(unnamedCount) : null, namedCount > 0 ? new Dictionary<string, DreamValue>(namedCount) : null);
            ReadOnlySpan<DreamValue> argumentValues = argumentCount > 0 ? state.PopCount(argumentCount) : null;

            for (int i = 0; i < argumentCount; i++) {
                DreamProcOpcodeParameterType argumentType = (DreamProcOpcodeParameterType)state.ReadByte();

                switch (argumentType) {
                    case DreamProcOpcodeParameterType.Named: {
                        string argumentName = state.ReadString();

                        arguments.NamedArguments![argumentName] = argumentValues[i];
                        break;
                    }
                    case DreamProcOpcodeParameterType.Unnamed:
                        arguments.OrderedArguments!.Add(argumentValues[i]);
                        break;
                    default:
                        throw new Exception("Invalid argument type (" + argumentType + ")");
                }
            }

            state.Push(arguments);
            return null;
        }

        public static ProcStatus? PushFloat(DMProcState state) {
            float value = state.ReadFloat();

            state.Push(new DreamValue(value));
            return null;
        }

        public static ProcStatus? PushNull(DMProcState state) {
            state.Push(DreamValue.Null);
            return null;
        }

        public static ProcStatus? PushType(DMProcState state) {
            int typeId = state.ReadInt();
            var type = state.Proc.ObjectTree.Types[typeId];

            state.Push(new DreamValue(type));
            return null;
        }

        public static ProcStatus? PushProc(DMProcState state) {
            int procId = state.ReadInt();

            state.Push(new DreamValue(state.Proc.ObjectTree.Procs[procId]));
            return null;
        }

        public static ProcStatus? PushProcStub(DMProcState state) {
            int ownerTypeId = state.ReadInt();
            var owner = state.Proc.ObjectTree.GetTreeEntry(ownerTypeId);

            state.Push(DreamValue.CreateProcStub(owner));
            return null;
        }

        public static ProcStatus? PushVerbStub(DMProcState state) {
            int ownerTypeId = state.ReadInt();
            var owner = state.Proc.ObjectTree.GetTreeEntry(ownerTypeId);

            state.Push(DreamValue.CreateVerbStub(owner));
            return null;
        }

        public static ProcStatus? PushProcArguments(DMProcState state) {
            List<DreamValue> args = new(state.GetArguments().ToArray());

            state.Push(new DreamProcArguments(args));
            return null;
        }

        public static ProcStatus? PushResource(DMProcState state) {
            string resourcePath = state.ReadString();

            state.Push(new DreamValue(state.Proc.DreamResourceManager.LoadResource(resourcePath)));
            return null;
        }

        public static ProcStatus? PushString(DMProcState state) {
            state.Push(new DreamValue(state.ReadString()));
            return null;
        }

        public static ProcStatus? PushGlobalVars(DMProcState state) {
            state.Push(new DreamValue(new DreamGlobalVars(state.Proc.ObjectTree.List.ObjectDefinition)));
            return null;
        }
        #endregion Values

        #region Math
        public static ProcStatus? Add(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();
            DreamValue? output = null;

            if (second == DreamValue.Null) {
                output = first;
            } else if (first == DreamValue.Null) {
                output = second;
            } else switch (first.Type) {
                case DreamValue.DreamValueType.Float: {
                    float firstFloat = first.GetValueAsFloat();

                    output = second.Type switch {
                        DreamValue.DreamValueType.Float => new DreamValue(firstFloat + second.GetValueAsFloat()),
                        _ => null
                    };
                    break;
                }
                case DreamValue.DreamValueType.String when second.Type == DreamValue.DreamValueType.String:
                    output = new DreamValue(first.GetValueAsString() + second.GetValueAsString());
                    break;
                case DreamValue.DreamValueType.DreamObject: {
                    IDreamMetaObject metaObject = first.GetValueAsDreamObject().ObjectDefinition.MetaObject;

                    output = metaObject?.OperatorAdd(first, second);
                    break;
                }
            }

            if (output != null) {
                state.Push(output.Value);
            } else {
                throw new Exception("Invalid add operation on " + first + " and " + second);
            }

            return null;
        }

        public static ProcStatus? Append(DMProcState state) {
            DMReference reference = state.ReadReference();
            DreamValue second = state.Pop();
            DreamValue first = state.GetReferenceValue(reference, peek: true);

            DreamValue result;
            if (first.TryGetValueAsDreamObject(out var firstObj)) {
                if (firstObj != null) {
                    IDreamMetaObject metaObject = firstObj.ObjectDefinition.MetaObject;

                    if (metaObject != null) {
                        state.PopReference(reference);
                        state.Push(metaObject.OperatorAppend(first, second));

                        return null;
                    } else {
                        throw new Exception($"Invalid append operation on {first} and {second}");
                    }
                } else {
                    result = second;
                }
            } else if (first.TryGetValueAsDreamResource(out _) || first.TryGetValueAsDreamObjectOfType(state.Proc.ObjectTree.Icon, out _)) {
                // Implicitly create a new /icon and ICON_ADD blend it
                // Note that BYOND creates something other than an /icon, but it behaves the same as one in most reasonable interactions
                DreamObject iconObj = state.Proc.ObjectTree.CreateObject(state.Proc.ObjectTree.Icon);
                var icon = DreamMetaObjectIcon.InitializeIcon(state.Proc.DreamResourceManager, iconObj);
                if (!state.Proc.DreamResourceManager.TryLoadIcon(first, out var from))
                    throw new Exception($"Failed to create an icon from {from}");

                icon.InsertStates(from, DreamValue.Null, DreamValue.Null, DreamValue.Null);
                DreamProcNativeIcon.Blend(icon, second, DreamIconOperationBlend.BlendType.Add, 0, 0);
                result = new DreamValue(iconObj);
            } else if (second != DreamValue.Null) {
                switch (first.Type) {
                    case DreamValue.DreamValueType.Float when second.Type == DreamValue.DreamValueType.Float:
                        result = new DreamValue(first.GetValueAsFloat() + second.GetValueAsFloat());
                        break;
                    case DreamValue.DreamValueType.String when second.Type == DreamValue.DreamValueType.String:
                        result = new DreamValue(first.GetValueAsString() + second.GetValueAsString());
                        break;
                    default:
                        throw new Exception("Invalid append operation on " + first + " and " + second);
                }
            } else {
                result = first;
            }

            state.AssignReference(reference, result);
            state.Push(result);
            return null;
        }

        public static ProcStatus? Increment(DMProcState state) {
            DMReference reference = state.ReadReference();
            DreamValue value = state.GetReferenceValue(reference, peek: true);

            if (value.TryGetValueAsInteger(out int intValue)) {
                state.AssignReference(reference, new(intValue + 1));
            } else {
                //If it's not a number, it turns into 1
                state.AssignReference(reference, new(1));
            }

            state.Push(value);
            return null;
        }

        public static ProcStatus? Decrement(DMProcState state) {
            DMReference reference = state.ReadReference();
            DreamValue value = state.GetReferenceValue(reference, peek: true);

            if (value.TryGetValueAsInteger(out int intValue)) {
                state.AssignReference(reference, new(intValue - 1));
            } else {
                //If it's not a number, it turns into -1
                state.AssignReference(reference, new(-1));
            }

            state.Push(value);
            return null;
        }

        public static ProcStatus? BitAnd(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            if (first.TryGetValueAsDreamList(out DreamList list)) {
                DreamList newList = state.Proc.ObjectTree.CreateList();

                if (second.TryGetValueAsDreamList(out DreamList secondList)) {
                    int len = list.GetLength();

                    for (int i = 1; i <= len; i++) {
                        DreamValue value = list.GetValue(new DreamValue(i));

                        if (secondList.ContainsValue(value)) {
                            DreamValue associativeValue = list.GetValue(value);

                            newList.AddValue(value);
                            if (associativeValue != DreamValue.Null) newList.SetValue(value, associativeValue);
                        }
                    }
                } else {
                    int len = list.GetLength();

                    for (int i = 1; i <= len; i++) {
                        DreamValue value = list.GetValue(new DreamValue(i));

                        if (value == second) {
                            DreamValue associativeValue = list.GetValue(value);

                            newList.AddValue(value);
                            if (associativeValue != DreamValue.Null) newList.SetValue(value, associativeValue);
                        }
                    }
                }

                state.Push(new DreamValue(newList));
            } else if (first != DreamValue.Null && second != DreamValue.Null) {
                state.Push(new DreamValue(first.GetValueAsInteger() & second.GetValueAsInteger()));
            } else {
                state.Push(new DreamValue(0));
            }

            return null;
        }

        public static ProcStatus? BitNot(DMProcState state) {
            var input = state.Pop();
            if (input.TryGetValueAsInteger(out var value)) {
                state.Push(new DreamValue((~value) & 0xFFFFFF));
            } else {
                if (input.TryGetValueAsDreamObjectOfType(state.Proc.ObjectTree.Matrix, out _)) // TODO ~ on /matrix
                {
                    throw new NotImplementedException("/matrix does not support the '~' operator yet");
                }

                state.Push(new DreamValue(16777215)); // 2^24 - 1
            }

            return null;
        }

        public static ProcStatus? BitOr(DMProcState state) {                        // x | y
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            if (first.Type == DreamValue.DreamValueType.DreamObject) {              // Object | y
                if (first != DreamValue.Null) {
                    IDreamMetaObject metaObject = first.MustGetValueAsDreamObject().ObjectDefinition.MetaObject;

                    if (metaObject != null) {
                        state.Push(metaObject.OperatorOr(first, second));
                    } else {
                        throw new Exception("Invalid or operation on " + first + " and " + second);
                    }
                } else {
                    state.Push(DreamValue.Null);
                }
            } else if (second != DreamValue.Null) {                                      // Non-Object | y
                switch (first.Type) {
                    case DreamValue.DreamValueType.Float when second.Type == DreamValue.DreamValueType.Float:
                        state.Push(new DreamValue(first.MustGetValueAsInteger() | second.MustGetValueAsInteger()));
                        break;
                    default:
                        throw new Exception("Invalid or operation on " + first + " and " + second);
                }
            } else if (first.TryGetValueAsInteger(out int firstInt)) {
                state.Push(new DreamValue(firstInt));
            } else {
                throw new Exception("Invalid or operation on " + first + " and " + second);
            }

            return null;
        }

        public static ProcStatus? BitShiftLeft(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            switch (first.Type) {
                case DreamValue.DreamValueType.DreamObject when first == DreamValue.Null:
                    state.Push(new DreamValue(0));
                    break;
                case DreamValue.DreamValueType.Float when second.Type == DreamValue.DreamValueType.Float:
                    state.Push(new DreamValue(first.MustGetValueAsInteger() << second.MustGetValueAsInteger()));
                    break;
                default:
                    throw new Exception($"Invalid bit shift left operation on {first} and {second}");
            }

            return null;
        }


        public static ProcStatus? BitShiftLeftReference(DMProcState state) {
            DMReference reference = state.ReadReference();
            DreamValue second = state.Pop();
            DreamValue first = state.GetReferenceValue(reference, peek: true);
            DreamValue result;
            switch (first.Type) {
                case DreamValue.DreamValueType.DreamObject when first == DreamValue.Null:
                    result = new DreamValue(0);
                    break;
                case DreamValue.DreamValueType.Float when second.Type == DreamValue.DreamValueType.Float:
                    result = new DreamValue(first.MustGetValueAsInteger() << second.MustGetValueAsInteger());
                    break;
                default:
                    throw new Exception($"Invalid bit shift left operation on {first} and {second}");
            }
            state.AssignReference(reference, result);
            state.Push(result);
            return null;
        }

        public static ProcStatus? BitShiftRight(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            if (first == DreamValue.Null) {
                state.Push(new DreamValue(0));
            } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                state.Push(new DreamValue(first.MustGetValueAsInteger() >> second.MustGetValueAsInteger()));
            } else {
                throw new Exception($"Invalid bit shift right operation on {first} and {second}");
            }

            return null;
        }

        public static ProcStatus? BitShiftRightReference(DMProcState state) {
            DMReference reference = state.ReadReference();
            DreamValue second = state.Pop();
            DreamValue first = state.GetReferenceValue(reference, peek: true);
            DreamValue result;
            switch (first.Type) {
                case DreamValue.DreamValueType.DreamObject when first == DreamValue.Null:
                    result = new DreamValue(0);
                    break;
                case DreamValue.DreamValueType.Float when second.Type == DreamValue.DreamValueType.Float:
                    result = new DreamValue(first.MustGetValueAsInteger() >> second.MustGetValueAsInteger());
                    break;
                default:
                    throw new Exception($"Invalid bit shift right operation on {first} and {second}");
            }
            state.AssignReference(reference, result);
            state.Push(result);
            return null;
        }

        public static ProcStatus? BitXor(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            state.Push(BitXorValues(state.Proc.ObjectTree, first, second));
            return null;
        }

        public static ProcStatus? BitXorReference(DMProcState state) {
            DreamValue second = state.Pop();
            DMReference reference = state.ReadReference();
            DreamValue first = state.GetReferenceValue(reference, peek: true);
            DreamValue result = BitXorValues(state.Proc.ObjectTree, first, second);

            state.AssignReference(reference, result);
            state.Push(result);
            return null;
        }

        public static ProcStatus? BooleanAnd(DMProcState state) {
            DreamValue a = state.Pop();
            int jumpPosition = state.ReadInt();

            if (!a.IsTruthy()) {
                state.Push(a);
                state.Jump(jumpPosition);
            }

            return null;
        }

        public static ProcStatus? BooleanNot(DMProcState state) {
            DreamValue value = state.Pop();

            state.Push(new DreamValue(value.IsTruthy() ? 0 : 1));
            return null;
        }

        public static ProcStatus? BooleanOr(DMProcState state) {
            DreamValue a = state.Pop();
            int jumpPosition = state.ReadInt();

            if (a.IsTruthy()) {
                state.Push(a);
                state.Jump(jumpPosition);
            }
            return null;
        }

        public static ProcStatus? Combine(DMProcState state) {
            DMReference reference = state.ReadReference();
            DreamValue second = state.Pop();
            DreamValue first = state.GetReferenceValue(reference, peek: true);

            DreamValue result;
            if (first.TryGetValueAsDreamObject(out var firstObj)) {
                if (firstObj != null) {
                    IDreamMetaObject metaObject = firstObj.ObjectDefinition.MetaObject;

                    if (metaObject != null) {
                        state.PopReference(reference);
                        state.Push(metaObject.OperatorCombine(first, second));

                        return null;
                    } else {
                        throw new Exception("Invalid combine operation on " + first + " and " + second);
                    }
                } else {
                    result = second;
                }
            } else if (second != DreamValue.Null) {
                if (first.TryGetValueAsInteger(out var firstInt) && second.TryGetValueAsInteger(out var secondInt)) {
                    result = new DreamValue(firstInt | secondInt);
                } else if (first == DreamValue.Null) {
                    result = second;
                } else {
                    throw new Exception("Invalid combine operation on " + first + " and " + second);
                }
            } else if (first.Type == DreamValue.DreamValueType.Float) {
                result = first;
            } else {
                throw new Exception("Invalid combine operation on " + first + " and " + second);
            }

            state.AssignReference(reference, result);
            state.Push(result);
            return null;
        }

        public static ProcStatus? Divide(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            state.Push(DivideValues(first, second));
            return null;
        }

        public static ProcStatus? DivideReference(DMProcState state) {
            DMReference reference = state.ReadReference();
            DreamValue second = state.Pop();
            DreamValue first = state.GetReferenceValue(reference, peek: true);
            DreamValue result = DivideValues(first, second);

            state.AssignReference(reference, result);
            state.Push(result);
            return null;
        }

        public static ProcStatus? Mask(DMProcState state) {
            DMReference reference = state.ReadReference();
            DreamValue second = state.Pop();
            DreamValue first = state.GetReferenceValue(reference, peek: true);

            DreamValue result;
            switch (first.Type) {
                case DreamValue.DreamValueType.DreamObject when first != DreamValue.Null: {
                    IDreamMetaObject metaObject = first.GetValueAsDreamObject().ObjectDefinition.MetaObject;

                    if (metaObject != null) {
                        state.PopReference(reference);
                        state.Push(metaObject.OperatorMask(first, second));

                        return null;
                    } else {
                        throw new Exception("Invalid mask operation on " + first + " and " + second);
                    }
                }
                case DreamValue.DreamValueType.DreamObject:
                    result = new DreamValue(0);
                    break;
                case DreamValue.DreamValueType.Float when second.Type == DreamValue.DreamValueType.Float:
                    result = new DreamValue(first.GetValueAsInteger() & second.GetValueAsInteger());
                    break;
                default:
                    throw new Exception("Invalid mask operation on " + first + " and " + second);
            }

            state.AssignReference(reference, result);
            state.Push(result);
            return null;
        }

        public static ProcStatus? Modulus(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                state.Push(new DreamValue(first.GetValueAsInteger() % second.GetValueAsInteger()));
            } else {
                throw new Exception("Invalid modulus operation on " + first + " and " + second);
            }

            return null;
        }

        public static ProcStatus? ModulusModulus(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            state.Push(ModulusModulusValues(first, second));

            return null;
        }

        public static ProcStatus? ModulusReference(DMProcState state) {
            DreamValue second = state.Pop();
            DMReference reference = state.ReadReference();
            DreamValue first = state.GetReferenceValue(reference, peek: true);
            DreamValue result = ModulusValues(first, second);

            state.AssignReference(reference, result);
            state.Push(result);
            return null;
        }

        public static ProcStatus? ModulusModulusReference(DMProcState state) {
            DreamValue second = state.Pop();
            DMReference reference = state.ReadReference();
            DreamValue first = state.GetReferenceValue(reference, peek: true);
            DreamValue result = ModulusModulusValues(first, second);

            state.AssignReference(reference, result);
            state.Push(result);
            return null;
        }

        public static ProcStatus? Multiply(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            state.Push(MultiplyValues(first, second));
            return null;
        }

        public static ProcStatus? MultiplyReference(DMProcState state) {
            DMReference reference = state.ReadReference();
            DreamValue second = state.Pop();
            DreamValue first = state.GetReferenceValue(reference, peek: true);
            DreamValue result = MultiplyValues(first, second);

            state.AssignReference(reference, result);
            state.Push(result);
            return null;
        }

        public static ProcStatus? Negate(DMProcState state) {
            DreamValue value = state.Pop();

            switch (value.Type) {
                case DreamValue.DreamValueType.Float: state.Push(new DreamValue(-value.GetValueAsFloat())); break;
                case DreamValue.DreamValueType.DreamObject when value == DreamValue.Null: state.Push(new DreamValue(0.0f)); break;
                default: throw new Exception("Invalid negate operation on " + value);
            }

            return null;
        }

        public static ProcStatus? Power(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            if (first.TryGetValueAsFloat(out var floatFirst) && second.TryGetValueAsFloat(out var floatSecond)) {
                state.Push(new DreamValue(MathF.Pow(floatFirst, floatSecond)));
            } else {
                throw new Exception("Invalid power operation on " + first + " and " + second);
            }

            return null;
        }

        public static ProcStatus? Remove(DMProcState state) {
            DMReference reference = state.ReadReference();
            DreamValue second = state.Pop();
            DreamValue first = state.GetReferenceValue(reference, peek: true);

            DreamValue result;
            switch (first.Type) {
                case DreamValue.DreamValueType.DreamObject when first != DreamValue.Null: {
                    IDreamMetaObject metaObject = first.GetValueAsDreamObject().ObjectDefinition.MetaObject;

                    if (metaObject != null) {
                        state.PopReference(reference);
                        state.Push(metaObject.OperatorRemove(first, second));

                        return null;
                    } else {
                        throw new Exception("Invalid remove operation on " + first + " and " + second);
                    }
                }
                case DreamValue.DreamValueType.DreamObject when second.Type == DreamValue.DreamValueType.Float:
                    result = new DreamValue(-second.GetValueAsFloat());
                    break;
                case DreamValue.DreamValueType.Float when second.Type == DreamValue.DreamValueType.Float:
                    result = new DreamValue(first.GetValueAsFloat() - second.GetValueAsFloat());
                    break;
                default:
                    throw new Exception("Invalid remove operation on " + first + " and " + second);
            }

            state.AssignReference(reference, result);
            state.Push(result);
            return null;
        }

        public static ProcStatus? Subtract(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();
            DreamValue? output = null;

            if (second == DreamValue.Null) {
                output = first;
            } else if (first == DreamValue.Null && second.Type == DreamValue.DreamValueType.Float) {
                output = new DreamValue(-second.GetValueAsFloat());
            } else switch (first.Type) {
                case DreamValue.DreamValueType.Float: {
                    float firstFloat = first.GetValueAsFloat();

                    output = second.Type switch {
                        DreamValue.DreamValueType.Float => new DreamValue(firstFloat - second.GetValueAsFloat()),
                        _ => null
                    };
                    break;
                }
                case DreamValue.DreamValueType.DreamObject: {
                    IDreamMetaObject metaObject = first.GetValueAsDreamObject().ObjectDefinition.MetaObject;

                    if (metaObject != null) {
                        output = metaObject.OperatorSubtract(first, second);
                    }
                    break;
                }
            }

            if (output != null) {
                state.Push(output.Value);
            } else {
                throw new Exception("Invalid subtract operation on " + first + " and " + second);
            }

            return null;
        }
        #endregion Math

        #region Comparisons
        public static ProcStatus? CompareEquals(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            state.Push(new DreamValue(IsEqual(first, second) ? 1 : 0));
            return null;
        }

        public static ProcStatus? CompareEquivalent(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            state.Push(new DreamValue(IsEquivalent(first, second) ? 1 : 0));
            return null;
        }

        public static ProcStatus? CompareGreaterThan(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            state.Push(new DreamValue(IsGreaterThan(first, second) ? 1 : 0));
            return null;
        }

        public static ProcStatus? CompareGreaterThanOrEqual(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();
            DreamValue result;

            if (first.TryGetValueAsFloat(out float lhs) && lhs == 0.0 && second == DreamValue.Null) result = new DreamValue(1);
            else if (first == DreamValue.Null && second.TryGetValueAsFloat(out float rhs) && rhs == 0.0) result = new DreamValue(1);
            else if (first == DreamValue.Null && second.TryGetValueAsString(out var s) && s == "") result = new DreamValue(1);
            else result = new DreamValue((IsEqual(first, second) || IsGreaterThan(first, second)) ? 1 : 0);

            state.Push(result);
            return null;
        }

        public static ProcStatus? CompareLessThan(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            state.Push(new DreamValue(IsLessThan(first, second) ? 1 : 0));
            return null;
        }

        public static ProcStatus? CompareLessThanOrEqual(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();
            DreamValue result;

            if (first.TryGetValueAsFloat(out float lhs) && lhs == 0.0 && second == DreamValue.Null) result = new DreamValue(1);
            else if (first == DreamValue.Null && second.TryGetValueAsFloat(out float rhs) && rhs == 0.0) result = new DreamValue(1);
            else if (first == DreamValue.Null && second.TryGetValueAsString(out var s) && s == "") result = new DreamValue(1);
            else result = new DreamValue((IsEqual(first, second) || IsLessThan(first, second)) ? 1 : 0);

            state.Push(result);
            return null;
        }

        public static ProcStatus? CompareNotEquals(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            state.Push(new DreamValue(IsEqual(first, second) ? 0 : 1));
            return null;
        }

        public static ProcStatus? CompareNotEquivalent(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            state.Push(new DreamValue(IsEquivalent(first, second) ? 0 : 1));
            return null;
        }

        public static ProcStatus? IsInRange(DMProcState state)
        {
            DreamValue end = state.Pop();
            DreamValue start = state.Pop();
            DreamValue var = state.Pop();
            if (var.Type != DreamValue.DreamValueType.Float) var = new DreamValue(0f);
            if (start.Type != DreamValue.DreamValueType.Float) start = new DreamValue(0f);
            if (end.Type != DreamValue.DreamValueType.Float) end = new DreamValue(0f);
            bool inRange = (IsEqual(start, var) || IsLessThan(start, var)) && (IsEqual(var, end) || IsLessThan(var, end));
            state.Push(new DreamValue(inRange ? 1 : 0));
            return null;
        }

        public static ProcStatus? IsType(DMProcState state) {
            DreamValue typeValue = state.Pop();
            DreamValue value = state.Pop();
            IDreamObjectTree.TreeEntry type;

            if (typeValue.TryGetValueAsDreamObject(out var typeObject)) {
                if (typeObject == null) {
                    state.Push(new DreamValue(0));
                    return null;
                }

                type = typeObject.ObjectDefinition.TreeEntry;
            } else {
                if(!typeValue.TryGetValueAsType(out type)) {
                    throw new Exception($"istype() attempted to check non-path {typeValue}");
                }
            }

            if (value.TryGetValueAsDreamObject(out var dreamObject) && dreamObject != null) {
                state.Push(new DreamValue(dreamObject.IsSubtypeOf(type) ? 1 : 0));
            } else {
                state.Push(new DreamValue(0));
            }

            return null;
        }
        #endregion Comparisons

        #region Flow
        public static ProcStatus? Call(DMProcState state) {
            DMReference procRef = state.ReadReference();
            DreamProcArguments arguments = state.PopArguments();

            DreamObject instance;
            DreamProc proc;
            switch (procRef.RefType) {
                case DMReference.Type.Self: {
                    instance = state.Instance;
                    proc = state.Proc;
                    break;
                }
                case DMReference.Type.SuperProc: {
                    instance = state.Instance;
                    proc = state.Proc.SuperProc;

                    if (proc == null) {
                        //Attempting to call a super proc where there is none will just return null
                        state.Push(DreamValue.Null);
                        return null;
                    }

                    break;
                }
                case DMReference.Type.Proc: {
                    DreamValue owner = state.Pop();
                    if (!owner.TryGetValueAsDreamObject(out instance) || instance == null)
                        throw new Exception($"Cannot dereference proc \"{procRef.Name}\" from {owner}");
                    if (!instance.TryGetProc(procRef.Name, out proc))
                        throw new Exception($"Type {instance.ObjectDefinition.Type} has no proc called \"{procRef.Name}\"");

                    break;
                }
                case DMReference.Type.GlobalProc: {
                    instance = null;
                    proc = state.Proc.ObjectTree.Procs[procRef.Index];

                    break;
                }
                case DMReference.Type.SrcProc: {
                    instance = state.Instance;
                    if (!instance.TryGetProc(procRef.Name, out proc))
                        throw new Exception($"Type {instance.ObjectDefinition.Type} has no proc called \"{procRef.Name}\"");

                    break;
                }
                default: throw new Exception($"Invalid proc reference type {procRef.RefType}");
            }

            state.Call(proc, instance, arguments);
            return ProcStatus.Called;
        }

        public static ProcStatus? CallStatement(DMProcState state) {
            DreamProcArguments arguments = state.PopArguments();
            DreamValue source = state.Pop();

            switch (source.Type) {
                case DreamValue.DreamValueType.DreamObject: {
                    DreamObject dreamObject = source.GetValueAsDreamObject();
                    DreamValue procId = state.Pop();
                    DreamProc proc = null;

                    switch (procId.Type) {
                        case DreamValue.DreamValueType.String:
                            proc = dreamObject.GetProc(procId.GetValueAsString());
                            break;
                        case DreamValue.DreamValueType.DreamProc: {
                            proc = procId.MustGetValueAsProc();
                            break;
                        }
                    }

                    if (proc != null) {
                        state.Call(proc, dreamObject, arguments);
                        return ProcStatus.Called;
                    }

                    throw new Exception($"Invalid proc ({procId})");
                }
                case DreamValue.DreamValueType.DreamProc:
                    state.Call(source.MustGetValueAsProc(), state.Instance, arguments);
                    return ProcStatus.Called;
                case DreamValue.DreamValueType.String:
                    unsafe {
                        if(!source.TryGetValueAsString(out var dllName))
                            throw new Exception($"{source} is not a valid DLL");

                        var popProc = state.Pop();
                        if(!popProc.TryGetValueAsString(out var procName)) {
                            throw new Exception($"{popProc} is not a valid proc name");
                        }

                        // DLL Invoke
                        var entryPoint = DllHelper.ResolveDllTarget(state.Proc.DreamResourceManager, dllName, procName);

                        Span<nint> argV = stackalloc nint[arguments.ArgumentCount];
                        argV.Fill(0);
                        try {
                            for (var i = 0; i < argV.Length; i++) {
                                var arg = arguments.OrderedArguments[i].Stringify();
                                argV[i] = Marshal.StringToCoTaskMemUTF8(arg);
                            }

                            byte* ret;
                            if (arguments.ArgumentCount > 0) {
                                fixed (nint* ptr = &argV[0]) {
                                    ret = entryPoint(arguments.ArgumentCount, (byte**)ptr);
                                }
                            } else {
                                ret = entryPoint(0, (byte**)0);
                            }

                            if (ret == null) {
                                state.Push(DreamValue.Null);
                                return null;
                            }

                            var retString = Marshal.PtrToStringUTF8((nint)ret);
                            state.Push(new DreamValue(retString));
                            return null;
                        } finally {
                            foreach (var arg in argV) {
                                if (arg != 0)
                                    Marshal.ZeroFreeCoTaskMemUTF8(arg);
                            }
                        }
                    }
                default:
                    throw new Exception($"Call statement has an invalid source ({source})");
            }
        }

        public static ProcStatus? Error(DMProcState state) {
            throw new Exception("Reached an error opcode");
        }

        public static ProcStatus? Jump(DMProcState state) {
            int position = state.ReadInt();

            state.Jump(position);
            return null;
        }

        public static ProcStatus? JumpIfFalse(DMProcState state) {
            int position = state.ReadInt();
            DreamValue value = state.Pop();

            if (!value.IsTruthy()) {
                state.Jump(position);
            }

            return null;
        }

        public static ProcStatus? JumpIfTrue(DMProcState state) {
            int position = state.ReadInt();
            DreamValue value = state.Pop();

            if (value.IsTruthy()) {
                state.Jump(position);
            }

            return null;
        }

        public static ProcStatus? JumpIfNullDereference(DMProcState state) {
            DMReference reference = state.ReadReference();
            int position = state.ReadInt();

            if (state.IsNullDereference(reference)) {
                state.Push(DreamValue.Null);
                state.Jump(position);
            }

            return null;
        }

        public static ProcStatus? Return(DMProcState state) {
            state.SetReturn(state.Pop());
            return ProcStatus.Returned;
        }

        public static ProcStatus? Throw(DMProcState state) {
            DreamValue value = state.Pop();

            throw new DMThrowException(value);
        }

        public static ProcStatus? Try(DMProcState state) {
            state.StartTryBlock(state.ReadInt(), state.ReadReference().Index);
            return null;
        }

        public static ProcStatus? TryNoValue(DMProcState state) {
            state.StartTryBlock(state.ReadInt());
            return null;
        }

        public static ProcStatus? EndTry(DMProcState state) {
            state.EndTryBlock();
            return null;
        }

        public static ProcStatus? SwitchCase(DMProcState state) {
            int casePosition = state.ReadInt();
            DreamValue testValue = state.Pop();
            DreamValue value = state.Pop();

            if (IsEqual(value, testValue)) {
                state.Jump(casePosition);
            } else {
                state.Push(value);
            }

            return null;
        }

        public static ProcStatus? SwitchCaseRange(DMProcState state) {
            int casePosition = state.ReadInt();
            DreamValue rangeUpper = state.Pop();
            DreamValue rangeLower = state.Pop();
            DreamValue value = state.Pop();

            bool matchesLower = IsGreaterThan(value, rangeLower) || IsEqual(value, rangeLower);
            bool matchesUpper = IsLessThan(value, rangeUpper) || IsEqual(value, rangeUpper);
            if (matchesLower && matchesUpper) {
                state.Jump(casePosition);
            } else {
                state.Push(value);
            }

            return null;
        }

        //Copy & run the interpreter in a new thread
        //Jump the current thread to after the spawn's code
        public static ProcStatus? Spawn(DMProcState state) {
            int jumpTo = state.ReadInt();
            float delay = state.Pop().GetValueAsFloat();
            int delayMilliseconds = (int)(delay * 100);

            // TODO: It'd be nicer if we could use something such as DreamThread.Spawn here
            // and have state.Spawn return a ProcState instead
            DreamThread newContext = state.Spawn();

            //Negative delays mean the spawned code runs immediately
            if (delayMilliseconds < 0) {
                newContext.Resume();
                // TODO: Does the rest of the proc get scheduled?
                // Does the value of the delay mean anything?
            } else {
                new Task(async () => {
                    if (delayMilliseconds != 0) {
                        await Task.Delay(delayMilliseconds);
                    } else {
                        await Task.Yield();
                    }
                    newContext.Resume();
                }).Start(TaskScheduler.FromCurrentSynchronizationContext());
            }

            state.Jump(jumpTo);
            return null;
        }

        public static ProcStatus? DebugSource(DMProcState state) {
            string source = state.ReadString();

            state.CurrentSource = source;
            return null;
        }

        public static ProcStatus? DebugLine(DMProcState state) {
            int line = state.ReadInt();

            state.CurrentLine = line;
            state.DebugManager.HandleLineChange(state, line);
            return null;
        }
        #endregion Flow

        #region Others

        private static void PerformOutput(DreamValue a, DreamValue b) {
            if (a == DreamValue.Null)
                return;

            if (a.TryGetValueAsDreamResource(out var resource)) {
                resource.Output(b);
            } else if (a.TryGetValueAsDreamObject(out var dreamObject)) {
                IDreamMetaObject? metaObject = dreamObject!.ObjectDefinition?.MetaObject;

                metaObject?.OperatorOutput(a, b);
            } else {
                throw new NotImplementedException($"Unimplemented output operation between {a} and {b}");
            }
        }

        public static ProcStatus? OutputReference(DMProcState state) {
            DMReference leftRef = state.ReadReference();
            DreamValue right = state.Pop();

            if (leftRef.RefType == DMReference.Type.ListIndex) {
                (DreamValue indexing, _) = state.GetIndexReferenceValues(leftRef, peek: true);

                if (indexing.TryGetValueAsDreamObjectOfType(state.Proc.ObjectTree.Savefile, out var savefile)) {
                    // Savefiles get some special treatment.
                    // "savefile[A] << B" is the same as "savefile[A] = B"

                    state.AssignReference(leftRef, right);
                    return null;
                }
            }

            PerformOutput(state.GetReferenceValue(leftRef), right);
            return null;
        }

        public static ProcStatus? Output(DMProcState state) {
            DreamValue right = state.Pop();
            DreamValue left = state.Pop();

            PerformOutput(left, right);
            return null;
        }

        public static ProcStatus? Input(DMProcState state) {
            DMReference leftRef = state.ReadReference();
            DMReference rightRef = state.ReadReference();

            if (leftRef.RefType == DMReference.Type.ListIndex) {
                (DreamValue indexing, _) = state.GetIndexReferenceValues(leftRef, peek: true);

                if (indexing.TryGetValueAsDreamObjectOfType(state.Proc.ObjectTree.Savefile, out var savefile)) {
                    // Savefiles get some special treatment.
                    // "savefile[A] >> B" is the same as "B = savefile[A]"

                    state.AssignReference(rightRef, state.GetReferenceValue(leftRef));
                    return null;
                } else {
                    // Pop the reference's stack values
                    state.GetReferenceValue(leftRef);
                    state.GetReferenceValue(rightRef);
                }
            }

            throw new NotImplementedException($"Input operation is unimplemented for {leftRef} and {rightRef}");
        }

        public static ProcStatus? Browse(DMProcState state) {
            state.Pop().TryGetValueAsString(out string? options);
            DreamValue body = state.Pop();
            DreamObject receiver = state.Pop().GetValueAsDreamObject();

            IEnumerable<DreamConnection> clients;
            if (receiver.IsSubtypeOf(state.Proc.ObjectTree.Mob)) {
                clients = new[] { state.DreamManager.GetConnectionFromMob(receiver) };
            } else if (receiver.IsSubtypeOf(state.Proc.ObjectTree.Client)) {
                clients = new[] { state.DreamManager.GetConnectionFromClient(receiver) };
            } else if (receiver == state.DreamManager.WorldInstance) {
                clients = state.DreamManager.Connections;
            } else {
                throw new Exception($"Invalid browse() recipient: expected mob, client, or world, got {receiver}");
            }

            string? browseValue;
            if (body.TryGetValueAsDreamResource(out var resource)) {
                browseValue = resource.ReadAsString();
            } else if (body.TryGetValueAsString(out browseValue) || body == DreamValue.Null) {
                // Got it.
            } else {
                throw new Exception($"Invalid browse() body: expected resource or string, got {body}");
            }

            foreach (DreamConnection client in clients) {
                client?.Browse(browseValue, options);
            }

            return null;
        }

        public static ProcStatus? BrowseResource(DMProcState state) {
            DreamValue filename = state.Pop();
            var value = state.Pop();

            if (!value.TryGetValueAsDreamResource(out var file)) {
                if (state.Proc.DreamResourceManager.TryLoadIcon(value, out var icon)) {
                    file = icon;
                } else {
                    throw new NotImplementedException();
                }
            }

            DreamObject receiver = state.Pop().GetValueAsDreamObject();

            DreamObject client;
            if (receiver.IsSubtypeOf(state.Proc.ObjectTree.Mob)) {
                client = receiver.GetVariable("client").GetValueAsDreamObject();
            } else if (receiver.IsSubtypeOf(state.Proc.ObjectTree.Client)) {
                client = receiver;
            } else {
                throw new Exception("Invalid browse_rsc() recipient");
            }

            if (client != null) {
                DreamConnection connection = state.DreamManager.GetConnectionFromClient(client);

                connection.BrowseResource(file, (filename != DreamValue.Null) ? filename.GetValueAsString() : Path.GetFileName(file.ResourcePath));
            }

            return null;
        }

        public static ProcStatus? DeleteObject(DMProcState state) {
            DreamObject dreamObject = state.Pop().GetValueAsDreamObject();

            dreamObject?.Delete(state.DreamManager);
            if (dreamObject is not null && dreamObject == state.Instance) {
                return ProcStatus.Returned;
            }
            return null;
        }

        public static ProcStatus? OutputControl(DMProcState state) {
            string control = state.Pop().GetValueAsString();
            DreamValue message = state.Pop();
            DreamObject receiver = state.Pop().GetValueAsDreamObject();

            if (receiver == state.DreamManager.WorldInstance) {
                //Same as "world << ..."
                receiver.ObjectDefinition.MetaObject.OperatorOutput(new(receiver), message);
                return null;
            }

            DreamObject? client;
            if (receiver.IsSubtypeOf(state.Proc.ObjectTree.Mob)) {
                receiver.GetVariable("client").TryGetValueAsDreamObjectOfType(state.Proc.ObjectTree.Client, out client);
            } else if (receiver.IsSubtypeOf(state.Proc.ObjectTree.Client)) {
                client = receiver;
            } else {
                throw new Exception("Invalid output() recipient");
            }

            if (client != null) {
                DreamConnection connection = state.DreamManager.GetConnectionFromClient(client);
                if (!message.TryGetValueAsString(out var messageStr) && message != DreamValue.Null)
                    throw new Exception($"Invalid output() message {message}");

                connection.OutputControl(messageStr, control);
            }

            // TODO: When errors are more strict (or a setting for it added), a null client should error

            return null;
        }

        public static ProcStatus? Prompt(DMProcState state) {
            DMValueType types = (DMValueType)state.ReadInt();
            DreamValue list = state.Pop();
            DreamValue message, title, defaultValue;

            DreamValue firstArg = state.Pop();
            if (firstArg.TryGetValueAsDreamObjectOfType(state.Proc.ObjectTree.Mob, out var recipientMob)) {
                message = state.Pop();
                title = state.Pop();
                defaultValue = state.Pop();
            } else {
                recipientMob = state.Usr;
                message = firstArg;
                title = state.Pop();
                defaultValue = state.Pop();
                state.Pop(); //Fourth argument, should be null
            }

            if (recipientMob == null) {
                state.Push(DreamValue.Null);
                return null;
            }

            if (recipientMob.GetVariable("client").TryGetValueAsDreamObjectOfType(state.Proc.ObjectTree.Client, out var clientObject)) {
                DreamConnection? connection = state.DreamManager.GetConnectionFromClient(clientObject);
                if (connection == null) {
                    state.Push(DreamValue.Null);
                    return null;
                }

                Task<DreamValue> promptTask;
                if (list.TryGetValueAsDreamList(out var valueList)) {
                    promptTask = connection.PromptList(types, valueList, title.Stringify(), message.Stringify(), defaultValue);
                } else {
                    promptTask = connection.Prompt(types, title.Stringify(), message.Stringify(), defaultValue.Stringify());
                }

                // Could use a better solution. Either no anonymous async native proc at all, or just a better way to call them.
                var waiter = AsyncNativeProc.CreateAnonymousState(state.Thread, async _ => await promptTask);
                state.Thread.PushProcState(waiter);
                return ProcStatus.Called;
            }

            state.Push(DreamValue.Null);
            return null;
        }

        public static ProcStatus? LocateCoord(DMProcState state)
        {
            var z = state.Pop();
            var y = state.Pop();
            var x = state.Pop();
            if (x.TryGetValueAsInteger(out var xInt) && y.TryGetValueAsInteger(out var yInt) &&
                z.TryGetValueAsInteger(out var zInt))
            {
                state.Proc.DreamMapManager.TryGetTurfAt((xInt, yInt), zInt, out var turf);
                state.Push(new DreamValue(turf));
            }
            else
            {
                state.Push(DreamValue.Null);
            }

            return null;
        }

        public static ProcStatus? Locate(DMProcState state) {
            if (!state.Pop().TryGetValueAsDreamObject(out var container))
            {
                state.Push(DreamValue.Null);
                return null;
            }

            DreamValue value = state.Pop();

            DreamList containerList;
            if (container != null && container.IsSubtypeOf(state.Proc.ObjectTree.Atom)) {
                container.GetVariable("contents").TryGetValueAsDreamList(out containerList);
            } else {
                containerList = container as DreamList;
            }

            if (value.TryGetValueAsString(out string refString)) {
                state.Push(state.DreamManager.LocateRef(refString));
            } else if (value.TryGetValueAsType(out var ancestor)) {
                if (containerList == null) {
                    state.Push(DreamValue.Null);

                    return null;
                }

                foreach (DreamValue containerItem in containerList.GetValues()) {
                    if (!containerItem.TryGetValueAsDreamObject(out DreamObject dmObject)) continue;

                    if (dmObject.IsSubtypeOf(ancestor)) {
                        state.Push(containerItem);

                        return null;
                    }
                }

                state.Push(DreamValue.Null);
            } else {
                if (containerList == null) {
                    state.Push(DreamValue.Null);

                    return null;
                }

                foreach (DreamValue containerItem in containerList.GetValues()) {
                    if (IsEqual(containerItem, value)) {
                        state.Push(containerItem);

                        return null;
                    }
                }

                state.Push(DreamValue.Null);
            }

            return null;
        }

        public static ProcStatus? PickWeighted(DMProcState state) {
            int count = state.ReadInt();

            (DreamValue Value, float CumulativeWeight)[] values = new (DreamValue, float)[count];
            float totalWeight = 0;
            for (int i = 0; i < count; i++) {
                DreamValue value = state.Pop();
                if (!state.Pop().TryGetValueAsFloat(out var weight))
                {
                    // Breaking change, no clue what weight BYOND is giving to non-nums
                    throw new Exception($"pick() weight '{weight}' is not a number");
                }

                totalWeight += weight;
                values[i] = (value, totalWeight);
            }

            double pick = state.DreamManager.Random.NextDouble() * totalWeight;
            for (int i = 0; i < values.Length; i++) {
                if (pick < values[i].CumulativeWeight) {
                    state.Push(values[i].Value);
                    break;
                }
            }

            return null;
        }

        public static ProcStatus? PickUnweighted(DMProcState state) {
            int count = state.ReadInt();

            DreamValue picked;
            if (count == 1) {
                DreamValue value = state.Pop();

                List<DreamValue> values;
                if (value.TryGetValueAsDreamList(out DreamList list)) {
                    values = list.GetValues();
                } else if (value.TryGetValueAsProcArguments(out var args)) {
                    values = args.GetAllArguments();
                } else {
                    state.Push(value);
                    return null;
                }

                if (values.Count == 0)
                    throw new Exception("pick() from empty list");

                picked = values[state.DreamManager.Random.Next(0, values.Count)];
            } else {
                int pickedIndex = state.DreamManager.Random.Next(0, count);

                picked = state.PopCount(count)[pickedIndex];
            }

            state.Push(picked);
            return null;
        }

        public static ProcStatus? Prob(DMProcState state) {
            DreamValue P = state.Pop();

            if (P.TryGetValueAsFloat(out float probability)) {
                int result = (state.DreamManager.Random.Next(0, 100) <= probability) ? 1 : 0;

                state.Push(new DreamValue(result));
            } else {
                state.Push(new DreamValue(0));
            }

            return null;
        }

        ///<summary>Right now this is used exclusively by addtext() calls, to concatenate its arguments together,
        ///but later it might make sense to have this be a simplification path for detected repetitive additions of strings,
        ///so as to slightly reduce the amount of re-allocation taking place.
        ///</summary>.
        public static ProcStatus? MassConcatenation(DMProcState state)
        {
            int count = state.ReadInt();
            if (count < 2) // One or zero arguments -- shouldn't really ever happen. addtext() compiletimes with <2 args and stringification should probably be a different opcode
            {
                Logger.Warning("addtext() called with " + count.ToString() + " arguments at runtime."); // TODO: tweak this warning if this ever gets used for other sorts of string concat
                state.Push(DreamValue.Null);
                return null;
            }
            int estimated_string_size = count * 10; // FIXME: We can do better with string size prediction here.
            StringBuilder builder = new StringBuilder(estimated_string_size); // An approximate guess at how big this string is going to be.
            foreach (DreamValue add in state.PopCount(count))
            {
                if (add.TryGetValueAsString(out var addStr))
                {
                    builder.Append(addStr);
                }
            }

            state.Push(new DreamValue(builder.ToString()));
            return null;
        }

        public static ProcStatus? IsSaved(DMProcState state) {
            DreamValue key = state.Pop();
            DreamValue owner = state.Pop();
            if (!key.TryGetValueAsString(out string property)) {
                throw new Exception($"Invalid var for issaved() call: {key}");
            }

            DreamObjectDefinition objectDefinition;
            if (owner.TryGetValueAsDreamObject(out var dreamObject)) {
                objectDefinition = dreamObject.ObjectDefinition;
            } else if (owner.TryGetValueAsType(out var type)) {
                objectDefinition = type.ObjectDefinition;
            } else {
                throw new Exception($"Invalid owner for issaved() call {owner}");
            }

            //TODO: Add support for var/const/ and var/tmp/ once those are properly in
            if (objectDefinition.GlobalVariables.ContainsKey(property)) {
                state.Push(new DreamValue(0));
            } else {
                state.Push(new DreamValue(1));
            }

            return null;
        }

        #endregion Others

        #region Helpers
        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        private static bool IsEqual(DreamValue first, DreamValue second) {
            switch (first.Type) {
                case DreamValue.DreamValueType.DreamObject: {
                    DreamObject firstValue = first.MustGetValueAsDreamObject();

                    switch (second.Type) {
                        case DreamValue.DreamValueType.DreamObject: return firstValue == second.MustGetValueAsDreamObject();
                        case DreamValue.DreamValueType.DreamType:
                        case DreamValue.DreamValueType.String:
                        case DreamValue.DreamValueType.Float: return false;
                    }

                    break;
                }
                case DreamValue.DreamValueType.Float: {
                    float firstValue = first.MustGetValueAsFloat();

                    switch (second.Type) {
                        case DreamValue.DreamValueType.Float: return firstValue == second.MustGetValueAsFloat();
                        case DreamValue.DreamValueType.DreamType:
                        case DreamValue.DreamValueType.DreamObject:
                        case DreamValue.DreamValueType.String: return false;
                    }

                    break;
                }
                case DreamValue.DreamValueType.String: {
                    string firstValue = first.MustGetValueAsString();

                    switch (second.Type) {
                        case DreamValue.DreamValueType.String: return firstValue == second.MustGetValueAsString();
                        case DreamValue.DreamValueType.DreamObject:
                        case DreamValue.DreamValueType.Float: return false;
                    }

                    break;
                }
                case DreamValue.DreamValueType.DreamType: {
                    var firstValue = first.MustGetValueAsType();

                    switch (second.Type) {
                        case DreamValue.DreamValueType.DreamType: return firstValue.Equals(second.MustGetValueAsType());
                        case DreamValue.DreamValueType.Float:
                        case DreamValue.DreamValueType.DreamObject:
                        case DreamValue.DreamValueType.String: return false;
                    }

                    break;
                }
                case DreamValue.DreamValueType.DreamProc: {
                    if (second.Type != DreamValue.DreamValueType.DreamProc)
                        return false;

                    return first.MustGetValueAsProc() == second.MustGetValueAsProc();
                }
                case DreamValue.DreamValueType.DreamResource: {
                    DreamResource firstValue = first.MustGetValueAsDreamResource();

                    switch (second.Type) {
                        case DreamValue.DreamValueType.DreamResource: return firstValue.ResourcePath == second.MustGetValueAsDreamResource().ResourcePath;
                        default: return false;
                    }
                }
            }

            throw new NotImplementedException("Equal comparison for " + first + " and " + second + " is not implemented");
        }

        private static bool IsEquivalent(DreamValue first, DreamValue second) {
            if(first.TryGetValueAsDreamObject(out var firstObject)) {
                if(firstObject?.ObjectDefinition?.MetaObject is not null) {
                    return firstObject.ObjectDefinition.MetaObject.OperatorEquivalent(first, second).IsTruthy();
                }
            }
            // Behaviour is otherwise equivalent (pun intended) to ==
            return IsEqual(first, second);
        }

        private static bool IsGreaterThan(DreamValue first, DreamValue second) {
            switch (first.Type) {
                case DreamValue.DreamValueType.Float when second.Type == DreamValue.DreamValueType.Float:
                    return first.MustGetValueAsFloat() > second.MustGetValueAsFloat();
                case DreamValue.DreamValueType.Float when second == DreamValue.Null:
                    return first.MustGetValueAsFloat() > 0;
                case DreamValue.DreamValueType.String when second.Type == DreamValue.DreamValueType.String:
                    return string.Compare(first.MustGetValueAsString(), second.MustGetValueAsString(), StringComparison.Ordinal) > 0;
                default: {
                    if (first == DreamValue.Null) {
                        if (second.Type == DreamValue.DreamValueType.Float) return 0 > second.MustGetValueAsFloat();
                        if (second.TryGetValueAsString(out var s)) return false;
                        if (second == DreamValue.Null) return false;
                    }
                    throw new Exception("Invalid greater than comparison on " + first + " and " + second);
                }
            }
        }

        private static bool IsLessThan(DreamValue first, DreamValue second) {
            switch (first.Type) {
                case DreamValue.DreamValueType.Float when second.Type == DreamValue.DreamValueType.Float:
                    return first.MustGetValueAsFloat() < second.MustGetValueAsFloat();
                case DreamValue.DreamValueType.Float when second == DreamValue.Null:
                    return first.MustGetValueAsFloat() < 0;
                case DreamValue.DreamValueType.String when second.Type == DreamValue.DreamValueType.String:
                    return string.Compare(first.MustGetValueAsString(), second.MustGetValueAsString(), StringComparison.Ordinal) < 0;
                default: {
                    if (first == DreamValue.Null) {
                        if (second.Type == DreamValue.DreamValueType.Float) return 0 < second.MustGetValueAsFloat();
                        if (second.TryGetValueAsString(out var s)) return s != "";
                        if (second == DreamValue.Null) return false;
                    }
                    throw new Exception("Invalid less than comparison between " + first + " and " + second);
                }
            }
        }

        private static DreamValue MultiplyValues(DreamValue first, DreamValue second) {
            if (first == DreamValue.Null || second == DreamValue.Null) {
                return new(0);
            } else if (first.TryGetValueAsDreamObject(out var firstObject)) {
                if (firstObject.ObjectDefinition.MetaObject == null)
                    throw new Exception("Invalid multiply operation on " + first + " and " + second);

                return firstObject.ObjectDefinition.MetaObject.OperatorMultiply(first, second);
            } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                return new(first.GetValueAsFloat() * second.GetValueAsFloat());
            } else {
                throw new Exception("Invalid multiply operation on " + first + " and " + second);
            }
        }

        private static DreamValue DivideValues(DreamValue first, DreamValue second) {
            if (first == DreamValue.Null) {
                return new(0);
            } else if (first.TryGetValueAsFloat(out var firstFloat) && second.TryGetValueAsFloat(out var secondFloat)) {
                if (secondFloat == 0) {
                    throw new Exception("Division by zero");
                }
                return new(firstFloat / secondFloat);
            } else {
                throw new Exception("Invalid divide operation on " + first + " and " + second);
            }
        }

        private static DreamValue BitXorValues(IDreamObjectTree objectTree, DreamValue first, DreamValue second) {
            if (first.TryGetValueAsDreamList(out var list)) {
                DreamList newList = objectTree.CreateList();
                List<DreamValue> values;

                if (second.TryGetValueAsDreamList(out DreamList secondList)) {
                    values = secondList.GetValues();
                } else {
                    values = new List<DreamValue>() { second };
                }

                foreach (DreamValue value in values) {
                    bool inFirstList = list.ContainsValue(value);
                    bool inSecondList = secondList.ContainsValue(value);

                    if (inFirstList ^ inSecondList) {
                        newList.AddValue(value);

                        DreamValue associatedValue = inFirstList ? list.GetValue(value) : secondList.GetValue(value);
                        if (associatedValue != DreamValue.Null) newList.SetValue(value, associatedValue);
                    }
                }

                return new DreamValue(newList);
            } else {
                return new DreamValue(first.MustGetValueAsInteger() ^ second.MustGetValueAsInteger());
            }
        }

        private static DreamValue ModulusValues(DreamValue first, DreamValue second) {
            if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                return new DreamValue(first.MustGetValueAsInteger() % second.MustGetValueAsInteger());
            } else {
                throw new Exception("Invalid modulus operation on " + first + " and " + second);
            }
        }

        private static DreamValue ModulusModulusValues(DreamValue first, DreamValue second) {
            if (first.TryGetValueAsFloat(out var firstFloat) && second.TryGetValueAsFloat(out var secondFloat)) {
                // BYOND docs say that A %% B is equivalent to B * fract(A/B)
                // BREAKING CHANGE: The floating point precision is slightly different between OD and BYOND, giving slightly different values
                var fraction = firstFloat / secondFloat;
                fraction -= MathF.Truncate(fraction);
                return new DreamValue(fraction * secondFloat);
            }
            throw new Exception("Invalid modulusmodulus operation on " + first + " and " + second);
        }
        #endregion Helpers
    }
}
