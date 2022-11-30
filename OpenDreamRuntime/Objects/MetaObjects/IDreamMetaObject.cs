using OpenDreamRuntime.Procs;

namespace OpenDreamRuntime.Objects.MetaObjects {
    public interface IDreamMetaObject {
        public bool ShouldCallNew { get; }
        public IDreamMetaObject? ParentType { get; set; }

        public void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) =>
            ParentType?.OnObjectCreated(dreamObject, creationArguments);

        public void OnObjectDeleted(DreamObject dreamObject) =>
            ParentType?.OnObjectDeleted(dreamObject);

        public void OnVariableSet(DreamObject dreamObject, string varName, DreamValue value, DreamValue oldValue) =>
            ParentType?.OnVariableSet(dreamObject, varName, value, oldValue);

        public DreamValue OnVariableGet(DreamObject dreamObject, string varName, DreamValue value) =>
            ParentType?.OnVariableGet(dreamObject, varName, value) ?? value;

        public ProcStatus OperatorOutput(DreamValue a, DreamValue b,  DMProcState state) {
            if (ParentType == null)
                throw new InvalidOperationException($"Cannot output {b} to {a}");

            return ParentType.OperatorOutput(a, b, state);
        }

        //Each of these operator functions *must* either push a value onto the stack, push a proc call, or throw an error.
        //If it pushes a proc call, it must return ProcStatus.Called, otherwise ProcStatus.Returned.
        public ProcStatus OperatorAdd(DreamValue a, DreamValue b,  DMProcState state) {
            if (ParentType == null)
                if(a.TryGetValueAsDreamObject(out DreamObject obj) && obj.TryGetProc("operator+", out DreamProc overload))
                {
                    state.Call(overload, obj, new DreamProcArguments(new List<DreamValue>(){a,b}));
                    return ProcStatus.Called;
                }
                else
                    throw new InvalidOperationException($"Addition cannot be done between {a} and {b}");

            return ParentType.OperatorAdd(a, b, state);
        }

        public ProcStatus OperatorSubtract(DreamValue a, DreamValue b,  DMProcState state) {
            if (ParentType == null)
                if(a.TryGetValueAsDreamObject(out DreamObject obj) && obj.TryGetProc("operator-", out DreamProc overload))
                {
                    state.Call(overload, obj, new DreamProcArguments(new List<DreamValue>(){a,b}));
                    return ProcStatus.Called;
                }
                else
                    throw new InvalidOperationException($"Subtraction cannot be done between {a} and {b}");

            return ParentType.OperatorSubtract(a, b, state);
        }

        public ProcStatus OperatorMultiply(DreamValue a, DreamValue b,  DMProcState state) {
            if (ParentType == null)
                if(a.TryGetValueAsDreamObject(out DreamObject obj) && obj.TryGetProc("operator*", out DreamProc overload))
                {
                    state.Call(overload, obj, new DreamProcArguments(new List<DreamValue>(){a,b}));
                    return ProcStatus.Called;
                }
                else
                    throw new InvalidOperationException($"Multiplication cannot be done between {a} and {b}");

            return ParentType.OperatorMultiply(a, b, state);
        }

        public ProcStatus OperatorAppend(DreamValue a, DreamValue b,  DMProcState state) {
            if (ParentType == null)
                if(a.TryGetValueAsDreamObject(out DreamObject obj) && obj.TryGetProc("operator+=", out DreamProc overload))
                {
                    state.Call(overload, obj, new DreamProcArguments(new List<DreamValue>(){a,b}));
                    return ProcStatus.Called;
                }
                else
                    throw new InvalidOperationException($"Cannot append {b} to {a}");

            return ParentType.OperatorAppend(a, b, state);
        }

        public ProcStatus OperatorRemove(DreamValue a, DreamValue b,  DMProcState state) {
            if (ParentType == null)
                if(a.TryGetValueAsDreamObject(out DreamObject obj) && obj.TryGetProc("operator-=", out DreamProc overload))
                {
                    state.Call(overload, obj, new DreamProcArguments(new List<DreamValue>(){a,b}));
                    return ProcStatus.Called;
                }
                else
                    throw new InvalidOperationException($"Cannot remove {b} from {a}");

            return ParentType.OperatorRemove(a, b, state);
        }

        public ProcStatus OperatorOr(DreamValue a, DreamValue b,  DMProcState state) {
            if (ParentType == null)
                if(a.TryGetValueAsDreamObject(out DreamObject obj) && obj.TryGetProc("operator|", out DreamProc overload))
                {
                    state.Call(overload, obj, new DreamProcArguments(new List<DreamValue>(){a,b}));
                    return ProcStatus.Called;
                }
                else
                    throw new InvalidOperationException($"Cannot or {a} and {b}");

            return ParentType.OperatorOr(a, b, state);
        }

