using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DMCompiler;
using DMCompiler.Bytecode;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Procs.Native;
using OpenDreamRuntime.Resources;
using OpenDreamShared.Dream;
using Robust.Shared.Random;
using Vector4 = Robust.Shared.Maths.Vector4;

namespace OpenDreamRuntime.Procs {
    internal static class DMOpcodeHandlers {
        #region Values
        public static ProcStatus PushReferenceValue(DMProcState state) {
            DreamReference reference = state.ReadReference();

            state.Push(state.GetReferenceValue(reference));
            return ProcStatus.Continue;
        }

        public static ProcStatus Assign(DMProcState state) {
            DreamReference reference = state.ReadReference();
            DreamValue value = state.Pop();

            state.AssignReference(reference, value);
            state.Push(value);
            return ProcStatus.Continue;
        }

        public static ProcStatus AssignInto(DMProcState state) {
            DreamReference reference = state.ReadReference();
            DreamValue value = state.Pop();
            DreamValue first = state.GetReferenceValue(reference);

            //TODO call operator:= for DreamObjects
            state.AssignReference(reference, value);
            state.Push(value);

            return ProcStatus.Continue;
        }


        public static ProcStatus CreateList(DMProcState state) {
            int size = state.ReadInt();
            var list = state.Proc.ObjectTree.CreateList(size);

            foreach (DreamValue value in state.PopCount(size)) {
                list.AddValue(value);
            }

            state.Push(new DreamValue(list));
            return ProcStatus.Continue;
        }

        public static ProcStatus CreateAssociativeList(DMProcState state) {
            int size = state.ReadInt();
            var list = state.Proc.ObjectTree.CreateList(size);

            ReadOnlySpan<DreamValue> popped = state.PopCount(size * 2);
            for (int i = 0; i < popped.Length; i += 2) {
                DreamValue key = popped[i];

                if (key.IsNull) {
                    list.AddValue(popped[i + 1]);
                } else {
                    list.SetValue(key, popped[i + 1], allowGrowth: true);
                }
            }

            state.Push(new DreamValue(list));
            return ProcStatus.Continue;
        }

        private static IDreamValueEnumerator GetContentsEnumerator(DreamObjectTree objectTree, AtomManager atomManager, DreamValue value, TreeEntry? filterType) {
            if (!value.TryGetValueAsDreamList(out var list)) {
                if (value.TryGetValueAsDreamObject(out var dreamObject)) {
                    if (dreamObject == null)
                        return new DreamValueArrayEnumerator(Array.Empty<DreamValue>());

                    if (dreamObject is DreamObjectAtom) {
                        list = dreamObject.GetVariable("contents").MustGetValueAsDreamList();
                    } else if (dreamObject is DreamObjectWorld) {
                        return new WorldContentsEnumerator(atomManager, filterType);
                    }
                }
            }

            if (list != null) {
                // world.contents has its own special enumerator to prevent the huge copy
                if (list is WorldContentsList)
                    return new WorldContentsEnumerator(atomManager, filterType);

                var values = list.GetValues().ToArray();

                return filterType == null
                    ? new DreamValueArrayEnumerator(values)
                    : new FilteredDreamValueArrayEnumerator(values, filterType);
            }
            // BYOND ignores all floats, strings, types, etc. here and just doesn't run the loop.
            return new DreamValueArrayEnumerator(Array.Empty<DreamValue>());
        }

        public static ProcStatus CreateListEnumerator(DMProcState state) {
            var enumerator = GetContentsEnumerator(state.Proc.ObjectTree, state.Proc.AtomManager, state.Pop(), null);

            state.EnumeratorStack.Push(enumerator);
            return ProcStatus.Continue;
        }

        public static ProcStatus CreateFilteredListEnumerator(DMProcState state) {
            var filterTypeId = state.ReadInt();
            var filterType = state.Proc.ObjectTree.GetTreeEntry(filterTypeId);
            var enumerator = GetContentsEnumerator(state.Proc.ObjectTree, state.Proc.AtomManager, state.Pop(), filterType);

            state.EnumeratorStack.Push(enumerator);
            return ProcStatus.Continue;
        }

        public static ProcStatus CreateTypeEnumerator(DMProcState state) {
            DreamValue typeValue = state.Pop();
            if (!typeValue.TryGetValueAsType(out var type)) {
                throw new Exception($"Cannot create a type enumerator with type {typeValue}");
            }

            if (type == state.Proc.ObjectTree.Client) {
                state.EnumeratorStack.Push(new DreamObjectEnumerator(state.DreamManager.Clients));
                return ProcStatus.Continue;
            }

            if (type.ObjectDefinition.IsSubtypeOf(state.Proc.ObjectTree.Atom)) {
                state.EnumeratorStack.Push(new WorldContentsEnumerator(state.Proc.AtomManager, type));
                return ProcStatus.Continue;
            }

            if (type.ObjectDefinition.IsSubtypeOf(state.Proc.ObjectTree.Datum)) {
                state.EnumeratorStack.Push(new DreamObjectEnumerator(state.DreamManager.Datums, type));
                return ProcStatus.Continue;
            }

            throw new Exception($"Type enumeration of {type} is not supported");
        }

        public static ProcStatus CreateRangeEnumerator(DMProcState state) {
            DreamValue step = state.Pop();
            DreamValue rangeEnd = state.Pop();
            DreamValue rangeStart = state.Pop();

            if (!step.TryGetValueAsFloat(out var stepValue))
                throw new Exception($"Invalid step {step}, must be a number");
            if (!rangeEnd.TryGetValueAsFloat(out var rangeEndValue))
                throw new Exception($"Invalid end {rangeEnd}, must be a number");
            if (!rangeStart.TryGetValueAsFloat(out var rangeStartValue))
                throw new Exception($"Invalid start {rangeStart}, must be a number");

            state.EnumeratorStack.Push(new DreamValueRangeEnumerator(rangeStartValue, rangeEndValue, stepValue));
            return ProcStatus.Continue;
        }

