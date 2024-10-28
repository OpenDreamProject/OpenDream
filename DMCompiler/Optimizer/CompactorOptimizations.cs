using DMCompiler.Bytecode;

namespace DMCompiler.Optimizer;

#region BytecodeCompactors

// PushString [string]
// ...
// PushString [string]
// -> PushNStrings [count] [string] ... [string]
internal sealed class PushNStrings : IBytecodeCompactor {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushString,
            DreamProcOpcode.PushString
        ];
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        int count = 0;
        int stackDelta = 0;

        while (index + count < input.Count &&
               input[index + count] is AnnotatedBytecodeInstruction { Opcode: DreamProcOpcode.PushString }) {
            count++;
        }

        List<IAnnotatedBytecode> args = new List<IAnnotatedBytecode>(count + 1);
        args.Add(new AnnotatedBytecodeInteger(count, new Location()));

        for (int i = 0; i < count; i++) {
            AnnotatedBytecodeInstruction instruction = (AnnotatedBytecodeInstruction)(input[index + i]);
            args.Add(instruction.GetArg(0));
            stackDelta++;
        }

        input.RemoveRange(index, count);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.PushNStrings, stackDelta, args));
    }
}

// PushFloat [float]
// ...
// PushFloat [float]
// -> PushNFloats [count] [float] ... [float]
internal sealed class PushNFloats : IBytecodeCompactor {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.PushFloat
        ];
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        int count = 0;
        int stackDelta = 0;

        while (index + count < input.Count &&
               input[index + count] is AnnotatedBytecodeInstruction { Opcode: DreamProcOpcode.PushFloat }) {
            count++;
        }

        List<IAnnotatedBytecode> args = new List<IAnnotatedBytecode>(count + 1);
        args.Add(new AnnotatedBytecodeInteger(count, new Location()));

        for (int i = 0; i < count; i++) {
            AnnotatedBytecodeInstruction instruction = (AnnotatedBytecodeInstruction)(input[index + i]);
            args.Add(instruction.GetArg(0));
            stackDelta++;
        }

        input.RemoveRange(index, count);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.PushNFloats, stackDelta, args));
    }
}

// PushReferenceValue [ref]
// ...
// PushReferenceValue [ref]
// -> PushNRef [count] [ref] ... [ref]
internal sealed class PushNRef : IBytecodeCompactor {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushReferenceValue,
            DreamProcOpcode.PushReferenceValue
        ];
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        int count = 0;
        int stackDelta = 0;

        while (index + count < input.Count &&
               input[index + count] is AnnotatedBytecodeInstruction { Opcode: DreamProcOpcode.PushReferenceValue }) {
            count++;
        }

        List<IAnnotatedBytecode> args = new List<IAnnotatedBytecode>(count + 1);
        args.Add(new AnnotatedBytecodeInteger(count, new Location()));

        for (int i = 0; i < count; i++) {
            AnnotatedBytecodeInstruction instruction = (AnnotatedBytecodeInstruction)(input[index + i]);
            args.Add(instruction.GetArg(0));
            stackDelta++;
        }

        input.RemoveRange(index, count);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.PushNRefs, stackDelta, args));
    }
}

