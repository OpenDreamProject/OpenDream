using System;
using System.Collections.Generic;
using DMCompiler.Bytecode;

namespace DMCompiler.Optimizer;

// Assign [ref]
// Pop
// -> AssignPop [ref]
internal sealed class AssignPop : IPeepholeOptimization {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.Assign,
            DreamProcOpcode.Pop
        ];
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        if (index + 1 >= input.Count) {
            throw new ArgumentOutOfRangeException("Bytecode index is outside the bounds of the input list.");
        }

        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        AnnotatedBytecodeReference? assignTarget = (firstInstruction.GetArgs()[0] as AnnotatedBytecodeReference);

        input.RemoveRange(index, 2);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.AssignPop,
            new List<IAnnotatedBytecode> { assignTarget }));
    }
}

// PushNull
// AssignPop [ref]
// -> AssignNull [ref]
internal sealed class AssignNull : IPeepholeOptimization {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushNull,
            DreamProcOpcode.AssignPop
        ];
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        // Ensure that we have at least two elements from the starting index to avoid out-of-bound errors
        if (index + 1 >= input.Count) {
            throw new ArgumentOutOfRangeException("Index plus one is outside the bounds of the input list.");
        }

        // Directly cast and extract the target from the second element's first argument
        AnnotatedBytecodeInstruction secondInstruction = (AnnotatedBytecodeInstruction)input[index + 1];
        AnnotatedBytecodeReference? assignTarget = (secondInstruction.GetArgs()[0] as AnnotatedBytecodeReference);

        // Remove the original instructions from input
        input.RemoveRange(index, 2);

        // Insert the new instruction with the extracted target as the only argument
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.NullRef,
            new List<IAnnotatedBytecode> { assignTarget }));
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
            throw new ArgumentOutOfRangeException("Index plus one is outside the bounds of the input list.");
        }

        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        AnnotatedBytecodeInstruction secondInstruction = (AnnotatedBytecodeInstruction)(input[index + 1]);
        AnnotatedBytecodeReference? pushVal = (firstInstruction.GetArgs()[0] as AnnotatedBytecodeReference);
        AnnotatedBytecodeString? derefField = (secondInstruction.GetArgs()[0] as AnnotatedBytecodeString);

        input.RemoveRange(index, 2);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.PushRefAndDereferenceField,
            new List<IAnnotatedBytecode> { pushVal, derefField }));
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
        AnnotatedBytecodeReference? pushVal = (firstInstruction.GetArgs()[0] as AnnotatedBytecodeReference);
        input.RemoveRange(index, 2);
        input.Insert(index,
            new AnnotatedBytecodeInstruction(DreamProcOpcode.ReturnReferenceValue,
                new List<IAnnotatedBytecode> { pushVal }));
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
            throw new ArgumentOutOfRangeException("Index plus one is outside the bounds of the input list.");
        }

        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        AnnotatedBytecodeInstruction secondInstruction = (AnnotatedBytecodeInstruction)(input[index + 1]);
        AnnotatedBytecodeReference? pushVal = (firstInstruction.GetArgs()[0] as AnnotatedBytecodeReference);
        AnnotatedBytecodeLabel? jumpLabel = (secondInstruction.GetArgs()[0] as AnnotatedBytecodeLabel);

        input.RemoveRange(index, 2);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.JumpIfReferenceFalse,
            new List<IAnnotatedBytecode> { pushVal, jumpLabel }));
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

        while (index + count < input.Count && input[index + count] is AnnotatedBytecodeInstruction instruction &&
               instruction.Opcode == DreamProcOpcode.PushString) {
            count++;
        }

        List<IAnnotatedBytecode> args = new List<IAnnotatedBytecode>(count + 1);
        args.Add(new AnnotatedBytecodeInteger(count, new Location()));

        for (int i = 0; i < count; i++) {
            AnnotatedBytecodeInstruction instruction = (AnnotatedBytecodeInstruction)(input[index + i]);
            args.Add(instruction.GetArgs()[0]);
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

        while (index + count < input.Count && input[index + count] is AnnotatedBytecodeInstruction instruction &&
               instruction.Opcode == DreamProcOpcode.PushFloat) {
            count++;
        }

        List<IAnnotatedBytecode> args = new List<IAnnotatedBytecode>(count + 1);
        args.Add(new AnnotatedBytecodeInteger(count, new Location()));

        for (int i = 0; i < count; i++) {
            AnnotatedBytecodeInstruction instruction = (AnnotatedBytecodeInstruction)(input[index + i]);
            args.Add(instruction.GetArgs()[0]);
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

        while (index + count < input.Count && input[index + count] is AnnotatedBytecodeInstruction instruction &&
               instruction.Opcode == DreamProcOpcode.PushReferenceValue) {
            count++;
        }

        List<IAnnotatedBytecode> args = new List<IAnnotatedBytecode>(count + 1);
        args.Add(new AnnotatedBytecodeInteger(count, new Location()));

        for (int i = 0; i < count; i++) {
            AnnotatedBytecodeInstruction instruction = (AnnotatedBytecodeInstruction)(input[index + i]);
            args.Add(instruction.GetArgs()[0]);
            stackDelta++;
        }

        input.RemoveRange(index, count);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.PushNRefs, stackDelta, args));
    }

}