        public static ProcStatus CreateObject(DMProcState state) {
            var argumentInfo = state.ReadProcArguments();
            var val = state.Pop();
            if (!val.TryGetValueAsType(out var objectType)) {
                if (val.TryGetValueAsString(out var pathString)) {
                    if (!state.Proc.ObjectTree.TryGetTreeEntry(pathString, out objectType)) {
                        ThrowCannotCreateUnknownObject(val);
                    }
                } else {
                    ThrowCannotCreateObjectFromInvalid(val);
                }
            }

            var objectDef = objectType.ObjectDefinition;
            var proc = objectDef.GetProc("New");
            var arguments = state.PopProcArguments(proc, argumentInfo.Type, argumentInfo.StackSize);

            if (objectDef.IsSubtypeOf(state.Proc.ObjectTree.Turf)) {
                // Turfs are special. They're never created outside of map initialization
                // So instead this will replace an existing turf's type and return that same turf
                DreamValue loc = arguments.GetArgument(0);
                if (!loc.TryGetValueAsDreamObject<DreamObjectTurf>(out var turf))
                    ThrowInvalidTurfLoc(loc);

                state.Proc.DreamMapManager.SetTurf(turf, objectDef, arguments);

                state.Push(loc);
                return ProcStatus.Continue;
            }

            DreamObject newObject = state.Proc.ObjectTree.CreateObject(objectType);
            var s = newObject.InitProc(state.Thread, state.Usr, arguments);
            if (s is not null) {
                state.Thread.PushProcState(s);
                return ProcStatus.Called;
            }

            state.Push(new DreamValue(newObject));
            return ProcStatus.Continue;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidTurfLoc(DreamValue loc) {
            throw new Exception($"Invalid turf loc {loc}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowCannotCreateObjectFromInvalid(DreamValue val) {
            throw new Exception($"Cannot create object from invalid type {val}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowCannotCreateUnknownObject(DreamValue val) {
            throw new Exception($"Cannot create unknown object {val}");
        }

        public static ProcStatus DestroyEnumerator(DMProcState state) {
            state.EnumeratorStack.Pop();
            return ProcStatus.Continue;
        }

        public static ProcStatus Enumerate(DMProcState state) {
            IDreamValueEnumerator enumerator = state.EnumeratorStack.Peek();
            DreamReference outputRef = state.ReadReference();
            int jumpToIfFailure = state.ReadInt();

            if (!enumerator.Enumerate(state, outputRef))
                state.Jump(jumpToIfFailure);

            return ProcStatus.Continue;
        }

        public static ProcStatus EnumerateNoAssign(DMProcState state) {
            IDreamValueEnumerator enumerator = state.EnumeratorStack.Peek();
            int jumpToIfFailure = state.ReadInt();

            if (!enumerator.Enumerate(state, null))
                state.Jump(jumpToIfFailure);

            return ProcStatus.Continue;
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

        private static void ToRoman(ref StringBuilder formattedString, ReadOnlySpan<DreamValue> interps, int nextInterpIndex, bool upperCase) {
            char[] arr;
            if(upperCase) {
                arr = new char[] { 'M', 'D', 'C', 'L', 'X', 'V', 'I' };
            } else {
                arr = new char[] { 'm', 'd', 'c', 'l', 'x', 'v', 'i' };
            }

            int[] numArr = new int[] { 1000, 500, 100, 50, 10, 5, 1 };

            if(!interps[nextInterpIndex].TryGetValueAsFloat(out float value)) {
                return;
            }

            if(float.IsNaN(value)) {
                formattedString.Append('�'); //fancy-ish way to represent
                return;
            }

            if(value < 0) {
                formattedString.Append('-');
                value = MathF.Abs(value);
            }

            if (float.IsInfinity(value)) {
                formattedString.Append('∞');
                return;
            }

            var intValue = (int)value;
            var i = 0;

            while (intValue != 0) {
                if(intValue >= numArr[i]) {
                    intValue -= numArr[i];
                    formattedString.Append(arr[i]);
                } else {
                    i++;
                }
            }
        }
        public static ProcStatus FormatString(DMProcState state) {
            string unformattedString = state.ReadString();
            StringBuilder formattedString = new StringBuilder();

            int interpCount = state.ReadInt();

            StringFormatEncoder.FormatSuffix? postPrefix = null; // Prefix that needs the effects of a suffix

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
                        // TODO: use postPrefix for \th interpolation
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
                    case StringFormatEncoder.FormatSuffix.StringifyNoArticle: {
                        if (interps[nextInterpIndex].TryGetValueAsDreamObject<DreamObject>(out var dreamObject)) {
                            formattedString.Append(dreamObject.GetNameUnformatted());
                        } else if (interps[nextInterpIndex].TryGetValueAsString(out var interpStr)) {
                            formattedString.Append(StringFormatEncoder.RemoveFormatting(interpStr));
                        }

                        // NOTE probably should put this above the TryGetAsDreamObject function and continue if formatting has occured
                        if(postPrefix != null) { // Cursed Hack
                            switch (postPrefix) {
                                case StringFormatEncoder.FormatSuffix.LowerRoman:
                                    ToRoman(ref formattedString, interps, nextInterpIndex, false);
                                    break;
                                case StringFormatEncoder.FormatSuffix.UpperRoman:
                                    ToRoman(ref formattedString, interps, nextInterpIndex, true);
                                    break;
                            }

                            postPrefix = null;
                        }

                        //Things that aren't objects or strings just print nothing in this case
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
                    case StringFormatEncoder.FormatSuffix.LowerIndefiniteArticle: {
                        var interpValue = interps[nextInterpIndex];
                        string displayName;
                        bool isPlural = false;

                        if (interpValue.TryGetValueAsDreamObject<DreamObject>(out var dreamObject)) {
                            displayName = dreamObject.GetRawName();

                            // Aayy babe whats ya pronouns
                            if (dreamObject.TryGetVariable("gender", out var gender) &&
                                gender.TryGetValueAsString(out var genderStr)) {
                                // NOTE: In Byond, this part does not work if var/gender is not a native property of this object.
                                isPlural = (genderStr == "plural");
                            }
                        } else if (interpValue.TryGetValueAsString(out var interpStr)) {
                            displayName = interpStr;
                        } else {
                            break;
                        }

                        if (DreamObject.StringIsProper(displayName))
                            break; // Proper nouns don't need articles, I guess.

                        // saves some wordiness with the ternaries below
                        bool wasCapital = formatType == StringFormatEncoder.FormatSuffix.UpperIndefiniteArticle;

                        if (isPlural)
                            formattedString.Append(wasCapital ? "Some " : "some ");
                        else if (DreamObject.StringStartsWithVowel(displayName))
                            formattedString.Append(wasCapital ? "An " : "an ");
                        else
                            formattedString.Append(wasCapital ? "A " : "a ");

                        break;
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
                        var interp = interps[prevInterpIndex];
                        if (interp.TryGetValueAsInteger(out var ordinalNumber)) {

                            // For some mystical reason byond converts \th to integers
                            // This is slightly hacky but the only reliable way I know how to replace the number
                            // Need to call stringy to make sure its the right length to cut
                            formattedString.Length -= interps[prevInterpIndex].Stringify().Length;
                            formattedString.Append(ordinalNumber);
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
                        } else if (interp.IsNull) {
                            formattedString.Append("0th");
                        } else if(interp.TryGetValueAsString(out var interpString)) {
                            var lastIdx = formattedString.ToString().LastIndexOf(interpString);
                            if (lastIdx != -1) { // Can this even fail?
                                formattedString.Remove(lastIdx, interpString.Length);
                                formattedString.Append("0th");
                            }
                        } else if (interp.TryGetValueAsDreamObject(out var interpObj)) {
                            var typeStr = interpObj.ObjectDefinition.Type.ToString();
                            var lastIdx = formattedString.ToString().LastIndexOf(typeStr);
                            if (lastIdx != -1) { // Can this even fail?
                                formattedString.Remove(lastIdx, typeStr.Length);
                                formattedString.Append("0th");
                            }
                        } else {
                            // TODO: if the preceding expression value is not a float, it should be replaced with 0 (0th)
                            // we support this behavior for some non-floats but not all, so just append 0th anyways for now
                            formattedString.Append("0th");
                        }
                        continue;
                    case StringFormatEncoder.FormatSuffix.LowerRoman:
                        postPrefix = formatType;
                        continue;
                    case StringFormatEncoder.FormatSuffix.UpperRoman:
                        postPrefix = formatType;
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
            return ProcStatus.Continue;
        }

        public static ProcStatus Initial(DMProcState state) {
            DreamValue key = state.Pop();
            DreamValue owner = state.Pop();

            // number indices always perform a normal list access here
            if (key.TryGetValueAsInteger(out _)) {
                state.Push(state.GetIndex(owner, key));
                return ProcStatus.Continue;
            }

            if (!key.TryGetValueAsString(out string property)) {
                throw new Exception("Invalid var for initial() call: " + key);
            }

            if (owner.TryGetValueAsDreamObject(out DreamObject dreamObject)) {
                // Calling initial() on a null value just returns null
                if (dreamObject == null) {
                    state.Push(DreamValue.Null);
                    return ProcStatus.Continue;
                }

                state.Push(dreamObject.Initial(property));
                return ProcStatus.Continue;
            }

            DreamObjectDefinition objectDefinition;
            if (owner.TryGetValueAsDreamObject(out DreamObject dreamObject2)) {
                objectDefinition = dreamObject2.ObjectDefinition;
            } else if (owner.TryGetValueAsType(out var ownerType)) {
                objectDefinition = ownerType.ObjectDefinition;
            } else {
                throw new Exception($"Invalid owner for initial() call {owner}");
            }

            state.Push(objectDefinition.Variables[property]);
            return ProcStatus.Continue;
        }

        public static ProcStatus IsNull(DMProcState state) {
            DreamValue value = state.Pop();

            state.Push(new DreamValue((value.IsNull) ? 1 : 0));
            return ProcStatus.Continue;
        }

        public static ProcStatus IsInList(DMProcState state) {
            DreamValue listValue = state.Pop();
            DreamValue value = state.Pop();

            if (listValue.TryGetValueAsDreamObject(out var listObject) && listObject != null) {
                DreamList? list = listObject as DreamList;

                if (list == null) {
                    if (listObject is DreamObjectAtom or DreamObjectWorld) {
                        list = listObject.GetVariable("contents").MustGetValueAsDreamList();
                    } else {
                        // BYOND ignores all floats, strings, types, etc. here and just returns 0.
                        state.Push(DreamValue.False);
                    }
                }

                state.Push(new DreamValue(list.ContainsValue(value) ? 1 : 0));
            } else {
                state.Push(DreamValue.False);
            }

            return ProcStatus.Continue;
        }

        public static ProcStatus Pop(DMProcState state) {
            state.PopDrop();
            return ProcStatus.Continue;
        }

        public static ProcStatus PopReference(DMProcState state) {
            DreamReference reference = state.ReadReference();
            state.PopReference(reference);
            return ProcStatus.Continue;
        }

        public static ProcStatus PushFloat(DMProcState state) {
            float value = state.ReadFloat();

            state.Push(new DreamValue(value));
            return ProcStatus.Continue;
        }

        public static ProcStatus PushNull(DMProcState state) {
            state.Push(DreamValue.Null);
            return ProcStatus.Continue;
        }

        public static ProcStatus PushType(DMProcState state) {
            int typeId = state.ReadInt();
            var type = state.Proc.ObjectTree.Types[typeId];

            state.Push(new DreamValue(type));
            return ProcStatus.Continue;
        }

        public static ProcStatus PushProc(DMProcState state) {
            int procId = state.ReadInt();

            state.Push(new DreamValue(state.Proc.ObjectTree.Procs[procId]));
            return ProcStatus.Continue;
        }

        public static ProcStatus PushResource(DMProcState state) {
            string resourcePath = state.ReadString();

            state.Push(new DreamValue(state.Proc.DreamResourceManager.LoadResource(resourcePath)));
            return ProcStatus.Continue;
        }

        public static ProcStatus PushString(DMProcState state) {
            state.Push(new DreamValue(state.ReadString()));
            return ProcStatus.Continue;
        }

        public static ProcStatus PushGlobalVars(DMProcState state) {
            state.Push(new DreamValue(new DreamGlobalVars(state.Proc.ObjectTree.List.ObjectDefinition)));
            return ProcStatus.Continue;
        }
        #endregion Values

        #region Math
        public static ProcStatus Add(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();
            DreamValue output = default;

            if (first.IsNull) {
                output = second;
            } else if (first.TryGetValueAsDreamResource(out _) || first.TryGetValueAsDreamObject<DreamObjectIcon>(out _)) {
                output = IconOperationAdd(state, first, second);
            } else if (first.TryGetValueAsDreamObject(out var firstDreamObject)) {
                output = firstDreamObject!.OperatorAdd(second);
            } else if (first.TryGetValueAsType(out _) || first.TryGetValueAsProc(out _)) {
                output = default; // Always errors
            } else if (second.IsNull) {
                output = first;
            } else switch (first.Type) {
                case DreamValue.DreamValueType.Float: {
                    float firstFloat = first.MustGetValueAsFloat();

                    if (second.Type == DreamValue.DreamValueType.Float) {
                        output = new DreamValue(firstFloat + second.MustGetValueAsFloat());
                    }
                    break;
                }
                case DreamValue.DreamValueType.String when second.Type == DreamValue.DreamValueType.String:
                    output = new DreamValue(first.MustGetValueAsString() + second.MustGetValueAsString());
                    break;
            }

            if (output.Type != 0) {
                state.Push(output);
            } else {
                ThrowInvalidAddOperation(first, second);
            }

            return ProcStatus.Continue;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidAddOperation(DreamValue first, DreamValue second) {
            throw new Exception("Invalid add operation on " + first + " and " + second);
        }

        public static ProcStatus Append(DMProcState state) {
            DreamReference reference = state.ReadReference();
            DreamValue second = state.Pop();
            DreamValue first = state.GetReferenceValue(reference, peek: true);

            DreamValue result;
            if (first.TryGetValueAsDreamResource(out _) || first.TryGetValueAsDreamObject<DreamObjectIcon>(out _)) {
                result = IconOperationAdd(state, first, second);
            } else if (first.TryGetValueAsDreamObject(out var firstObj)) {
                if (firstObj != null) {
                    state.PopReference(reference);
                    state.Push(firstObj.OperatorAppend(second));

                    return ProcStatus.Continue;
                } else {
                    result = second;
                }
            } else if (!second.IsNull) {
                switch (first.Type) {
                    case DreamValue.DreamValueType.Float when second.Type == DreamValue.DreamValueType.Float:
                        result = new DreamValue(first.MustGetValueAsFloat() + second.MustGetValueAsFloat());
                        break;
                    case DreamValue.DreamValueType.String when second.Type == DreamValue.DreamValueType.String:
                        result = new DreamValue(first.MustGetValueAsString() + second.MustGetValueAsString());
                        break;
                    default:
                        throw new Exception("Invalid append operation on " + first + " and " + second);
                }
            } else {
                result = first;
            }

            state.AssignReference(reference, result);
            state.Push(result);
            return ProcStatus.Continue;
        }

        public static ProcStatus Increment(DMProcState state) {
            DreamReference reference = state.ReadReference();
            DreamValue value = state.GetReferenceValue(reference, peek: true);

            //If it's not a number, it turns into 1
            state.AssignReference(reference, new(value.UnsafeGetValueAsFloat() + 1));

            state.Push(value);
            return ProcStatus.Continue;
        }

        public static ProcStatus Decrement(DMProcState state) {
            DreamReference reference = state.ReadReference();
            DreamValue value = state.GetReferenceValue(reference, peek: true);

            //If it's not a number, it turns into -1
            state.AssignReference(reference, new(value.UnsafeGetValueAsFloat() - 1));

            state.Push(value);
            return ProcStatus.Continue;
        }

        public static ProcStatus BitAnd(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            if (!first.IsDreamObject<DreamList>() && !first.IsNull && !second.IsNull) {
                state.Push(new DreamValue(first.MustGetValueAsInteger() & second.MustGetValueAsInteger()));
            } else if (first.TryGetValueAsDreamList(out var list)) {
                DreamList newList = state.Proc.ObjectTree.CreateList();

                if (second.TryGetValueAsDreamList(out var secondList)) {
                    int len = list.GetLength();

                    for (int i = 1; i <= len; i++) {
                        DreamValue value = list.GetValue(new DreamValue(i));

                        if (secondList.ContainsValue(value)) {
                            DreamValue associativeValue = list.GetValue(value);

                            newList.AddValue(value);
                            if (!associativeValue.IsNull) newList.SetValue(value, associativeValue);
                        }
                    }
                } else {
                    int len = list.GetLength();

                    for (int i = 1; i <= len; i++) {
                        DreamValue value = list.GetValue(new DreamValue(i));

                        if (value == second) {
                            DreamValue associativeValue = list.GetValue(value);

                            newList.AddValue(value);
                            if (!associativeValue.IsNull) newList.SetValue(value, associativeValue);
                        }
                    }
                }

                state.Push(new DreamValue(newList));
            } else {
                state.Push(new DreamValue(0));
            }

            return ProcStatus.Continue;
        }

        public static ProcStatus BitNot(DMProcState state) {
            var input = state.Pop();
            if (input.TryGetValueAsInteger(out var value)) {
                state.Push(new DreamValue((~value) & 0xFFFFFF));
            } else {
                if (input.TryGetValueAsDreamObject<DreamObjectMatrix>(out _)) { // TODO ~ on /matrix
                    throw new NotImplementedException("/matrix does not support the '~' operator yet");
                }

                state.Push(new DreamValue(16777215)); // 2^24 - 1
            }

            return ProcStatus.Continue;
        }

        public static ProcStatus BitOr(DMProcState state) {                        // x | y
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            if (first.Type == DreamValue.DreamValueType.DreamObject) {              // Object | y
                if (!first.IsNull) {
                    state.Push(first.MustGetValueAsDreamObject()!.OperatorOr(second));
                } else {
                    state.Push(DreamValue.Null);
                }
            } else if (!second.IsNull) {                                      // Non-Object | y
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

            return ProcStatus.Continue;
        }

        public static ProcStatus BitShiftLeft(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            switch (first.Type) {
                case DreamValue.DreamValueType.DreamObject when first.IsNull:
                    state.Push(new DreamValue(0));
                    break;
                case DreamValue.DreamValueType.Float when second.Type == DreamValue.DreamValueType.Float:
                    state.Push(new DreamValue(first.MustGetValueAsInteger() << second.MustGetValueAsInteger()));
                    break;
                default:
                    throw new Exception($"Invalid bit shift left operation on {first} and {second}");
            }

            return ProcStatus.Continue;
        }


        public static ProcStatus BitShiftLeftReference(DMProcState state) {
            DreamReference reference = state.ReadReference();
            DreamValue second = state.Pop();
            DreamValue first = state.GetReferenceValue(reference, peek: true);
            DreamValue result;
            switch (first.Type) {
                case DreamValue.DreamValueType.DreamObject when first.IsNull:
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
            return ProcStatus.Continue;
        }

        public static ProcStatus BitShiftRight(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            if (first.IsNull) {
                state.Push(new DreamValue(0));
            } else if (first.Type == DreamValue.DreamValueType.Float && second.Type == DreamValue.DreamValueType.Float) {
                state.Push(new DreamValue(first.MustGetValueAsInteger() >> second.MustGetValueAsInteger()));
            } else {
                throw new Exception($"Invalid bit shift right operation on {first} and {second}");
            }

            return ProcStatus.Continue;
        }

        public static ProcStatus BitShiftRightReference(DMProcState state) {
            DreamReference reference = state.ReadReference();
            DreamValue second = state.Pop();
            DreamValue first = state.GetReferenceValue(reference, peek: true);
            DreamValue result;
            switch (first.Type) {
                case DreamValue.DreamValueType.DreamObject when first.IsNull:
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
            return ProcStatus.Continue;
        }

        public static ProcStatus BitXor(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            state.Push(BitXorValues(state.Proc.ObjectTree, first, second));
            return ProcStatus.Continue;
        }

        public static ProcStatus BitXorReference(DMProcState state) {
            DreamValue second = state.Pop();
            DreamReference reference = state.ReadReference();
            DreamValue first = state.GetReferenceValue(reference, peek: true);
            DreamValue result = BitXorValues(state.Proc.ObjectTree, first, second);

            state.AssignReference(reference, result);
            state.Push(result);
            return ProcStatus.Continue;
        }

        public static ProcStatus BooleanAnd(DMProcState state) {
            DreamValue a = state.Pop();
            int jumpPosition = state.ReadInt();

            if (!a.IsTruthy()) {
                state.Push(a);
                state.Jump(jumpPosition);
            }

            return ProcStatus.Continue;
        }

        public static ProcStatus BooleanNot(DMProcState state) {
            DreamValue value = state.Pop();

            state.Push(new DreamValue(value.IsTruthy() ? 0 : 1));
            return ProcStatus.Continue;
        }

        public static ProcStatus BooleanOr(DMProcState state) {
            DreamValue a = state.Pop();
            int jumpPosition = state.ReadInt();

            if (a.IsTruthy()) {
                state.Push(a);
                state.Jump(jumpPosition);
            }
            return ProcStatus.Continue;
        }

        public static ProcStatus Combine(DMProcState state) {
            DreamReference reference = state.ReadReference();
            DreamValue second = state.Pop();
            DreamValue first = state.GetReferenceValue(reference, peek: true);

            DreamValue result;
            if (first.TryGetValueAsDreamObject(out var firstObj)) {
                if (firstObj != null) {
                    state.PopReference(reference);
                    state.Push(firstObj.OperatorCombine(second));

                    return ProcStatus.Continue;
                } else {
                    result = second;
                }
            } else if (!second.IsNull) {
                if (first.TryGetValueAsInteger(out var firstInt) && second.TryGetValueAsInteger(out var secondInt)) {
                    result = new DreamValue(firstInt | secondInt);
                } else if (first.IsNull) {
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
            return ProcStatus.Continue;
        }

        public static ProcStatus Divide(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            state.Push(DivideValues(first, second));
            return ProcStatus.Continue;
        }

        public static ProcStatus DivideReference(DMProcState state) {
            DreamReference reference = state.ReadReference();
            DreamValue second = state.Pop();
            DreamValue first = state.GetReferenceValue(reference, peek: true);
            DreamValue result = DivideValues(first, second);

            state.AssignReference(reference, result);
            state.Push(result);
            return ProcStatus.Continue;
        }

        public static ProcStatus Mask(DMProcState state) {
            DreamReference reference = state.ReadReference();
            DreamValue second = state.Pop();
            DreamValue first = state.GetReferenceValue(reference, peek: true);

            DreamValue result;
            switch (first.Type) {
                case DreamValue.DreamValueType.DreamObject when !first.IsNull: {
                    state.PopReference(reference);
                    state.Push(first.MustGetValueAsDreamObject()!.OperatorMask(second));

                    return ProcStatus.Continue;
                }
                case DreamValue.DreamValueType.DreamObject: // null
                    result = new DreamValue(0);
                    break;
                case DreamValue.DreamValueType.Float when second.Type == DreamValue.DreamValueType.Float:
                    result = new DreamValue(first.MustGetValueAsInteger() & second.MustGetValueAsInteger());
                    break;
                default:
                    throw new Exception("Invalid mask operation on " + first + " and " + second);
            }

            state.AssignReference(reference, result);
            state.Push(result);
            return ProcStatus.Continue;
        }

        public static ProcStatus Modulus(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();
            state.Push(ModulusValues(first, second));
            return ProcStatus.Continue;
        }

        public static ProcStatus ModulusModulus(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            state.Push(ModulusModulusValues(first, second));

            return ProcStatus.Continue;
        }

        public static ProcStatus ModulusReference(DMProcState state) {
            DreamValue second = state.Pop();
            DreamReference reference = state.ReadReference();
            DreamValue first = state.GetReferenceValue(reference, peek: true);
            DreamValue result = ModulusValues(first, second);

            state.AssignReference(reference, result);
            state.Push(result);
            return ProcStatus.Continue;
        }

        public static ProcStatus ModulusModulusReference(DMProcState state) {
            DreamValue second = state.Pop();
            DreamReference reference = state.ReadReference();
            DreamValue first = state.GetReferenceValue(reference, peek: true);
            DreamValue result = ModulusModulusValues(first, second);

            state.AssignReference(reference, result);
            state.Push(result);
            return ProcStatus.Continue;
        }

        public static ProcStatus Multiply(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            state.Push(MultiplyValues(first, second));
            return ProcStatus.Continue;
        }

        public static ProcStatus MultiplyReference(DMProcState state) {
            DreamReference reference = state.ReadReference();
            DreamValue second = state.Pop();
            DreamValue first = state.GetReferenceValue(reference, peek: true);
            DreamValue result = MultiplyValues(first, second);

            state.AssignReference(reference, result);
            state.Push(result);
            return ProcStatus.Continue;
        }

        public static ProcStatus Negate(DMProcState state) {
            float value = state.Pop().UnsafeGetValueAsFloat();

            state.Push(new DreamValue(-value));

            return ProcStatus.Continue;
        }

        public static ProcStatus Power(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            if (!first.TryGetValueAsFloat(out var floatFirst) && !first.IsNull)
                throw new Exception($"Invalid power operation on {first} and {second}");

            var floatSecond = second.UnsafeGetValueAsFloat(); // Non-numbers treated as 0 here

            state.Push(new DreamValue(MathF.Pow(floatFirst, floatSecond)));
            return ProcStatus.Continue;
        }

        public static ProcStatus Remove(DMProcState state) {
            DreamReference reference = state.ReadReference();
            DreamValue second = state.Pop();
            DreamValue first = state.GetReferenceValue(reference, peek: true);

            DreamValue result;
            switch (first.Type) {
                case DreamValue.DreamValueType.DreamObject when !first.IsNull:
                    state.PopReference(reference);
                    state.Push(first.MustGetValueAsDreamObject()!.OperatorRemove(second));

                    return ProcStatus.Continue;
                case DreamValue.DreamValueType.DreamObject when first.IsNull: // null is treated as 0
                case DreamValue.DreamValueType.Float:
                    if (second.Type != DreamValue.DreamValueType.Float && !second.IsNull)
                        goto default;

                    // UnsafeGetValueAsFloat() so that null is treated as 0.
                    result = new DreamValue(first.UnsafeGetValueAsFloat() - second.UnsafeGetValueAsFloat());
                    break;
                default:
                    throw new Exception($"Invalid remove operation on {first} and {second}");
            }

            state.AssignReference(reference, result);
            state.Push(result);
            return ProcStatus.Continue;
        }

        public static ProcStatus Subtract(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();
            DreamValue output = default;

            if (first.TryGetValueAsFloat(out var firstFloat) || first.IsNull) {
                if (second.TryGetValueAsFloat(out var secondFloat) || second.IsNull) {
                    output = new(firstFloat - secondFloat);
                }
            } else if (first.TryGetValueAsDreamObject(out var firstObject)) {
                output = firstObject!.OperatorSubtract(second); // Not null because of the above first.IsNull check
            }

            if (output.Type != 0) {
                state.Push(output);
            } else {
                throw new Exception($"Invalid subtract operation on {first} and {second}");
            }

            return ProcStatus.Continue;
        }
        #endregion Math

        #region Comparisons
        public static ProcStatus CompareEquals(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            state.Push(new DreamValue(IsEqual(first, second) ? 1 : 0));
            return ProcStatus.Continue;
        }

        public static ProcStatus CompareEquivalent(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            state.Push(new DreamValue(IsEquivalent(first, second) ? 1 : 0));
            return ProcStatus.Continue;
        }

        public static ProcStatus CompareGreaterThan(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            state.Push(new DreamValue(IsGreaterThan(first, second) ? 1 : 0));
            return ProcStatus.Continue;
        }

        public static ProcStatus CompareGreaterThanOrEqual(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();
            DreamValue result;

            if (first.TryGetValueAsFloat(out float lhs) && lhs == 0.0 && second.IsNull) result = new DreamValue(1);
            else if (first.IsNull && second.TryGetValueAsFloat(out float rhs) && rhs == 0.0) result = new DreamValue(1);
            else if (first.IsNull && second.TryGetValueAsString(out var s) && s == "") result = new DreamValue(1);
            else result = new DreamValue((IsEqual(first, second) || IsGreaterThan(first, second)) ? 1 : 0);

            state.Push(result);
            return ProcStatus.Continue;
        }

        public static ProcStatus CompareLessThan(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            state.Push(new DreamValue(IsLessThan(first, second) ? 1 : 0));
            return ProcStatus.Continue;
        }

        public static ProcStatus CompareLessThanOrEqual(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();
            DreamValue result;

            if (first.TryGetValueAsFloat(out float lhs) && lhs == 0.0 && second.IsNull) result = new DreamValue(1);
            else if (first.IsNull && second.TryGetValueAsFloat(out float rhs) && rhs == 0.0) result = new DreamValue(1);
            else if (first.IsNull && second.TryGetValueAsString(out var s) && s == "") result = new DreamValue(1);
            else result = new DreamValue((IsEqual(first, second) || IsLessThan(first, second)) ? 1 : 0);

            state.Push(result);
            return ProcStatus.Continue;
        }

        public static ProcStatus CompareNotEquals(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            state.Push(new DreamValue(IsEqual(first, second) ? 0 : 1));
            return ProcStatus.Continue;
        }

        public static ProcStatus CompareNotEquivalent(DMProcState state) {
            DreamValue second = state.Pop();
            DreamValue first = state.Pop();

            state.Push(new DreamValue(IsEquivalent(first, second) ? 0 : 1));
            return ProcStatus.Continue;
        }

        public static ProcStatus IsInRange(DMProcState state) {
            DreamValue end = state.Pop();
            DreamValue start = state.Pop();
            DreamValue var = state.Pop();

            if (var.Type != DreamValue.DreamValueType.Float) var = new DreamValue(0f);
            if (start.Type != DreamValue.DreamValueType.Float) start = new DreamValue(0f);
            if (end.Type != DreamValue.DreamValueType.Float) end = new DreamValue(0f);

            bool inRange = (IsEqual(start, var) || IsLessThan(start, var)) && (IsEqual(var, end) || IsLessThan(var, end));
            state.Push(new DreamValue(inRange ? 1 : 0));
            return ProcStatus.Continue;
        }

        public static ProcStatus IsType(DMProcState state) {
            DreamValue typeValue = state.Pop();
            DreamValue value = state.Pop();
            TreeEntry type;

            if (typeValue.TryGetValueAsDreamObject(out var typeObject)) {
                if (typeObject == null) {
                    state.Push(new DreamValue(0));
                    return ProcStatus.Continue;
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

            return ProcStatus.Continue;
        }
        #endregion Comparisons

        #region Flow
        public static ProcStatus Call(DMProcState state) {
            DreamReference procRef = state.ReadReference();
            var argumentInfo = state.ReadProcArguments();

            DreamObject instance;
            DreamProc proc;
            switch (procRef.Type) {
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
                        return ProcStatus.Continue;
                    }

                    break;
                }
                case DMReference.Type.GlobalProc: {
                    instance = null;
                    proc = state.Proc.ObjectTree.Procs[procRef.Value];

                    break;
                }
                case DMReference.Type.SrcProc: {
                    instance = state.Instance;
                    if (!instance.TryGetProc(state.ResolveString(procRef.Value), out proc))
                        throw new Exception($"Type {instance.ObjectDefinition.Type} has no proc called \"{state.ResolveString(procRef.Value)}\"");

                    break;
                }
                default: throw new Exception($"Invalid proc reference type {procRef.Type}");
            }

            DreamProcArguments arguments = state.PopProcArguments(proc, argumentInfo.Type, argumentInfo.StackSize);

            return state.Call(proc, instance, arguments);
        }

        public static ProcStatus CallStatement(DMProcState state) {
            var argumentsInfo = state.ReadProcArguments();
            DreamValue source = state.Pop();

            switch (source.Type) {
                case DreamValue.DreamValueType.DreamObject: {
                    DreamObject? dreamObject = source.MustGetValueAsDreamObject();
                    DreamValue procId = state.Pop();
                    DreamProc? proc = null;

                    switch (procId.Type) {
                        case DreamValue.DreamValueType.String:
                            proc = dreamObject?.GetProc(procId.MustGetValueAsString());
                            break;
                        case DreamValue.DreamValueType.DreamProc: {
                            proc = procId.MustGetValueAsProc();
                            break;
                        }
                    }

                    if (proc != null) {
                        DreamProcArguments arguments = state.PopProcArguments(proc, argumentsInfo.Type, argumentsInfo.StackSize);

                        return state.Call(proc, dreamObject, arguments);
                    }

                    throw new Exception($"Invalid proc ({procId} on {dreamObject})");
                }
                case DreamValue.DreamValueType.DreamProc: {
                    var proc = source.MustGetValueAsProc();

                    DreamProcArguments arguments = state.PopProcArguments(proc, argumentsInfo.Type, argumentsInfo.StackSize);

                    return state.Call(proc, state.Instance, arguments);
                }
                case DreamValue.DreamValueType.String:
                    unsafe {
                        if(!source.TryGetValueAsString(out var dllName))
                            throw new Exception($"{source} is not a valid DLL");

                        var popProc = state.Pop();
                        if(!popProc.TryGetValueAsString(out var procName)) {
                            throw new Exception($"{popProc} is not a valid proc name");
                        }

                        DreamProcArguments arguments = state.PopProcArguments(null, argumentsInfo.Type, argumentsInfo.StackSize);

                        // DLL Invoke
                        var entryPoint = DllHelper.ResolveDllTarget(state.Proc.DreamResourceManager, dllName, procName);

                        Span<nint> argV = stackalloc nint[arguments.Count];
                        argV.Fill(0);
                        try {
                            for (var i = 0; i < argV.Length; i++) {
                                var arg = arguments.GetArgument(i).Stringify();
                                argV[i] = Marshal.StringToCoTaskMemUTF8(arg);
                            }

                            byte* ret;
                            if (arguments.Count > 0) {
                                fixed (nint* ptr = &argV[0]) {
                                    ret = entryPoint(arguments.Count, (byte**)ptr);
                                }
                            } else {
                                ret = entryPoint(0, (byte**)0);
                            }

                            if (ret == null) {
                                state.Push(DreamValue.Null);
                                return ProcStatus.Continue;
                            }

                            var retString = Marshal.PtrToStringUTF8((nint)ret);
                            state.Push(new DreamValue(retString));
                            return ProcStatus.Continue;
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

        public static ProcStatus Error(DMProcState state) {
            throw new Exception("Reached an error opcode");
        }

        public static ProcStatus Invalid(DMProcState state) {
            throw new Exception("Reached an invalid opcode!");
        }

        public static ProcStatus Jump(DMProcState state) {
            int position = state.ReadInt();

            state.Jump(position);
            return ProcStatus.Continue;
        }

        public static ProcStatus JumpIfFalse(DMProcState state) {
            int position = state.ReadInt();
            DreamValue value = state.Pop();

            if (!value.IsTruthy()) {
                state.Jump(position);
            }

            return ProcStatus.Continue;
        }

        public static ProcStatus JumpIfTrue(DMProcState state) {
            int position = state.ReadInt();
            DreamValue value = state.Pop();

            if (value.IsTruthy()) {
                state.Jump(position);
            }

            return ProcStatus.Continue;
        }
        public static ProcStatus JumpIfNull(DMProcState state) {
            int position = state.ReadInt();

            if (state.Peek().IsNull) {
                state.PopDrop();
                state.Jump(position);
            }

            return ProcStatus.Continue;
        }

        public static ProcStatus JumpIfNullNoPop(DMProcState state) {
            int position = state.ReadInt();

            if (state.Peek().IsNull) {
                state.Jump(position);
            }

            return ProcStatus.Continue;
        }

        public static ProcStatus JumpIfNullDereference(DMProcState state) {
            DreamReference reference = state.ReadReference();
            int position = state.ReadInt();

            if (state.IsNullDereference(reference)) {
                state.Push(DreamValue.Null);
                state.Jump(position);
            }

            return ProcStatus.Continue;
        }

        public static ProcStatus JumpIfTrueReference(DMProcState state) {
            DreamReference reference = state.ReadReference();
            int position = state.ReadInt();

            var value = state.GetReferenceValue(reference, true);

            if (value.IsTruthy()) {
                state.PopReference(reference);
                state.Push(value);
                state.Jump(position);
            }

            return ProcStatus.Continue;
        }

        public static ProcStatus JumpIfFalseReference(DMProcState state) {
            DreamReference reference = state.ReadReference();
            int position = state.ReadInt();

            var value = state.GetReferenceValue(reference, true);

            if (!value.IsTruthy()) {
                state.PopReference(reference);
                state.Push(value);
                state.Jump(position);
            }

            return ProcStatus.Continue;
        }

        public static ProcStatus DereferenceField(DMProcState state) {
            string name = state.ReadString();
            DreamValue owner = state.Pop();

            state.Push(state.DereferenceField(owner, name));
            return ProcStatus.Continue;
        }

        public static ProcStatus Return(DMProcState state) {
            state.SetReturn(state.Pop());
            return ProcStatus.Returned;
        }

        public static ProcStatus Throw(DMProcState state) {
            DreamValue value = state.Pop();

            throw new DMThrowException(value);
        }

        public static ProcStatus Try(DMProcState state) {
            state.StartTryBlock(state.ReadInt(), state.ReadReference().Value);
            return ProcStatus.Continue;
        }

        public static ProcStatus TryNoValue(DMProcState state) {
            state.StartTryBlock(state.ReadInt());
            return ProcStatus.Continue;
        }

        public static ProcStatus EndTry(DMProcState state) {
            state.EndTryBlock();
            return ProcStatus.Continue;
        }

        public static ProcStatus Sin(DMProcState state) {
            float x = state.Pop().UnsafeGetValueAsFloat();
            float result = SharedOperations.Sin(x);

            state.Push(new DreamValue(result));
            return ProcStatus.Continue;
        }

        public static ProcStatus Cos(DMProcState state) {
            float x = state.Pop().UnsafeGetValueAsFloat();
            float result = SharedOperations.Cos(x);

            state.Push(new DreamValue(result));
            return ProcStatus.Continue;
        }

        public static ProcStatus Tan(DMProcState state) {
            float x = state.Pop().UnsafeGetValueAsFloat();
            float result = SharedOperations.Tan(x);

            state.Push(new DreamValue(result));
            return ProcStatus.Continue;
        }

        public static ProcStatus ArcSin(DMProcState state) {
            float x = state.Pop().UnsafeGetValueAsFloat();
            float result = SharedOperations.ArcSin(x);

            state.Push(new DreamValue(result));
            return ProcStatus.Continue;
        }

        public static ProcStatus ArcCos(DMProcState state) {
            float x = state.Pop().UnsafeGetValueAsFloat();
            float result = SharedOperations.ArcCos(x);

            state.Push(new DreamValue(result));
            return ProcStatus.Continue;
        }

        public static ProcStatus ArcTan(DMProcState state) {
            float a = state.Pop().UnsafeGetValueAsFloat();
            float result = SharedOperations.ArcTan(a);

            state.Push(new DreamValue(result));
            return ProcStatus.Continue;
        }

        public static ProcStatus ArcTan2(DMProcState state) {
            float y = state.Pop().UnsafeGetValueAsFloat();
            float x = state.Pop().UnsafeGetValueAsFloat();
            float result = SharedOperations.ArcTan(x, y);

            state.Push(new DreamValue(result));
            return ProcStatus.Continue;
        }

        public static ProcStatus Sqrt(DMProcState state) {
            float a = state.Pop().UnsafeGetValueAsFloat();
            float result = SharedOperations.Sqrt(a);

            state.Push(new DreamValue(result));
            return ProcStatus.Continue;
        }

        public static ProcStatus Log(DMProcState state) {
            float baseValue = state.Pop().UnsafeGetValueAsFloat();
            float value = state.Pop().UnsafeGetValueAsFloat();
            float result = SharedOperations.Log(value, baseValue);

            state.Push(new DreamValue(result));
            return ProcStatus.Continue;
        }

        public static ProcStatus LogE(DMProcState state) {
            float y = state.Pop().UnsafeGetValueAsFloat();
            float result = SharedOperations.Log(y);

            state.Push(new DreamValue(result));
            return ProcStatus.Continue;
        }

        public static ProcStatus Abs(DMProcState state) {
            float a = state.Pop().UnsafeGetValueAsFloat();
            float result = SharedOperations.Abs(a);

            state.Push(new DreamValue(result));
            return ProcStatus.Continue;
        }

        public static ProcStatus SwitchCase(DMProcState state) {
            int casePosition = state.ReadInt();
            DreamValue testValue = state.Pop();
            DreamValue value = state.Pop();

            if (IsEqual(value, testValue)) {
                state.Jump(casePosition);
            } else {
                state.Push(value);
            }

            return ProcStatus.Continue;
        }

        public static ProcStatus SwitchCaseRange(DMProcState state) {
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

            return ProcStatus.Continue;
        }

        //Copy & run the interpreter in a new thread
        //Jump the current thread to after the spawn's code
        public static ProcStatus Spawn(DMProcState state) {
            int jumpTo = state.ReadInt();
            state.Pop().TryGetValueAsFloat(out var delay);

            // TODO: It'd be nicer if we could use something such as DreamThread.Spawn here
            // and have state.Spawn return a ProcState instead
            DreamThread newContext = state.Spawn();

            //Negative delays mean the spawned code runs immediately
            if (delay < 0) {
                newContext.Resume();
                // TODO: Does the rest of the proc get scheduled?
                // Does the value of the delay mean anything?
            } else {
                async void Wait() {
                    await state.ProcScheduler.CreateDelay(delay);
                    newContext.Resume();
                }

                Wait();
            }

            state.Jump(jumpTo);
            return ProcStatus.Continue;
        }

        public static ProcStatus DebuggerBreakpoint(DMProcState state) {
            return state.DebugManager.HandleBreakpoint(state);
        }
        #endregion Flow

        #region Builtins
        public static ProcStatus GetStep(DMProcState state) {
            var d = state.Pop();
            var l = state.Pop();

            if (!l.TryGetValueAsDreamObject<DreamObjectAtom>(out var loc)) {
                state.Push(DreamValue.Null);
                return ProcStatus.Continue;
            }

            var dir = d.MustGetValueAsInteger();

            state.Push(new(DreamProcNativeHelpers.GetStep(state.Proc.AtomManager, state.Proc.DreamMapManager, loc, (AtomDirection)dir)));
            return ProcStatus.Continue;
        }

        public static ProcStatus Length(DMProcState state) {
            var o = state.Pop();

            state.Push(DreamProcNativeRoot._length(o, true));
            return ProcStatus.Continue;
        }

        public static ProcStatus GetDir(DMProcState state) {
            var loc2R = state.Pop();
            var loc1R = state.Pop();

            if (!loc1R.TryGetValueAsDreamObject<DreamObjectAtom>(out var loc1)) {
                state.Push(new DreamValue(0));
                return ProcStatus.Continue;
            }

            if (!loc2R.TryGetValueAsDreamObject<DreamObjectAtom>(out var loc2)) {
                state.Push(new DreamValue(0));
                return ProcStatus.Continue;
            }

            state.Push(new((int)DreamProcNativeHelpers.GetDir(state.Proc.AtomManager, loc1, loc2)));
            return ProcStatus.Continue;
        }

        public static ProcStatus Gradient(DMProcState state) {
            var argumentInfo = state.ReadProcArguments();

            DreamValue gradientIndex = default, gradientColorSpace = DreamValue.Null;
            List<DreamValue> gradientValues = new();

            // Arguments need specially handled due to the fact that index can be either a keyed arg or the last arg
            // This is kinda ridiculous...
            if (argumentInfo.Type == DMCallArgumentsType.FromStackKeyed) {
                var stack = state.PopCount(argumentInfo.StackSize);
                var argumentCount = argumentInfo.StackSize / 2;

                gradientValues.EnsureCapacity(argumentCount - 1);
                for (int i = 0; i < argumentCount; i++) {
                    var argumentKey = stack[i * 2];
                    var argumentValue = stack[i * 2 + 1];

                    if (argumentKey.TryGetValueAsString(out var argumentKeyStr)) {
                        if (argumentKeyStr == "index") {
                            gradientIndex = argumentValue;
                            continue;
                        }

                        if (argumentKeyStr == "space") {
                            gradientColorSpace = argumentValue;
                            continue;
                        }
                    }

                    if (i == argumentCount - 1 && gradientIndex == default) {
                        gradientIndex = argumentValue;
                        continue;
                    }

                    gradientValues.Add(argumentValue);
                }
            } else if (argumentInfo.Type == DMCallArgumentsType.FromArgumentList) {
                if (!state.Pop().TryGetValueAsDreamList(out var argList))
                    throw new Exception("Invalid gradient() arguments");

                var argListValues = argList.GetValues();

                gradientValues.EnsureCapacity(argListValues.Count - 1);
                for (int i = 0; i < argListValues.Count; i++) {
                    var value = argListValues[i];

                    if (value.TryGetValueAsString(out var argumentKey)) {
                        if (argumentKey == "index") {
                            gradientIndex = argList.GetValue(value);
                            continue;
                        }

                        if (argumentKey == "space") {
                            gradientColorSpace = argList.GetValue(value);
                            continue;
                        }
                    }

                    if (i == argListValues.Count - 1 && gradientIndex == default) {
                        gradientIndex = value;
                        continue;
                    }

                    gradientValues.Add(value);
                }
            } else {
                var arguments = state.PopProcArguments(null, argumentInfo.Type, argumentInfo.StackSize);

                gradientIndex = arguments.Values[^1];
                for (int i = 0; i < arguments.Count - 1; i++) {
                    gradientValues.Add(arguments.Values[i]);
                }
            }

            if (gradientIndex == default)
                throw new Exception("No gradient index given");

            state.Push(CalculateGradient(gradientValues, gradientColorSpace, gradientIndex));
            return ProcStatus.Continue;
        }

        public static ProcStatus LocateCoord(DMProcState state) {
            var z = (int)state.Pop().UnsafeGetValueAsFloat();
            var y = (int)state.Pop().UnsafeGetValueAsFloat();
            var x = (int)state.Pop().UnsafeGetValueAsFloat();

            state.Proc.DreamMapManager.TryGetTurfAt((x, y), z, out var turf);
            state.Push(new DreamValue(turf));
            return ProcStatus.Continue;
        }

        public static ProcStatus Locate(DMProcState state) {
            if (!state.Pop().TryGetValueAsDreamObject(out var container)) {
                state.Push(DreamValue.Null);
                return ProcStatus.Continue;
            }

            DreamValue value = state.Pop();

            DreamList? containerList;
            if (container is DreamObjectAtom or DreamObjectWorld) {
                // TODO: Using world.contents for this is hilariously bad due to using WorldContentsList.GetValues()
                container.GetVariable("contents").TryGetValueAsDreamList(out containerList);
            } else {
                containerList = container as DreamList;
            }

            if (value.TryGetValueAsString(out var refString)) {
                state.Push(state.DreamManager.LocateRef(refString));
            } else if (value.TryGetValueAsType(out var ancestor)) {
                if (containerList == null) {
                    state.Push(DreamValue.Null);

                    return ProcStatus.Continue;
                }

                foreach (DreamValue containerItem in containerList.GetValues()) {
                    DreamObjectDefinition itemDef;
                    if (containerItem.TryGetValueAsType(out var type)) {
                        itemDef = type.ObjectDefinition;
                    } else if (containerItem.TryGetValueAsDreamObject(out var dmObject) && dmObject != null) {
                        itemDef = dmObject.ObjectDefinition;
                    } else {
                        continue;
                    }

                    if (itemDef.IsSubtypeOf(ancestor)) {
                        state.Push(containerItem);

                        return ProcStatus.Continue;
                    }
                }

                state.Push(DreamValue.Null);
            } else {
                if (containerList == null) {
                    state.Push(DreamValue.Null);

                    return ProcStatus.Continue;
                }

                foreach (DreamValue containerItem in containerList.GetValues()) {
                    if (IsEqual(containerItem, value)) {
                        state.Push(containerItem);

                        return ProcStatus.Continue;
                    }
                }

                state.Push(DreamValue.Null);
            }

            return ProcStatus.Continue;
        }

        public static ProcStatus PickWeighted(DMProcState state) {
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

            return ProcStatus.Continue;
        }

        public static ProcStatus PickUnweighted(DMProcState state) {
            int count = state.ReadInt();

            DreamValue picked;
            if (count == 1) {
                DreamValue value = state.Pop();

                List<DreamValue> values;
                if (value.TryGetValueAsDreamList(out var list)) {
                    values = list.GetValues();
                } else {
                    state.Push(value);
                    return ProcStatus.Continue;
                }

                if (values.Count == 0)
                    throw new Exception("pick() from empty list");

                picked = values[state.DreamManager.Random.Next(0, values.Count)];
            } else {
                int pickedIndex = state.DreamManager.Random.Next(0, count);

                picked = state.PopCount(count)[pickedIndex];
            }

            state.Push(picked);
            return ProcStatus.Continue;
        }

        public static ProcStatus Prob(DMProcState state) {
            DreamValue P = state.Pop();

            if (P.TryGetValueAsFloat(out float probability)) {
                int result = (state.DreamManager.Random.Prob(probability / 100)) ? 1 : 0;

                state.Push(new DreamValue(result));
            } else {
                state.Push(new DreamValue(0));
            }

            return ProcStatus.Continue;
        }

        public static ProcStatus IsSaved(DMProcState state) {
            DreamValue key = state.Pop();
            DreamValue owner = state.Pop();

            // number indices always evaluate to false here
            if (key.TryGetValueAsFloat(out _)) {
                state.Push(DreamValue.False);
                return ProcStatus.Continue;
            }

            if (!key.TryGetValueAsString(out string property)) {
                throw new Exception($"Invalid var for issaved() call: {key}");
            }

            if (owner.TryGetValueAsDreamObject(out DreamObject dreamObject)) {
                state.Push(dreamObject.IsSaved(property) ? DreamValue.True : DreamValue.False);
                return ProcStatus.Continue;
            }

            DreamObjectDefinition objectDefinition;
            if (owner.TryGetValueAsDreamObject(out var dreamObject2)) {
                objectDefinition = dreamObject2.ObjectDefinition;
            } else if (owner.TryGetValueAsType(out var type)) {
                objectDefinition = type.ObjectDefinition;
            } else {
                throw new Exception($"Invalid owner for issaved() call {owner}");
            }

            if (objectDefinition.GlobalVariables.ContainsKey(property)
            || (objectDefinition.ConstVariables is not null && objectDefinition.ConstVariables.Contains(property))
            || (objectDefinition.TmpVariables is not null && objectDefinition.TmpVariables.Contains(property))) {
                state.Push(new DreamValue(0));
            } else {
                state.Push(new DreamValue(1));
            }

            return ProcStatus.Continue;
        }
        #endregion Builtins

        #region Others
        private static void PerformOutput(DreamValue a, DreamValue b) {
            if (a.TryGetValueAsDreamResource(out var resource)) {
                resource.Output(b);
            } else if (a.TryGetValueAsDreamObject(out var dreamObject)) {
                if (dreamObject == null)
                    return;

                dreamObject.OperatorOutput(b);
            } else {
                throw new NotImplementedException($"Unimplemented output operation between {a} and {b}");
            }
        }

        public static ProcStatus OutputReference(DMProcState state) {
            DreamReference leftRef = state.ReadReference();
            DreamValue right = state.Pop();

            if (leftRef.Type == DMReference.Type.ListIndex) {
                state.GetIndexReferenceValues(leftRef, out _, out var indexing, peek: true);

                if (indexing.TryGetValueAsDreamObject<DreamObjectSavefile>(out _)) {
                    // Savefiles get some special treatment.
                    // "savefile[A] << B" is the same as "savefile[A] = B"

                    state.AssignReference(leftRef, right);
                    return ProcStatus.Continue;
                }
            }

            PerformOutput(state.GetReferenceValue(leftRef), right);
            return ProcStatus.Continue;
        }

        public static ProcStatus Output(DMProcState state) {
            DreamValue right = state.Pop();
            DreamValue left = state.Pop();

            PerformOutput(left, right);
            return ProcStatus.Continue;
        }

        public static ProcStatus Input(DMProcState state) {
            DreamReference leftRef = state.ReadReference();
            DreamReference rightRef = state.ReadReference();

            if (leftRef.Type == DMReference.Type.ListIndex) {
                state.GetIndexReferenceValues(leftRef, out _, out var indexing, peek: true);

                if (indexing.TryGetValueAsDreamObject<DreamObjectSavefile>(out _)) {
                    // Savefiles get some special treatment.
                    // "savefile[A] >> B" is the same as "B = savefile[A]"

                    state.AssignReference(rightRef, state.GetReferenceValue(leftRef));
                    return ProcStatus.Continue;
                } else {
                    // Pop the reference's stack values
                    state.GetReferenceValue(leftRef);
                    state.GetReferenceValue(rightRef);
                }
            }

            throw new NotImplementedException($"Input operation is unimplemented for {leftRef} and {rightRef}");
        }

        public static ProcStatus Browse(DMProcState state) {
            state.Pop().TryGetValueAsString(out string? options);
            DreamValue body = state.Pop();
            if (!state.Pop().TryGetValueAsDreamObject(out var receiver) ||  receiver == null)
                return ProcStatus.Continue;

            IEnumerable<DreamConnection> clients;
            if (receiver is DreamObjectMob { Connection: {} mobConnection }) {
                clients = new[] { mobConnection };
            } else if (receiver is DreamObjectClient receiverClient) {
                clients = new[] { receiverClient.Connection };
            } else if (receiver == state.DreamManager.WorldInstance) {
                clients = state.DreamManager.Connections;
            } else {
                throw new Exception($"Invalid browse() recipient: expected mob, client, or world, got {receiver}");
            }

            string? browseValue;
            if (body.TryGetValueAsDreamResource(out var resource)) {
                browseValue = resource.ReadAsString();
            } else if (body.TryGetValueAsString(out browseValue) || body.IsNull) {
                // Got it.
            } else {
                throw new Exception($"Invalid browse() body: expected resource or string, got {body}");
            }

            foreach (DreamConnection client in clients) {
                client.Browse(browseValue, options);
            }

            return ProcStatus.Continue;
        }

        public static ProcStatus BrowseResource(DMProcState state) {
            DreamValue filename = state.Pop();
            var value = state.Pop();

            if (!value.TryGetValueAsDreamResource(out var file)) {
                if (state.Proc.DreamResourceManager.TryLoadIcon(value, out var icon)) {
                    file = icon;
                } else {
                    throw new NotImplementedException();
                }
            }

            if (!state.Pop().TryGetValueAsDreamObject(out var receiver) || receiver == null)
                return ProcStatus.Continue;

            DreamConnection? connection;
            if (receiver is DreamObjectMob receiverMob) {
                connection = receiverMob.Connection;
            } else if (receiver is DreamObjectClient receiverClient) {
                connection = receiverClient.Connection;
            } else {
                throw new Exception("Invalid browse_rsc() recipient");
            }

            connection?.BrowseResource(file, filename.IsNull ? Path.GetFileName(file.ResourcePath) : filename.GetValueAsString());
            return ProcStatus.Continue;
        }

        public static ProcStatus DeleteObject(DMProcState state) {
            state.Pop().TryGetValueAsDreamObject(out var dreamObject);

            if (dreamObject is not null) {
                dreamObject.Delete();

                if (dreamObject == state.Instance) // We just deleted our src, end the proc TODO: Is the entire thread cancelled?
                    return ProcStatus.Returned;
            }

            return ProcStatus.Continue;
        }

        public static ProcStatus OutputControl(DMProcState state) {
            string control = state.Pop().GetValueAsString();
            string message = state.Pop().Stringify();
            if (!state.Pop().TryGetValueAsDreamObject(out var receiver) || receiver == null)
                return ProcStatus.Continue;

            // TODO: When errors are more strict (or a setting for it added), a null receiver should error

            if (receiver is DreamObjectMob receiverMob) {
                receiverMob.Connection?.OutputControl(message, control);
            } else if (receiver is DreamObjectClient receiverClient) {
                receiverClient.Connection.OutputControl(message, control);
            } else if (receiver is DreamObjectWorld) {
                // Output to every player
                foreach (var connection in state.DreamManager.Connections) {
                    connection.OutputControl(message, control);
                }
            } else {
                throw new Exception($"Invalid output() recipient: {receiver}");
            }

            return ProcStatus.Continue;
        }

        public static ProcStatus Prompt(DMProcState state) {
            DreamValueType types = (DreamValueType)state.ReadInt();
            DreamValue list = state.Pop();
            DreamValue message, title, defaultValue;

            DreamValue firstArg = state.Pop();
            firstArg.TryGetValueAsDreamObject(out var recipient);

            if (recipient is DreamObjectMob or DreamObjectClient) {
                message = state.Pop();
                title = state.Pop();
                defaultValue = state.Pop();
            } else {
                recipient = state.Usr;
                message = firstArg;
                title = state.Pop();
                defaultValue = state.Pop();
                state.PopDrop(); //Fourth argument, should be null
            }

            DreamConnection? connection = null;
            if (recipient is DreamObjectMob recipientMob)
                connection = recipientMob.Connection;
            else if (recipient is DreamObjectClient recipientClient)
                connection = recipientClient.Connection;

            if (connection == null) {
                state.Push(DreamValue.Null);
                return ProcStatus.Continue;
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

        public static ProcStatus Ftp(DMProcState state) {
            DreamValue name = state.Pop();
            DreamValue file = state.Pop();
            if (!state.Pop().TryGetValueAsDreamObject(out var receiver) || receiver == null)
                return ProcStatus.Continue;

            DreamConnection? connection;
            if (receiver is DreamObjectMob receiverMob) {
                connection = receiverMob.Connection;
            } else if (receiver is DreamObjectClient receiverClient) {
                connection = receiverClient.Connection;
            } else {
                throw new Exception("Invalid ftp() recipient");
            }

            if (!file.TryGetValueAsDreamResource(out var resource)) {
                if (file.TryGetValueAsString(out var resourcePath)) {
                    if (!state.Proc.DreamResourceManager.DoesFileExist(resourcePath))
                        return ProcStatus.Continue; // Do nothing

                    resource = state.Proc.DreamResourceManager.LoadResource(resourcePath);
                } else if (file.TryGetValueAsDreamObject<DreamObjectIcon>(out var icon)) {
                    resource = icon.Icon.GenerateDMI();
                } else {
                    throw new Exception($"{file} is not a valid file");
                }
            }

            if (!name.TryGetValueAsString(out var suggestedName))
                suggestedName = Path.GetFileName(resource.ResourcePath) ?? string.Empty;

            connection.SendFile(resource, suggestedName);
            return ProcStatus.Continue;
        }

        ///<summary>Right now this is used exclusively by addtext() calls, to concatenate its arguments together,
        ///but later it might make sense to have this be a simplification path for detected repetitive additions of strings,
        ///so as to slightly reduce the amount of re-allocation taking place.
        ///</summary>.
        public static ProcStatus MassConcatenation(DMProcState state) {
            int count = state.ReadInt();

            // One or zero arguments -- shouldn't really ever happen. addtext() compiletimes with <2 args and stringification should probably be a different opcode
            if (count < 2) {
                // TODO: tweak this warning if this ever gets used for other sorts of string concat
                Logger.GetSawmill("opendream.opcodes").Warning($"addtext() called with {count} arguments at runtime.");
                state.Push(DreamValue.Null);
                return ProcStatus.Continue;
            }

            // An approximate guess at how big this string is going to be.
            int estimatedStringSize = count * 10; // FIXME: We can do better with string size prediction here.
            var builder = new StringBuilder(estimatedStringSize);

            foreach (DreamValue add in state.PopCount(count)) {
                if (add.TryGetValueAsString(out var addStr)) {
                    builder.Append(addStr);
                }
            }

            state.Push(new DreamValue(builder.ToString()));
            return ProcStatus.Continue;
        }

        public static ProcStatus DereferenceIndex(DMProcState state) {
            DreamValue index = state.Pop();
            DreamValue obj = state.Pop();

            state.Push(state.GetIndex(obj, index));
            return ProcStatus.Continue;
        }

        public static ProcStatus DereferenceCall(DMProcState state) {
            string name = state.ReadString();
            var argumentInfo = state.ReadProcArguments();
            var argumentValues = state.PopCount(argumentInfo.StackSize);
            DreamValue obj = state.Pop();

            if (!obj.TryGetValueAsDreamObject(out var instance) || instance == null)
                throw new Exception($"Cannot dereference proc \"{name}\" from {obj}");
            if (!instance.TryGetProc(name, out var proc))
                throw new Exception($"Type {instance.ObjectDefinition.Type} has no proc called \"{name}\"");

            var arguments = state.CreateProcArguments(argumentValues, proc, argumentInfo.Type, argumentInfo.StackSize);

            return state.Call(proc, instance, arguments);
        }
        #endregion Others

        #region Helpers
        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        private static bool IsEqual(DreamValue first, DreamValue second) {
            switch (first.Type) {
                case DreamValue.DreamValueType.DreamObject: {
                    DreamObject? firstValue = first.MustGetValueAsDreamObject();

                    switch (second.Type) {
                        case DreamValue.DreamValueType.DreamObject: return firstValue == second.MustGetValueAsDreamObject();
                        case DreamValue.DreamValueType.Appearance:
                        case DreamValue.DreamValueType.DreamProc:
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
                case DreamValue.DreamValueType.Appearance: {
                    if (!second.TryGetValueAsAppearance(out var secondValue))
                        return false;

                    IconAppearance firstValue = first.MustGetValueAsAppearance();
                    return firstValue.Equals(secondValue);
                }
            }

            throw new NotImplementedException($"Equal comparison for {first} and {second} is not implemented");
        }

        private static bool IsEquivalent(DreamValue first, DreamValue second) {
            if (first.TryGetValueAsDreamObject(out var firstObject) && firstObject != null) {
                return firstObject.OperatorEquivalent(second).IsTruthy();
            }

            // Behaviour is otherwise equivalent (pun intended) to ==
            return IsEqual(first, second);
        }

        private static bool IsGreaterThan(DreamValue first, DreamValue second) {
            switch (first.Type) {
                case DreamValue.DreamValueType.Float when second.Type == DreamValue.DreamValueType.Float:
                    return first.MustGetValueAsFloat() > second.MustGetValueAsFloat();
                case DreamValue.DreamValueType.Float when second.IsNull:
                    return first.MustGetValueAsFloat() > 0;
                case DreamValue.DreamValueType.String when second.Type == DreamValue.DreamValueType.String:
                    return string.Compare(first.MustGetValueAsString(), second.MustGetValueAsString(), StringComparison.Ordinal) > 0;
                default: {
                    if (first.IsNull) {
                        if (second.Type == DreamValue.DreamValueType.Float) return 0 > second.MustGetValueAsFloat();
                        if (second.TryGetValueAsString(out var s)) return false;
                        if (second.IsNull) return false;
                    }
                    throw new Exception("Invalid greater than comparison on " + first + " and " + second);
                }
            }
        }

        private static bool IsLessThan(DreamValue first, DreamValue second) {
            switch (first.Type) {
                case DreamValue.DreamValueType.Float when second.Type == DreamValue.DreamValueType.Float:
                    return first.MustGetValueAsFloat() < second.MustGetValueAsFloat();
                case DreamValue.DreamValueType.Float when second.IsNull:
                    return first.MustGetValueAsFloat() < 0;
                case DreamValue.DreamValueType.String when second.Type == DreamValue.DreamValueType.String:
                    return string.Compare(first.MustGetValueAsString(), second.MustGetValueAsString(), StringComparison.Ordinal) < 0;
                default: {
                    if (first.IsNull) {
                        if (second.Type == DreamValue.DreamValueType.Float) return 0 < second.MustGetValueAsFloat();
                        if (second.TryGetValueAsString(out var s)) return s != "";
                        if (second.IsNull) return false;
                    }
                    throw new Exception("Invalid less than comparison between " + first + " and " + second);
                }
            }
        }

        private static DreamValue MultiplyValues(DreamValue first, DreamValue second) {
            if (first.TryGetValueAsFloat(out var firstFloat) || first.IsNull) {
                var secondFloat = second.UnsafeGetValueAsFloat(); // Non-numbers are always treated as 0 here

                return new(firstFloat * secondFloat);
            } else if (first.TryGetValueAsDreamObject(out var firstObject)) {
                return firstObject!.OperatorMultiply(second); // Not null because of the above first.IsNull check
            }

            throw new Exception($"Invalid multiply operation on {first} and {second}");
        }

        private static DreamValue DivideValues(DreamValue first, DreamValue second) {
            if (first.IsNull) {
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

        private static DreamValue BitXorValues(DreamObjectTree objectTree, DreamValue first, DreamValue second) {
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
                        if (!associatedValue.IsNull) newList.SetValue(value, associatedValue);
                    }
                }

                return new DreamValue(newList);
            } else {
                return new DreamValue(first.MustGetValueAsInteger() ^ second.MustGetValueAsInteger());
            }
        }

        private static DreamValue ModulusValues(DreamValue first, DreamValue second) {
            if (first.TryGetValueAsFloat(out var firstFloat) || first.IsNull) {
                if (second.TryGetValueAsFloat(out var secondFloat)) {
                    return new DreamValue(firstFloat % secondFloat);
                }
            }

            throw new Exception($"Invalid modulus operation on {first} and {second}");
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

        private static DreamValue CalculateGradient(List<DreamValue> gradientValues, DreamValue colorSpaceValue, DreamValue indexValue) {
            if (gradientValues.Count == 1) {
                if (!gradientValues[0].TryGetValueAsDreamList(out var gradientList))
                    throw new Exception("Invalid gradient() values; expected either a list or at least 2 values");

                gradientValues = gradientList.GetValues();
            }

            if (!indexValue.TryGetValueAsFloat(out float index))
                throw new FormatException("Failed to parse index as float");

            colorSpaceValue.TryGetValueAsInteger(out var colorSpace);

            bool loop = gradientValues.Contains(new("loop"));

            // true: look for int: false look for color
            bool colorOrInt = true;

            float workingFloat = 0;
            float maxValue = 1;
            float minValue = 0;
            float leftBound = 0;
            float rightBound = 1;

            Color? left = null;
            Color? right = null;

            foreach (DreamValue value in gradientValues) {
                if (colorOrInt && value.TryGetValueAsFloat(out float flt)) { // Int
                    colorOrInt = false;
                    workingFloat = flt;
                    maxValue = Math.Max(maxValue, flt);
                    minValue = Math.Min(minValue, flt);
                    continue; // Successful parse
                }

                if (!value.TryGetValueAsString(out string? strValue)) {
                    strValue = "#00000000";
                }

                if (strValue == "loop") continue;

                if (!ColorHelpers.TryParseColor(strValue, out Color color))
                    color = new(0, 0, 0, 0);

                if (loop && index >= maxValue) {
                    index %= maxValue;
                }

                if (workingFloat >= index) {
                    right = color;
                    rightBound = workingFloat;
                    break;
                } else {
                    left = color;
                    leftBound = workingFloat;
                }

                if (colorOrInt) {
                    workingFloat = 1;
                }

                colorOrInt = true;
            }

            // Convert the index to a 0-1 range
            float normalized = (index - leftBound) / (rightBound - leftBound);

            // Cheap way to make sure the gradient works at the extremes (eg 1 and 0)
            if (!left.HasValue || (right.HasValue && normalized == 1) || (right.HasValue && normalized == 0)) {
                if (right?.AByte == 255) {
                    return new DreamValue(right?.ToHexNoAlpha().ToLower() ?? "#00000000");
                }
                return new DreamValue(right?.ToHex().ToLower() ?? "#00000000");
            } else if (!right.HasValue) {
                if (left?.AByte == 255) {
                    return new DreamValue(left?.ToHexNoAlpha().ToLower() ?? "#00000000");
                }
                return new DreamValue(left?.ToHex().ToLower() ?? "#00000000");
            } else if (!left.HasValue && !right.HasValue) {
                throw new InvalidOperationException("Failed to find any colors");
            }

            Color returnVal;
            switch (colorSpace) {
                case 0: // RGB
                    returnVal = Color.InterpolateBetween(left.GetValueOrDefault(), right.GetValueOrDefault(), normalized);
                    break;
                case 1 or 2: // HSV/HSL
                    Vector4 vec1 = new(Color.ToHsv(left.GetValueOrDefault()));
                    Vector4 vec2 = new(Color.ToHsv(right.GetValueOrDefault()));

                    // Some precision is lost when converting back to HSV at very small values this fixes that issue
                    if (normalized < 0.05f) {
                        normalized += 0.001f;
                    }

                    // This time it's overshooting
                    // dw these numbers are insanely arbitrary
                    if(normalized > 0.9f) {
                        normalized -= 0.00445f;
                    }

                    float newHue;
                    float delta = vec2.X - vec1.X;
                    if (vec1.X > vec2.X) {
                        (vec1.X, vec2.X) = (vec2.X, vec1.X);
                        delta = -delta;
                        normalized = 1 - normalized;
                    }

                    if (delta > 0.5f) { // 180deg
                        vec1.X += 1f; // 360deg
                        newHue = (vec1.X + normalized * (vec2.X - vec1.X)) % 1; // 360deg
                    } else {
                        newHue = vec1.X + normalized * delta;
                    }

                    Vector4 holder = new(
                        newHue,
                        vec1.Y + normalized * (vec2.Y - vec1.Y),
                        vec1.Z + normalized * (vec2.Z - vec1.Z),
                        vec1.W + normalized * (vec2.W - vec1.W));

                    returnVal = Color.FromHsv(holder);
                    break;
                default:
                    throw new NotSupportedException("Cannot interpolate colorspace");
            }

            if (returnVal.AByte == 255)
                return new DreamValue(returnVal.ToHexNoAlpha().ToLower());
            return new DreamValue(returnVal.ToHex().ToLower());
        }

        private static DreamValue IconOperationAdd(DMProcState state, DreamValue icon, DreamValue blend) {
            // Create a new /icon and ICON_ADD blend it
            // Note that BYOND creates something other than an /icon, but it behaves the same as one in most reasonable interactions
            var iconObj = state.Proc.ObjectTree.CreateObject<DreamObjectIcon>(state.Proc.ObjectTree.Icon);
            if (!state.Proc.DreamResourceManager.TryLoadIcon(icon, out var from))
                throw new Exception($"Failed to create an icon from {from}");

            iconObj.Icon.InsertStates(from, DreamValue.Null, DreamValue.Null, DreamValue.Null);
            DreamProcNativeIcon.Blend(iconObj.Icon, blend, DreamIconOperationBlend.BlendType.Add, 0, 0);
            return new DreamValue(iconObj);
        }
        #endregion Helpers

        #region Peephole Optimizations

        public static ProcStatus PushReferenceAndJumpIfNotNull(DMProcState state) {
            DreamReference reference = state.ReadReference();
            int jumpTo = state.ReadInt();

            DreamValue value = state.GetReferenceValue(reference);

            if (!value.IsNull) {
                state.Push(value);
                state.Jump(jumpTo);
            } else {
                state.Push(DreamValue.Null);
            }

            return ProcStatus.Continue;
        }

        public static ProcStatus NullRef(DMProcState state) {
            state.AssignReference(state.ReadReference(), DreamValue.Null);
            return ProcStatus.Continue;
        }

        public static ProcStatus AssignPop(DMProcState state) {
            DreamReference reference = state.ReadReference();
            DreamValue value = state.Pop();

            state.AssignReference(reference, value);
            return ProcStatus.Continue;
        }

        public static ProcStatus PushReferenceAndDereferenceField(DMProcState state) {
            DreamReference reference = state.ReadReference();
            string fieldName = state.ReadString();

            DreamValue owner = state.GetReferenceValue(reference);
            state.Push(state.DereferenceField(owner, fieldName));

            return ProcStatus.Continue;
        }

        public static ProcStatus PushNStrings(DMProcState state) {
            int count = state.ReadInt();

            for (int i = 0; i < count; i++) {
                string str = state.ReadString();

                state.Push(new DreamValue(str));
            }


            return ProcStatus.Continue;
        }

        public static ProcStatus PushNFloats(DMProcState state) {
            int count = state.ReadInt();

            for (int i = 0; i < count; i++) {
                float flt = state.ReadFloat();

                state.Push(new DreamValue(flt));
            }

            return ProcStatus.Continue;
        }

        public static ProcStatus PushNRefs(DMProcState state) {
            int count = state.ReadInt();

            for (int i = 0; i < count; i++) {
                DreamReference reference = state.ReadReference();

                state.Push(state.GetReferenceValue(reference));
            }

            return ProcStatus.Continue;
        }

        public static ProcStatus PushNResources(DMProcState state) {
            int count = state.ReadInt();

            for (int i = 0; i < count; i++) {
                string resourcePath = state.ReadString();
                state.Push(new DreamValue(state.Proc.DreamResourceManager.LoadResource(resourcePath)));
            }

            return ProcStatus.Continue;
        }

        public static ProcStatus PushStringFloat(DMProcState state) {
            string str = state.ReadString();
            float flt = state.ReadFloat();

            state.Push(new DreamValue(str));
            state.Push(new DreamValue(flt));

            return ProcStatus.Continue;
        }

        public static ProcStatus JumpIfReferenceFalse(DMProcState state) {
            DreamReference reference = state.ReadReference();
            int jumpTo = state.ReadInt();

            DreamValue value = state.GetReferenceValue(reference);

            if (!value.IsTruthy()) {
                state.Jump(jumpTo);
            }

            return ProcStatus.Continue;
        }

        public static ProcStatus SwitchOnFloat(DMProcState state) {
            float testValue = state.ReadFloat();
            int casePosition = state.ReadInt();
            var test = state.Pop();
            if (test.TryGetValueAsFloat(out var value)) {
                if (testValue == value) {
                    state.Jump(casePosition);
                } else {
                    state.Push(test);
                }
            } else {
                state.Push(test);
            }

            return ProcStatus.Continue;
        }

        public static ProcStatus SwitchOnString(DMProcState state) {
            string testValue = state.ReadString();
            int casePosition = state.ReadInt();
            var test = state.Pop();
            if (test.TryGetValueAsString(out var value)) {
                if (testValue.Equals(value)) {
                    state.Jump(casePosition);
                } else {
                    state.Push(test);
                }
            } else {
                state.Push(test);
            }

            return ProcStatus.Continue;
        }


        public static ProcStatus PushNOfStringFloat(DMProcState state) {
            int count = state.ReadInt();

            for (int i = 0; i < count; i++) {
                string str = state.ReadString();
                float flt = state.ReadFloat();

                state.Push(new DreamValue(str));
                state.Push(new DreamValue(flt));
            }

            return ProcStatus.Continue;
        }

        public static ProcStatus CreateListNFloats(DMProcState state) {
            int size = state.ReadInt();
            var list = state.Proc.ObjectTree.CreateList(size);

            for (int i = 0; i < size; i++) {
                float flt = state.ReadFloat();

                list.AddValue(new DreamValue(flt));
            }

            state.Push(new DreamValue(list));
            return ProcStatus.Continue;
        }

        public static ProcStatus CreateListNStrings(DMProcState state) {
            int size = state.ReadInt();
            var list = state.Proc.ObjectTree.CreateList(size);

            for (int i = 0; i < size; i++) {
                string str = state.ReadString();

                list.AddValue(new DreamValue(str));
            }

            state.Push(new DreamValue(list));
            return ProcStatus.Continue;
        }

        public static ProcStatus CreateListNRefs(DMProcState state) {
            int size = state.ReadInt();
            var list = state.Proc.ObjectTree.CreateList(size);

            for (int i = 0; i < size; i++) {
                DreamReference reference = state.ReadReference();

                list.AddValue(state.GetReferenceValue(reference));
            }

            state.Push(new DreamValue(list));
            return ProcStatus.Continue;
        }

        public static ProcStatus CreateListNResources(DMProcState state) {
            int size = state.ReadInt();
            var list = state.Proc.ObjectTree.CreateList(size);

            for (int i = 0; i < size; i++) {
                string resourcePath = state.ReadString();
                list.AddValue(new DreamValue(state.Proc.DreamResourceManager.LoadResource(resourcePath)));
            }

            state.Push(new DreamValue(list));
            return ProcStatus.Continue;
        }

        public static ProcStatus JumpIfNotNull(DMProcState state) {
            int position = state.ReadInt();

            if (!state.Peek().IsNull) {
                state.PopDrop();
                state.Jump(position);
            }

            return ProcStatus.Continue;
        }

        public static ProcStatus IsTypeDirect(DMProcState state) {
            DreamValue value = state.Pop();
            int typeId = state.ReadInt();
            var typeValue = state.Proc.ObjectTree.Types[typeId];

            if (value.TryGetValueAsDreamObject(out var dreamObject) && dreamObject != null) {
                state.Push(new DreamValue(dreamObject.IsSubtypeOf(typeValue) ? 1 : 0));
            } else {
                state.Push(new DreamValue(0));
            }

            return ProcStatus.Continue;
        }

        #endregion
    }
}
