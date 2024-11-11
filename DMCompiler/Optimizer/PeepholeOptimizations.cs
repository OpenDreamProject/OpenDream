using DMCompiler.Bytecode;

namespace DMCompiler.Optimizer;

// Append [ref]
// Pop
// -> AppendNoPush [ref]
internal sealed class AppendNoPush : IPeepholeOptimization {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.Append,
            DreamProcOpcode.Pop
        ];
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        if (index + 1 >= input.Count) {
            throw new ArgumentOutOfRangeException(nameof(index), "Bytecode index is outside the bounds of the input list.");
        }

        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        AnnotatedBytecodeReference assignTarget = firstInstruction.GetArg<AnnotatedBytecodeReference>(0);

        input.RemoveRange(index, 2);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.AppendNoPush, [assignTarget]));
    }
}

// Assign [ref]
// Pop
// -> AssignNoPush [ref]
internal sealed class AssignNoPush : IPeepholeOptimization {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.Assign,
            DreamProcOpcode.Pop
        ];
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        if (index + 1 >= input.Count) {
            throw new ArgumentOutOfRangeException(nameof(index), "Bytecode index is outside the bounds of the input list.");
        }

        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        AnnotatedBytecodeReference assignTarget = firstInstruction.GetArg<AnnotatedBytecodeReference>(0);

        input.RemoveRange(index, 2);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.AssignNoPush, [assignTarget]));
    }
}

// PushNull
// AssignNoPush [ref]
// -> AssignNull [ref]
internal sealed class AssignNull : IPeepholeOptimization {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushNull,
            DreamProcOpcode.AssignNoPush
        ];
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        // Ensure that we have at least two elements from the starting index to avoid out-of-bound errors
        if (index + 1 >= input.Count) {
            throw new ArgumentOutOfRangeException(nameof(index), "Index plus one is outside the bounds of the input list.");
        }

        // Directly cast and extract the target from the second element's first argument
        AnnotatedBytecodeInstruction secondInstruction = (AnnotatedBytecodeInstruction)input[index + 1];
        AnnotatedBytecodeReference assignTarget = secondInstruction.GetArg<AnnotatedBytecodeReference>(0);

        // Remove the original instructions from input
        input.RemoveRange(index, 2);

        // Insert the new instruction with the extracted target as the only argument
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.NullRef, [assignTarget]));
    }
}

// PushReferenceValue [ref]
// DereferenceField [field]
// -> PushRefAndDereferenceField [ref, field]
internal sealed class PushField : IPeepholeOptimization {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushReferenceValue,
            DreamProcOpcode.DereferenceField
        ];
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        if (index + 1 >= input.Count) {
            throw new ArgumentOutOfRangeException(nameof(index), "Index plus one is outside the bounds of the input list.");
        }

        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        AnnotatedBytecodeInstruction secondInstruction = (AnnotatedBytecodeInstruction)(input[index + 1]);
        AnnotatedBytecodeReference pushVal = firstInstruction.GetArg<AnnotatedBytecodeReference>(0);
        AnnotatedBytecodeString derefField = secondInstruction.GetArg<AnnotatedBytecodeString>(0);

        input.RemoveRange(index, 2);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.PushRefAndDereferenceField,
            [pushVal, derefField]));
    }
}

// PushReferenceValue [ref]
// Return
// -> ReturnReferenceValue [ref]
internal class ReturnReferenceValue : IPeepholeOptimization {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushReferenceValue,
            DreamProcOpcode.Return
        ];
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        AnnotatedBytecodeReference pushVal = firstInstruction.GetArg<AnnotatedBytecodeReference>(0);
        input.RemoveRange(index, 2);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.ReturnReferenceValue, [pushVal]));
    }
}