// PushString [string]
// PushFloat [float]
// -> PushStringFloat [string] [float]
internal sealed class PushStringFloat : IPeepholeOptimization {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushString,
            DreamProcOpcode.PushFloat
        ];
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        if (index + 1 >= input.Count) {
            throw new ArgumentOutOfRangeException("Index plus one is outside the bounds of the input list.");
        }

        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        AnnotatedBytecodeInstruction secondInstruction = (AnnotatedBytecodeInstruction)(input[index + 1]);
        AnnotatedBytecodeString? pushVal1 = (firstInstruction.GetArgs()[0] as AnnotatedBytecodeString);
        AnnotatedBytecodeFloat? pushVal2 = (secondInstruction.GetArgs()[0] as AnnotatedBytecodeFloat);

        input.RemoveRange(index, 2);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.PushStringFloat,
            new List<IAnnotatedBytecode> { pushVal1, pushVal2 }));
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
            throw new ArgumentOutOfRangeException("Index plus one is outside the bounds of the input list.");
        }

        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        AnnotatedBytecodeInstruction secondInstruction = (AnnotatedBytecodeInstruction)(input[index + 1]);
        AnnotatedBytecodeFloat? pushVal = (firstInstruction.GetArgs()[0] as AnnotatedBytecodeFloat);
        AnnotatedBytecodeLabel? jumpLabel = (secondInstruction.GetArgs()[0] as AnnotatedBytecodeLabel);

        input.RemoveRange(index, 2);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.SwitchOnFloat,
            new List<IAnnotatedBytecode> { pushVal, jumpLabel }));
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
            throw new ArgumentOutOfRangeException("Index plus one is outside the bounds of the input list.");
        }

        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        AnnotatedBytecodeInstruction secondInstruction = (AnnotatedBytecodeInstruction)(input[index + 1]);
        AnnotatedBytecodeString? pushVal = (firstInstruction.GetArgs()[0] as AnnotatedBytecodeString);
        AnnotatedBytecodeLabel? jumpLabel = (secondInstruction.GetArgs()[0] as AnnotatedBytecodeLabel);

        input.RemoveRange(index, 2);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.SwitchOnString,
            new List<IAnnotatedBytecode> { pushVal, jumpLabel }));
    }

}