// PushString [string]
// PushFloat [float]
// -> PushStringFloat [string] [float]
// or if there's multiple
// -> PushNOfStringFloat [count] [string] [float] ... [string] [float]
internal sealed class PushStringFloat : IBytecodeCompactor {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushString,
            DreamProcOpcode.PushFloat
        ];
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        if (index + 1 >= input.Count) {
            throw new ArgumentOutOfRangeException(nameof(index), "Index plus one is outside the bounds of the input list.");
        }

        int count = 0;
        while (index + count*2 + 1 < input.Count &&
               input[index + count * 2] is AnnotatedBytecodeInstruction { Opcode: DreamProcOpcode.PushString } && input[index + count * 2 + 1] is AnnotatedBytecodeInstruction { Opcode: DreamProcOpcode.PushFloat }) {
            count++;
        }

        // If the pattern only occurs once, replace with PushStringFloat and return
        if (count == 1) {
            AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
            AnnotatedBytecodeInstruction secondInstruction = (AnnotatedBytecodeInstruction)(input[index + 1]);
            AnnotatedBytecodeString pushVal1 = firstInstruction.GetArg<AnnotatedBytecodeString>(0);
            AnnotatedBytecodeFloat pushVal2 = secondInstruction.GetArg<AnnotatedBytecodeFloat>(0);

            input.RemoveRange(index, 2);
            input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.PushStringFloat, [pushVal1, pushVal2]));
            return;
        }

        // Otherwise, replace with PushNOfStringFloat

        int stackDelta = 0;
        List<IAnnotatedBytecode> args = new List<IAnnotatedBytecode>(2 * count + 1) { new AnnotatedBytecodeInteger(count, input[index].GetLocation()) };

        for (int i = 0; i < count; i++) {
            AnnotatedBytecodeInstruction stringInstruction = (AnnotatedBytecodeInstruction)(input[index + i*2]);
            AnnotatedBytecodeInstruction floatInstruction = (AnnotatedBytecodeInstruction)(input[index + i*2 + 1]);
            args.Add(stringInstruction.GetArg<AnnotatedBytecodeString>(0));
            args.Add(floatInstruction.GetArg<AnnotatedBytecodeFloat>(0));
            stackDelta += 2;
        }

        input.RemoveRange(index, count * 2);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.PushNOfStringFloats, stackDelta, args));
    }
}

// PushResource [resource]
// ...
// PushResource [resource]
// -> PushNResources [count] [resource] ... [resource]
internal sealed class PushNResources : IBytecodeCompactor {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushResource,
            DreamProcOpcode.PushResource
        ];
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        int count = 0;
        int stackDelta = 0;
        while (index + count < input.Count &&
               input[index + count] is AnnotatedBytecodeInstruction { Opcode: DreamProcOpcode.PushResource }) {
            count++;
        }

        List<IAnnotatedBytecode> args = new List<IAnnotatedBytecode>(count + 1);
        args.Add(new AnnotatedBytecodeInteger(count, new Location()));

        for (int i = 0; i < count; i++) {
            AnnotatedBytecodeInstruction instruction = (AnnotatedBytecodeInstruction)(input[index + i]);
            args.Add(instruction.GetArg(0));
            stackDelta++;
        }

        input.RemoveRange(index, count);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.PushNResources, stackDelta, args));
    }
}

#endregion

#region ListCompactors

// PushNFloats [count] [float] ... [float]
// CreateList [count]
// -> CreateListNFloats [count] [float] ... [float]
internal sealed class CreateListNFloats : IListCompactor {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushNFloats,
            DreamProcOpcode.CreateList
        ];
    }

    public bool CheckPreconditions(List<IAnnotatedBytecode> input, int index) {
        if (index + 1 >= input.Count) {
            throw new ArgumentOutOfRangeException(nameof(index), "Index plus one is outside the bounds of the input list.");
        }

        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        AnnotatedBytecodeInstruction secondInstruction = (AnnotatedBytecodeInstruction)(input[index + 1]);
        int pushVal1 = firstInstruction.GetArg<AnnotatedBytecodeInteger>(0).Value;
        int pushVal2 = secondInstruction.GetArg<AnnotatedBytecodeListSize>(0).Size;

        return pushVal1 == pushVal2;
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        if (index + 1 >= input.Count) {
            throw new ArgumentOutOfRangeException(nameof(index), "Index plus one is outside the bounds of the input list.");
        }

        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        int pushVal1 = firstInstruction.GetArg<AnnotatedBytecodeInteger>(0).Value;

        List<IAnnotatedBytecode> args = new List<IAnnotatedBytecode>(pushVal1 + 1);
        args.Add(new AnnotatedBytecodeInteger(pushVal1, new Location()));
        args.AddRange(firstInstruction.GetArgs()[1..(pushVal1+1)]);

        input.RemoveRange(index, 2);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.CreateListNFloats, 1, args));
    }
}