// PushFloat [float]
// Return
// -> ReturnFloat [float]
internal class ReturnFloat : IPeepholeOptimization {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.Return
        ];
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        var firstInstruction = IPeepholeOptimization.GetInstructionAndValue(input[index], out var pushVal);
        IPeepholeOptimization.ReplaceInstructions(input, index, 2,
            new AnnotatedBytecodeInstruction(DreamProcOpcode.ReturnFloat, [new AnnotatedBytecodeFloat(pushVal, firstInstruction.Location)]));
    }
}

// PushReferenceValue [ref]
// JumpIfFalse [label]
// -> JumpIfReferenceFalse [ref] [label]
internal sealed class JumpIfReferenceFalse : IPeepholeOptimization {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushReferenceValue,
            DreamProcOpcode.JumpIfFalse
        ];
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        if (index + 1 >= input.Count) {
            throw new ArgumentOutOfRangeException(nameof(index), "Index plus one is outside the bounds of the input list.");
        }

        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        AnnotatedBytecodeInstruction secondInstruction = (AnnotatedBytecodeInstruction)(input[index + 1]);
        AnnotatedBytecodeReference pushVal = firstInstruction.GetArg<AnnotatedBytecodeReference>(0);
        AnnotatedBytecodeLabel jumpLabel = secondInstruction.GetArg<AnnotatedBytecodeLabel>(0);

        input.RemoveRange(index, 2);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.JumpIfReferenceFalse,
            [pushVal, jumpLabel]));
    }
}

// PushString [string]
// ...
// PushString [string]
// -> PushNStrings [count] [string] ... [string]
internal sealed class PushNStrings : IPeepholeOptimization {
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
internal sealed class PushNFloats : IPeepholeOptimization {
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
internal sealed class PushNRef : IPeepholeOptimization {
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
internal sealed class PushStringFloat : IPeepholeOptimization {
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

// PushFloat [float]
// SwitchCase [label]
// -> SwitchOnFloat [float] [label]
internal sealed class SwitchOnFloat : IPeepholeOptimization {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.SwitchCase
        ];
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        if (index + 1 >= input.Count) {
            throw new ArgumentOutOfRangeException(nameof(index), "Index plus one is outside the bounds of the input list.");
        }

        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        AnnotatedBytecodeInstruction secondInstruction = (AnnotatedBytecodeInstruction)(input[index + 1]);
        AnnotatedBytecodeFloat pushVal = firstInstruction.GetArg<AnnotatedBytecodeFloat>(0);
        AnnotatedBytecodeLabel jumpLabel = secondInstruction.GetArg<AnnotatedBytecodeLabel>(0);

        input.RemoveRange(index, 2);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.SwitchOnFloat, [pushVal, jumpLabel]));
    }
}

// PushString [string]
// SwitchCase [label]
// -> SwitchOnString [string] [label]
internal sealed class SwitchOnString : IPeepholeOptimization {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushString,
            DreamProcOpcode.SwitchCase
        ];
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        if (index + 1 >= input.Count) {
            throw new ArgumentOutOfRangeException(nameof(index), "Index plus one is outside the bounds of the input list.");
        }

        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        AnnotatedBytecodeInstruction secondInstruction = (AnnotatedBytecodeInstruction)(input[index + 1]);
        AnnotatedBytecodeString pushVal = firstInstruction.GetArg<AnnotatedBytecodeString>(0);
        AnnotatedBytecodeLabel jumpLabel = secondInstruction.GetArg<AnnotatedBytecodeLabel>(0);

        input.RemoveRange(index, 2);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.SwitchOnString, [pushVal, jumpLabel]));
    }
}