        public ProcStatus OperatorEquivalent(DreamValue a, DreamValue b,  DMProcState state) {
            if (ParentType == null)
                if(a.TryGetValueAsDreamObject(out DreamObject obj) && obj.TryGetProc("operator~=", out DreamProc overload))
                {
                    state.Call(overload, obj, new DreamProcArguments(new List<DreamValue>(){a,b}));
                    return ProcStatus.Called;
                }
                else
                {
                    state.Push(a.Equals(b) ? new DreamValue(1f) : new DreamValue(0f));
                    return ProcStatus.Returned;
                }


            return ParentType.OperatorEquivalent(a, b, state);
        }

        public ProcStatus OperatorCombine(DreamValue a, DreamValue b,  DMProcState state) {
            if (ParentType == null)
                if(a.TryGetValueAsDreamObject(out DreamObject obj) && obj.TryGetProc("operator|=", out DreamProc overload))
                {
                    state.Call(overload, obj, new DreamProcArguments(new List<DreamValue>(){a,b}));
                    return ProcStatus.Called;
                }
                else
                    throw new InvalidOperationException($"Cannot combine {a} and {b}");

            return ParentType.OperatorCombine(a, b, state);
        }

        public ProcStatus OperatorMask(DreamValue a, DreamValue b,  DMProcState state) {
            if (ParentType == null)
                if(a.TryGetValueAsDreamObject(out DreamObject obj) && obj.TryGetProc("operator&=", out DreamProc overload))
                {
                    state.Call(overload, obj, new DreamProcArguments(new List<DreamValue>(){a,b}));
                    return ProcStatus.Called;
                }
                else
                    throw new InvalidOperationException($"Cannot mask {a} and {b}");

            return ParentType.OperatorMask(a, b, state);
        }

        public ProcStatus OperatorIndex(DreamValue a, DreamValue index, DMProcState state) {
            if (ParentType == null)
                if(a.TryGetValueAsDreamObject(out DreamObject obj) && obj.TryGetProc("operator[]", out DreamProc overload))
                {
                    state.Call(overload, obj, new DreamProcArguments(new List<DreamValue>(){index}));
                    return ProcStatus.Called;
                }
                else
                    throw new InvalidOperationException($"Cannot index {a}");

            return ParentType.OperatorIndex(a, index, state);
        }

        public ProcStatus OperatorIndexAssign(DreamValue a, DreamValue index, DreamValue value, DMProcState state) {
            if (ParentType == null)
                if(a.TryGetValueAsDreamObject(out DreamObject obj) && obj.TryGetProc("operator[]=", out DreamProc overload))
                {
                    state.Call(overload, obj, new DreamProcArguments(new List<DreamValue>(){index,value}));
                    return ProcStatus.Called;
                }
                else
                    throw new InvalidOperationException($"Cannot assign {value} to index {index} of {a}");

            return ParentType.OperatorIndexAssign(a, index, value, state);
        }

        public ProcStatus OperatorBitAnd(DreamValue a, DreamValue b,  DMProcState state) {
            if (ParentType == null)
                if(a.TryGetValueAsDreamObject(out DreamObject obj) && obj.TryGetProc("operator&", out DreamProc overload))
                {
                    state.Call(overload, obj, new DreamProcArguments(new List<DreamValue>(){a,b}));
                    return ProcStatus.Called;
                }
                else
                    throw new InvalidOperationException($"Cannot bit-and {a} and {b}");

            return ParentType.OperatorBitAnd(a, b, state);
        }

        public ProcStatus OperatorBitNot(DreamValue a, DMProcState state) {
            if (ParentType == null)
                if(a.TryGetValueAsDreamObject(out DreamObject obj) && obj.TryGetProc("operator~", out DreamProc overload))
                {
                    state.Call(overload, obj, new DreamProcArguments(new List<DreamValue>(){a}));
                    return ProcStatus.Called;
                }
                else
                    throw new InvalidOperationException($"Cannot bit-not {a}");

            return ParentType.OperatorBitNot(a, state);
        }

        public ProcStatus OperatorBitOr(DreamValue a, DreamValue b,  DMProcState state) {
            if (ParentType == null)
                if(a.TryGetValueAsDreamObject(out DreamObject obj) && obj.TryGetProc("operator|", out DreamProc overload))
                {
                    state.Call(overload, obj, new DreamProcArguments(new List<DreamValue>(){a,b}));
                    return ProcStatus.Called;
                }
                else
                    throw new InvalidOperationException($"Cannot bit-or {a} and {b}");

            return ParentType.OperatorBitOr(a, b, state);
        }