// PushNStrings [count] [string] ... [string]
// CreateList [count]
// -> CreateListNStrings [count] [string] ... [string]
internal sealed class CreateListNStrings : IListCompactor {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushNStrings,
            DreamProcOpcode.CreateList
        ];
    }

    public bool CheckPreconditions(List<IAnnotatedBytecode> input, int index) {
        if (index + 1 >= input.Count) {
            throw new ArgumentOutOfRangeException(nameof(index), "Index plus one is outside the bounds of the input list.");
        }

        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        AnnotatedBytecodeInstruction secondInstruction = (AnnotatedBytecodeInstruction)(input[index + 1]);
        int pushVal1 = firstInstruction.GetArg<AnnotatedBytecodeInteger>(0).Value;
        int pushVal2 = secondInstruction.GetArg<AnnotatedBytecodeListSize>(0).Size;

        return pushVal1 == pushVal2;
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        if (index + 1 >= input.Count) {
            throw new ArgumentOutOfRangeException(nameof(index), "Index plus one is outside the bounds of the input list.");
        }

        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        int pushVal1 = firstInstruction.GetArg<AnnotatedBytecodeInteger>(0).Value;

        List<IAnnotatedBytecode> args = new List<IAnnotatedBytecode>(pushVal1 + 1);
        args.Add(new AnnotatedBytecodeInteger(pushVal1, new Location()));
        args.AddRange(firstInstruction.GetArgs()[1..(pushVal1+1)]);

        input.RemoveRange(index, 2);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.CreateListNStrings, 1, args));
    }
}

// PushNResources [count] [resource] ... [resource]
// CreateList [count]
// -> CreateListNResources [count] [resource] ... [resource]
internal sealed class CreateListNResources : IListCompactor {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushNResources,
            DreamProcOpcode.CreateList
        ];
    }

    public bool CheckPreconditions(List<IAnnotatedBytecode> input, int index) {
        if (index + 1 >= input.Count) {
            throw new ArgumentOutOfRangeException(nameof(index), "Index plus one is outside the bounds of the input list.");
        }

        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        AnnotatedBytecodeInstruction secondInstruction = (AnnotatedBytecodeInstruction)(input[index + 1]);
        int pushVal1 = firstInstruction.GetArg<AnnotatedBytecodeInteger>(0).Value;
        int pushVal2 = secondInstruction.GetArg<AnnotatedBytecodeListSize>(0).Size;

        return pushVal1 == pushVal2;
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        if (index + 1 >= input.Count) {
            throw new ArgumentOutOfRangeException(nameof(index), "Index plus one is outside the bounds of the input list.");
        }

        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        int pushVal1 = firstInstruction.GetArg<AnnotatedBytecodeInteger>(0).Value;

        List<IAnnotatedBytecode> args = new List<IAnnotatedBytecode>(pushVal1 + 1);
        args.Add(new AnnotatedBytecodeInteger(pushVal1, new Location()));
        args.AddRange(firstInstruction.GetArgs()[1..(pushVal1+1)]);

        input.RemoveRange(index, 2);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.CreateListNResources, 1, args));
    }
}

// PushNRefs [count] [ref] ... [ref]
// CreateList [count]
// -> CreateListNRefs [count] [ref] ... [ref]
internal sealed class CreateListNRefs : IListCompactor {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushNRefs,
            DreamProcOpcode.CreateList
        ];
    }

    public bool CheckPreconditions(List<IAnnotatedBytecode> input, int index) {
        if (index + 1 >= input.Count) {
            throw new ArgumentOutOfRangeException(nameof(index),"Bytecode index is outside the bounds of the input list.");
        }

        int pushVal1 = ((AnnotatedBytecodeInstruction)input[index]).GetArg<AnnotatedBytecodeInteger>(0).Value;
        int pushVal2 = ((AnnotatedBytecodeInstruction)input[index + 1]).GetArg<AnnotatedBytecodeListSize>(0).Size;

        return pushVal1 == pushVal2;
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        if (index + 1 >= input.Count) {
            throw new ArgumentOutOfRangeException(nameof(index), "Bytecode index is outside the bounds of the input list.");
        }

        var firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        int pushVal1 = firstInstruction.GetArg<AnnotatedBytecodeInteger>(0).Value;

        List<IAnnotatedBytecode> args = new List<IAnnotatedBytecode>(1 + pushVal1);
        args.Add(new AnnotatedBytecodeInteger(pushVal1, new Location()));
        args.AddRange(firstInstruction.GetArgs()[1..(pushVal1+1)]);

        input.RemoveRange(index, 2);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.CreateListNRefs, 1, args));
    }
}

#endregion