// PushResource [resource]
// ...
// PushResource [resource]
// -> PushNResources [count] [resource] ... [resource]
internal sealed class PushNResources : IPeepholeOptimization {
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

// PushNFloats [count] [float] ... [float]
// CreateList [count]
// -> CreateListNFloats [count] [float] ... [float]
internal sealed class CreateListNFloats : IPeepholeOptimization {
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
internal sealed class CreateListNStrings : IPeepholeOptimization {
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
internal sealed class CreateListNResources : IPeepholeOptimization {
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
internal sealed class CreateListNRefs : IPeepholeOptimization {
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

// Jump [label1]
// Jump [label2] <- Dead code
// -> Jump [label1]
internal sealed class RemoveJumpFollowedByJump : IPeepholeOptimization {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.Jump,
            DreamProcOpcode.Jump
        ];
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        input.RemoveAt(index + 1);
    }
}

// PushType [type]
// IsType
// -> IsTypeDirect [type]
internal sealed class IsTypeDirect : IPeepholeOptimization {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushType,
            DreamProcOpcode.IsType
        ];
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        if (index + 1 >= input.Count) {
            throw new ArgumentOutOfRangeException(nameof(index), "Index plus one is outside the bounds of the input list.");
        }

        var firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        AnnotatedBytecodeTypeId pushVal = firstInstruction.GetArg<AnnotatedBytecodeTypeId>(0);

        input.RemoveRange(index, 2);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.IsTypeDirect, [pushVal]));
    }
}

#region Constant Folding
// PushFloat [constant]
// PushFloat [constant]
// Multiply
// -> PushFloat [result]
internal sealed class ConstFoldMultiply : IPeepholeOptimization {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.Multiply,
        ];
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        var firstInstruction = IPeepholeOptimization.GetInstructionAndValue(input[index], out var pushVal1);

        IPeepholeOptimization.GetInstructionAndValue(input[index + 1], out var pushVal2);

        var args = new List<IAnnotatedBytecode>(1) {new AnnotatedBytecodeFloat(pushVal1 * pushVal2, firstInstruction.Location)};

        IPeepholeOptimization.ReplaceInstructions(input, index, 3,
            new AnnotatedBytecodeInstruction(DreamProcOpcode.PushFloat, 1, args));
    }
}

// PushFloat [constant]
// PushFloat [constant]
// Divide
// -> PushFloat [result]
internal sealed class ConstFoldDivide : IPeepholeOptimization {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.Divide,
        ];
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        var firstInstruction = IPeepholeOptimization.GetInstructionAndValue(input[index], out var pushVal1);

        IPeepholeOptimization.GetInstructionAndValue(input[index + 1], out var pushVal2);

        // At runtime, given "A / B" we pop B then A
        // In the peephole optimizer, index is "A", index+1 is "B"
        var args = new List<IAnnotatedBytecode>(1) {new AnnotatedBytecodeFloat(pushVal1 / pushVal2, firstInstruction.Location)};

        IPeepholeOptimization.ReplaceInstructions(input, index, 3,
            new AnnotatedBytecodeInstruction(DreamProcOpcode.PushFloat, 1, args));
    }
}

// PushFloat [constant]
// PushFloat [constant]
// Add
// -> PushFloat [result]
internal sealed class ConstFoldAdd : IPeepholeOptimization {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.Add,
        ];
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        var firstInstruction = IPeepholeOptimization.GetInstructionAndValue(input[index], out var pushVal1);

        IPeepholeOptimization.GetInstructionAndValue(input[index + 1], out var pushVal2);

        var args = new List<IAnnotatedBytecode>(1) {new AnnotatedBytecodeFloat(pushVal1 + pushVal2, firstInstruction.Location)};

        IPeepholeOptimization.ReplaceInstructions(input, index, 3,
            new AnnotatedBytecodeInstruction(DreamProcOpcode.PushFloat, 1, args));
    }
}

// PushFloat [constant]
// PushFloat [constant]
// Subtract
// -> PushFloat [result]
internal sealed class ConstFoldSubtract : IPeepholeOptimization {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.Subtract,
        ];
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        var firstInstruction = IPeepholeOptimization.GetInstructionAndValue(input[index], out var pushVal1);

        IPeepholeOptimization.GetInstructionAndValue(input[index + 1], out var pushVal2);

        // At runtime, given "A - B" we pop B then A
        // In the peephole optimizer, index is "A", index+1 is "B"
        var args = new List<IAnnotatedBytecode>(1) {new AnnotatedBytecodeFloat(pushVal1 - pushVal2, firstInstruction.Location)};

        IPeepholeOptimization.ReplaceInstructions(input, index, 3,
            new AnnotatedBytecodeInstruction(DreamProcOpcode.PushFloat, 1, args));
    }
}