        public ProcStatus OperatorBitShiftLeft(DreamValue a, DreamValue b, DMProcState state) {
            if (ParentType == null)
                if(a.TryGetValueAsDreamObject(out DreamObject obj) && obj.TryGetProc("operator<<", out DreamProc overload))
                {
                    state.Call(overload, obj, new DreamProcArguments(new List<DreamValue>(){a,b}));
                    return ProcStatus.Called;
                }
                else
                    throw new InvalidOperationException($"Cannot bit-shift-left {a} by {b}");

            return ParentType.OperatorBitShiftLeft(a, b, state);
        }
        public ProcStatus OperatorBitShiftRight(DreamValue a, DreamValue b, DMProcState state) {
            if (ParentType == null)
                if(a.TryGetValueAsDreamObject(out DreamObject obj) && obj.TryGetProc("operator>>", out DreamProc overload))
                {
                    state.Call(overload, obj, new DreamProcArguments(new List<DreamValue>(){a,b}));
                    return ProcStatus.Called;
                }
                else
                    throw new InvalidOperationException($"Cannot bit-shift-right {a} by {b}");

            return ParentType.OperatorBitShiftRight(a, b, state);
        }

        public ProcStatus OperatorBitXor(DreamValue a, DreamValue b,  DMProcState state) {
            if (ParentType == null)
                if(a.TryGetValueAsDreamObject(out DreamObject obj) && obj.TryGetProc("operator^", out DreamProc overload))
                {
                    state.Call(overload, obj, new DreamProcArguments(new List<DreamValue>(){a,b}));
                    return ProcStatus.Called;
                }
                else
                    throw new InvalidOperationException($"Cannot bit-xor {a} and {b}");

            return ParentType.OperatorBitXor(a, b, state);
        }
        public ProcStatus OperatorDivide(DreamValue a, DreamValue b,  DMProcState state) {
            if (ParentType == null)
                if(a.TryGetValueAsDreamObject(out DreamObject obj) && obj.TryGetProc("operator/", out DreamProc overload))
                {
                    state.Call(overload, obj, new DreamProcArguments(new List<DreamValue>(){a,b}));
                    return ProcStatus.Called;
                }
                else
                    throw new InvalidOperationException($"Cannot divide {a} by {b}");

            return ParentType.OperatorDivide(a, b, state);
        }
        public ProcStatus OperatorModulus(DreamValue a, DreamValue b,  DMProcState state) {
            if (ParentType == null)
                if(a.TryGetValueAsDreamObject(out DreamObject obj) && obj.TryGetProc("operator%", out DreamProc overload))
                {
                    state.Call(overload, obj, new DreamProcArguments(new List<DreamValue>(){a,b}));
                    return ProcStatus.Called;
                }
                else
                    throw new InvalidOperationException($"Cannot modulo {a} by {b}");

            return ParentType.OperatorModulus(a, b, state);
        }
        public ProcStatus OperatorModulusModulus(DreamValue a, DreamValue b,  DMProcState state) {
            if (ParentType == null)
                if(a.TryGetValueAsDreamObject(out DreamObject obj) && obj.TryGetProc("operator%%", out DreamProc overload))
                {
                    state.Call(overload, obj, new DreamProcArguments(new List<DreamValue>(){a,b}));
                    return ProcStatus.Called;
                }
                else
                    throw new InvalidOperationException($"Cannot modulo-modulo {a} by {b}");

            return ParentType.OperatorModulusModulus(a, b, state);
        }
        public ProcStatus OperatorNegate(DreamValue a, DMProcState state) {
            if (ParentType == null)
                if(a.TryGetValueAsDreamObject(out DreamObject obj) && obj.TryGetProc("operator-", out DreamProc overload))
                {
                    state.Call(overload, obj, new DreamProcArguments());
                    return ProcStatus.Called;
                }
                else
                    throw new InvalidOperationException($"Cannot negate {a}");

            return ParentType.OperatorNegate(a, state);
        }
        public ProcStatus OperatorPower(DreamValue a, DreamValue b,  DMProcState state) {
            if (ParentType == null)
                if(a.TryGetValueAsDreamObject(out DreamObject obj) && obj.TryGetProc("operator^", out DreamProc overload))
                {
                    state.Call(overload, obj, new DreamProcArguments(new List<DreamValue>(){a,b}));
                    return ProcStatus.Called;
                }
                else
                    throw new InvalidOperationException($"Cannot raise {a} to the power of {b}");

            return ParentType.OperatorPower(a, b, state);
        }


    }
}