// PushStringFloat [string] [float]
// ...
// PushStringFloat [string] [float]
// -> PushArbitraryNOfStringFloat [count] [string] [float] ... [string] [float]
internal sealed class PushNOfStringFloat : IPeepholeOptimization {
    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushStringFloat,
            DreamProcOpcode.PushStringFloat
        ];
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        int count = 0;
        int stackDelta = 0;
        while (index + count < input.Count && input[index + count] is AnnotatedBytecodeInstruction instruction &&
               instruction.Opcode == DreamProcOpcode.PushStringFloat) {
            count++;
        }

        List<IAnnotatedBytecode> args = new List<IAnnotatedBytecode>(2 * count + 1);
        args.Add(new AnnotatedBytecodeInteger(count, new Location()));

        for (int i = 0; i < count; i++) {
            AnnotatedBytecodeInstruction instruction = (AnnotatedBytecodeInstruction)(input[index + i]);
            args.Add(instruction.GetArgs()[0]);
            args.Add(instruction.GetArgs()[1]);
            stackDelta += 2;
        }

        input.RemoveRange(index, count);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.PushNOfStringFloats, stackDelta, args));
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
        while (index + count < input.Count && input[index + count] is AnnotatedBytecodeInstruction instruction &&
               instruction.Opcode == DreamProcOpcode.PushResource) {
            count++;
        }

        List<IAnnotatedBytecode> args = new List<IAnnotatedBytecode>(count + 1);
        args.Add(new AnnotatedBytecodeInteger(count, new Location()));

        for (int i = 0; i < count; i++) {
            AnnotatedBytecodeInstruction instruction = (AnnotatedBytecodeInstruction)(input[index + i]);
            args.Add(instruction.GetArgs()[0]);
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
            throw new ArgumentOutOfRangeException("Index plus one is outside the bounds of the input list.");
        }

        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        AnnotatedBytecodeInstruction secondInstruction = (AnnotatedBytecodeInstruction)(input[index + 1]);
        int pushVal1 = ((firstInstruction.GetArgs()[0] as AnnotatedBytecodeInteger)!).Value;
        int pushVal2 = ((secondInstruction.GetArgs()[0] as AnnotatedBytecodeListSize)!).Size;

        return pushVal1 == pushVal2;
    }


    public void Apply(List<IAnnotatedBytecode> input, int index) {
        if (index + 1 >= input.Count) {
            throw new ArgumentOutOfRangeException("Index plus one is outside the bounds of the input list.");
        }

        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        int pushVal1 = ((firstInstruction.GetArgs()[0] as AnnotatedBytecodeInteger)!).Value;

        List<IAnnotatedBytecode> args = new List<IAnnotatedBytecode>(pushVal1 + 1);
        args.Add(new AnnotatedBytecodeInteger(pushVal1, new Location()));

        for (int i = 1; i <= pushVal1 && i < firstInstruction.GetArgs().Count; i++) {
            args.Add(firstInstruction.GetArgs()[i]);
        }

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
            throw new ArgumentOutOfRangeException("Index plus one is outside the bounds of the input list.");
        }

        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        AnnotatedBytecodeInstruction secondInstruction = (AnnotatedBytecodeInstruction)(input[index + 1]);
        int pushVal1 = ((firstInstruction.GetArgs()[0] as AnnotatedBytecodeInteger)!).Value;
        int pushVal2 = ((secondInstruction.GetArgs()[0] as AnnotatedBytecodeListSize)!).Size;

        return pushVal1 == pushVal2;
    }


    public void Apply(List<IAnnotatedBytecode> input, int index) {
        if (index + 1 >= input.Count) {
            throw new ArgumentOutOfRangeException("Index plus one is outside the bounds of the input list.");
        }

        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        int pushVal1 = ((firstInstruction.GetArgs()[0] as AnnotatedBytecodeInteger)!).Value;

        List<IAnnotatedBytecode> args = new List<IAnnotatedBytecode>(pushVal1 + 1);
        args.Add(new AnnotatedBytecodeInteger(pushVal1, new Location()));

        for (int i = 1; i <= pushVal1 && i < firstInstruction.GetArgs().Count; i++) {
            args.Add(firstInstruction.GetArgs()[i]);
        }

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
            throw new ArgumentOutOfRangeException("Index plus one is outside the bounds of the input list.");
        }

        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        AnnotatedBytecodeInstruction secondInstruction = (AnnotatedBytecodeInstruction)(input[index + 1]);
        int pushVal1 = ((firstInstruction.GetArgs()[0] as AnnotatedBytecodeInteger)!).Value;
        int pushVal2 = ((secondInstruction.GetArgs()[0] as AnnotatedBytecodeListSize)!).Size;

        return pushVal1 == pushVal2;
    }


    public void Apply(List<IAnnotatedBytecode> input, int index) {
        if (index + 1 >= input.Count) {
            throw new ArgumentOutOfRangeException("Index plus one is outside the bounds of the input list.");
        }

        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        int pushVal1 = ((firstInstruction.GetArgs()[0] as AnnotatedBytecodeInteger)!).Value;

        List<IAnnotatedBytecode> args = new List<IAnnotatedBytecode>(pushVal1 + 1);
        args.Add(new AnnotatedBytecodeInteger(pushVal1, new Location()));

        var firstInstructionArgs = firstInstruction.GetArgs();
        for (int i = 1; i <= pushVal1 && i < firstInstructionArgs.Count; i++) {
            args.Add(firstInstructionArgs[i]);
        }

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
            throw new ArgumentOutOfRangeException("Bytecode index is outside the bounds of the input list.");
        }

        int pushVal1 = ((((input[index] as AnnotatedBytecodeInstruction)!).GetArgs()[0] as AnnotatedBytecodeInteger)!).Value;
        int pushVal2 = ((((input[index + 1] as AnnotatedBytecodeInstruction)!).GetArgs()[0] as AnnotatedBytecodeListSize)!).Size;

        return pushVal1 == pushVal2;
    }

    public void Apply(List<IAnnotatedBytecode> input, int index) {
        if (index + 1 >= input.Count) {
            throw new ArgumentOutOfRangeException("Bytecode index is outside the bounds of the input list.");
        }

        var firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        int pushVal1 = ((firstInstruction.GetArgs()[0] as AnnotatedBytecodeInteger)!).Value;

        List<IAnnotatedBytecode> args = new List<IAnnotatedBytecode>(1 + pushVal1);
        args.Add(new AnnotatedBytecodeInteger(pushVal1, new Location()));

        var firstInstructionArgs = firstInstruction.GetArgs();
        for (int i = 1; i <= pushVal1 && i < firstInstructionArgs.Count; i++) {
            args.Add(firstInstructionArgs[i]);
        }

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
            throw new ArgumentOutOfRangeException("Index plus one is outside the bounds of the input list.");
        }

        var firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        AnnotatedBytecodeTypeId? pushVal = (firstInstruction.GetArgs()[0] as AnnotatedBytecodeTypeId);

        input.RemoveRange(index, 2);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.IsTypeDirect,
            new List<IAnnotatedBytecode> { pushVal }));
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
        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        var pushVal1 = ((firstInstruction.GetArgs()[0] as AnnotatedBytecodeFloat)!).Value;

        AnnotatedBytecodeInstruction secondInstruction = (AnnotatedBytecodeInstruction)(input[index + 1]);
        var pushVal2 = ((secondInstruction.GetArgs()[0] as AnnotatedBytecodeFloat)!).Value;

        var args = new List<IAnnotatedBytecode>(1) {new AnnotatedBytecodeFloat(pushVal1 * pushVal2, firstInstruction.Location)};

        input.RemoveRange(index, 3);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.PushFloat, 1, args));
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
        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        var pushVal1 = ((firstInstruction.GetArgs()[0] as AnnotatedBytecodeFloat)!).Value;

        AnnotatedBytecodeInstruction secondInstruction = (AnnotatedBytecodeInstruction)(input[index + 1]);
        var pushVal2 = ((secondInstruction.GetArgs()[0] as AnnotatedBytecodeFloat)!).Value;

        // At runtime, given "A / B" we pop B then A
        // In the peephole optimizer, index is "A", index+1 is "B"
        var args = new List<IAnnotatedBytecode>(1) {new AnnotatedBytecodeFloat(pushVal1 / pushVal2, firstInstruction.Location)};

        input.RemoveRange(index, 3);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.PushFloat, 1, args));
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
        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        var pushVal1 = ((firstInstruction.GetArgs()[0] as AnnotatedBytecodeFloat)!).Value;

        AnnotatedBytecodeInstruction secondInstruction = (AnnotatedBytecodeInstruction)(input[index + 1]);
        var pushVal2 = ((secondInstruction.GetArgs()[0] as AnnotatedBytecodeFloat)!).Value;

        var args = new List<IAnnotatedBytecode>(1) {new AnnotatedBytecodeFloat(pushVal1 + pushVal2, firstInstruction.Location)};

        input.RemoveRange(index, 3);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.PushFloat, 1, args));
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
        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        var pushVal1 = ((firstInstruction.GetArgs()[0] as AnnotatedBytecodeFloat)!).Value;

        AnnotatedBytecodeInstruction secondInstruction = (AnnotatedBytecodeInstruction)(input[index + 1]);
        var pushVal2 = ((secondInstruction.GetArgs()[0] as AnnotatedBytecodeFloat)!).Value;

        // At runtime, given "A - B" we pop B then A
        // In the peephole optimizer, index is "A", index+1 is "B"
        var args = new List<IAnnotatedBytecode>(1) {new AnnotatedBytecodeFloat(pushVal1 - pushVal2, firstInstruction.Location)};

        input.RemoveRange(index, 3);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.PushFloat, 1, args));
    }
}

#endregion