// PushFloat [constant]
// PushFloat [constant]
// Modulus
// -> PushFloat [result]
internal sealed class ConstFoldModulus : IPeepholeOptimization {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.Modulus,
        ];
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        var firstInstruction = IPeepholeOptimization.GetInstructionAndValue(input[index], out var pushVal1);

        IPeepholeOptimization.GetInstructionAndValue(input[index + 1], out var pushVal2);

        // At runtime, given "A % B" we pop B then A
        // In the peephole optimizer, index is "A", index+1 is "B"
        var args = new List<IAnnotatedBytecode>(1) {new AnnotatedBytecodeFloat((int)pushVal1 % (int)pushVal2, firstInstruction.Location)};

        IPeepholeOptimization.ReplaceInstructions(input, index, 3,
            new AnnotatedBytecodeInstruction(DreamProcOpcode.PushFloat, 1, args));
    }
}

// PushFloat [constant]
// PushFloat [constant]
// Power
// -> PushFloat [result]
internal sealed class ConstFoldPower : IPeepholeOptimization {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.Power,
        ];
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        var firstInstruction = IPeepholeOptimization.GetInstructionAndValue(input[index], out var pushVal1);

        IPeepholeOptimization.GetInstructionAndValue(input[index + 1], out var pushVal2);

        // At runtime, given "A ** B" we pop B then A
        // In the peephole optimizer, index is "A", index+1 is "B"
        var args = new List<IAnnotatedBytecode>(1) {new AnnotatedBytecodeFloat(MathF.Pow(pushVal1, pushVal2), firstInstruction.Location)};

        IPeepholeOptimization.ReplaceInstructions(input, index, 3,
            new AnnotatedBytecodeInstruction(DreamProcOpcode.PushFloat, 1, args));
    }
}

// PushFloat [constant]
// PushFloat [constant]
// BitshiftLeft
// -> PushFloat [result]
internal sealed class ConstFoldBitshiftLeft : IPeepholeOptimization {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.BitShiftLeft,
        ];
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        var firstInstruction = IPeepholeOptimization.GetInstructionAndValue(input[index], out var pushVal1);
        IPeepholeOptimization.GetInstructionAndValue(input[index + 1], out var pushVal2);

        // At runtime, given "A << B" we pop B then A
        // In the peephole optimizer, index is "A", index+1 is "B"
        var args = new List<IAnnotatedBytecode>(1) {new AnnotatedBytecodeFloat(((int)pushVal1 << (int)pushVal2), firstInstruction.Location)};

        IPeepholeOptimization.ReplaceInstructions(input, index, 3,
            new AnnotatedBytecodeInstruction(DreamProcOpcode.PushFloat, 1, args));
    }
}

// PushFloat [constant]
// PushFloat [constant]
// BitshiftRight
// -> PushFloat [result]
internal sealed class ConstFoldBitshiftRight : IPeepholeOptimization {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.BitShiftRight,
        ];
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        var firstInstruction = IPeepholeOptimization.GetInstructionAndValue(input[index], out var pushVal1);
        IPeepholeOptimization.GetInstructionAndValue(input[index + 1], out var pushVal2);

        // At runtime, given "A >> B" we pop B then A
        // In the peephole optimizer, index is "A", index+1 is "B"
        var args = new List<IAnnotatedBytecode>(1) {new AnnotatedBytecodeFloat(((int)pushVal1 >> (int)pushVal2), firstInstruction.Location)};

        IPeepholeOptimization.ReplaceInstructions(input, index, 3,
            new AnnotatedBytecodeInstruction(DreamProcOpcode.PushFloat, 1, args));
    }
}

#endregion
