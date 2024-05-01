using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DMCompiler.Bytecode;

namespace DMCompiler.Optimizer;

// Assign [ref]
// Pop
// -> AssignPop [ref]
internal class AssignPop : IPeepholeOptimization {
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

        AnnotatedBytecodeInstruction firstInstruction = Unsafe.As<AnnotatedBytecodeInstruction>(input[index]);
        AnnotatedBytecodeReference? assignTarget = (firstInstruction.GetArgs()[0] as AnnotatedBytecodeReference);

        input.RemoveRange(index, 2);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.AssignPop,
            new List<IAnnotatedBytecode> { assignTarget }));
    }
}

// PushNull
// AssignPop [ref]
// -> AssignNull [ref]
internal class AssignNull : IPeepholeOptimization {
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
internal class PushField : IPeepholeOptimization {
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

        AnnotatedBytecodeInstruction firstInstruction = Unsafe.As<AnnotatedBytecodeInstruction>(input[index]);
        AnnotatedBytecodeInstruction secondInstruction = Unsafe.As<AnnotatedBytecodeInstruction>(input[index + 1]);
        AnnotatedBytecodeReference? pushVal = (firstInstruction.GetArgs()[0] as AnnotatedBytecodeReference);
        AnnotatedBytecodeString? derefField = (secondInstruction.GetArgs()[0] as AnnotatedBytecodeString);

        input.RemoveRange(index, 2);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.PushRefAndDereferenceField,
            new List<IAnnotatedBytecode> { pushVal, derefField }));
    }

}

// PushReferenceValue [ref]
// JumpIfFalse [label]
// -> JumpIfReferenceFalse [ref] [label]
internal class JumpIfReferenceFalse : IPeepholeOptimization {
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

        AnnotatedBytecodeInstruction firstInstruction = Unsafe.As<AnnotatedBytecodeInstruction>(input[index]);
        AnnotatedBytecodeInstruction secondInstruction = Unsafe.As<AnnotatedBytecodeInstruction>(input[index + 1]);
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
internal class PushNStrings : IPeepholeOptimization {
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
            AnnotatedBytecodeInstruction instruction = Unsafe.As<AnnotatedBytecodeInstruction>(input[index + i]);
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
internal class PushNFloats : IPeepholeOptimization {
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
            AnnotatedBytecodeInstruction instruction = Unsafe.As<AnnotatedBytecodeInstruction>(input[index + i]);
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
internal class PushNRef : IPeepholeOptimization {
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
            AnnotatedBytecodeInstruction instruction = Unsafe.As<AnnotatedBytecodeInstruction>(input[index + i]);
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
internal class PushStringFloat : IPeepholeOptimization {
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

        AnnotatedBytecodeInstruction firstInstruction = Unsafe.As<AnnotatedBytecodeInstruction>(input[index]);
        AnnotatedBytecodeInstruction secondInstruction = Unsafe.As<AnnotatedBytecodeInstruction>(input[index + 1]);
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
internal class SwitchOnFloat : IPeepholeOptimization {
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

        AnnotatedBytecodeInstruction firstInstruction = Unsafe.As<AnnotatedBytecodeInstruction>(input[index]);
        AnnotatedBytecodeInstruction secondInstruction = Unsafe.As<AnnotatedBytecodeInstruction>(input[index + 1]);
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
internal class SwitchOnString : IPeepholeOptimization {
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

        AnnotatedBytecodeInstruction firstInstruction = Unsafe.As<AnnotatedBytecodeInstruction>(input[index]);
        AnnotatedBytecodeInstruction secondInstruction = Unsafe.As<AnnotatedBytecodeInstruction>(input[index + 1]);
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
internal class PushNOfStringFloat : IPeepholeOptimization {
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
            AnnotatedBytecodeInstruction instruction = Unsafe.As<AnnotatedBytecodeInstruction>(input[index + i]);
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
internal class PushNResources : IPeepholeOptimization {
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
            AnnotatedBytecodeInstruction instruction = Unsafe.As<AnnotatedBytecodeInstruction>(input[index + i]);
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
internal class CreateListNFloats : IPeepholeOptimization {
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

        AnnotatedBytecodeInstruction firstInstruction = Unsafe.As<AnnotatedBytecodeInstruction>(input[index]);
        AnnotatedBytecodeInstruction secondInstruction = Unsafe.As<AnnotatedBytecodeInstruction>(input[index + 1]);
        int pushVal1 = ((firstInstruction.GetArgs()[0] as AnnotatedBytecodeInteger)!).Value;
        int pushVal2 = ((secondInstruction.GetArgs()[0] as AnnotatedBytecodeListSize)!).Size;

        return pushVal1 == pushVal2;
    }


    public void Apply(List<IAnnotatedBytecode> input, int index) {
        if (index + 1 >= input.Count) {
            throw new ArgumentOutOfRangeException("Index plus one is outside the bounds of the input list.");
        }

        AnnotatedBytecodeInstruction firstInstruction = Unsafe.As<AnnotatedBytecodeInstruction>(input[index]);
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
internal class CreateListNStrings : IPeepholeOptimization {
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

        AnnotatedBytecodeInstruction firstInstruction = Unsafe.As<AnnotatedBytecodeInstruction>(input[index]);
        AnnotatedBytecodeInstruction secondInstruction = Unsafe.As<AnnotatedBytecodeInstruction>(input[index + 1]);
        int pushVal1 = ((firstInstruction.GetArgs()[0] as AnnotatedBytecodeInteger)!).Value;
        int pushVal2 = ((secondInstruction.GetArgs()[0] as AnnotatedBytecodeListSize)!).Size;

        return pushVal1 == pushVal2;
    }


    public void Apply(List<IAnnotatedBytecode> input, int index) {
        if (index + 1 >= input.Count) {
            throw new ArgumentOutOfRangeException("Index plus one is outside the bounds of the input list.");
        }

        AnnotatedBytecodeInstruction firstInstruction = Unsafe.As<AnnotatedBytecodeInstruction>(input[index]);
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
internal class CreateListNResources : IPeepholeOptimization {
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

        AnnotatedBytecodeInstruction firstInstruction = Unsafe.As<AnnotatedBytecodeInstruction>(input[index]);
        AnnotatedBytecodeInstruction secondInstruction = Unsafe.As<AnnotatedBytecodeInstruction>(input[index + 1]);
        int pushVal1 = ((firstInstruction.GetArgs()[0] as AnnotatedBytecodeInteger)!).Value;
        int pushVal2 = ((secondInstruction.GetArgs()[0] as AnnotatedBytecodeListSize)!).Size;

        return pushVal1 == pushVal2;
    }


    public void Apply(List<IAnnotatedBytecode> input, int index) {
        if (index + 1 >= input.Count) {
            throw new ArgumentOutOfRangeException("Index plus one is outside the bounds of the input list.");
        }

        AnnotatedBytecodeInstruction firstInstruction = Unsafe.As<AnnotatedBytecodeInstruction>(input[index]);
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
internal class CreateListNRefs : IPeepholeOptimization {
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

        var firstInstruction = Unsafe.As<AnnotatedBytecodeInstruction>(input[index]);
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
internal class RemoveJumpFollowedByJump : IPeepholeOptimization {
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
internal class IsTypeDirect : IPeepholeOptimization {
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

        var firstInstruction = Unsafe.As<AnnotatedBytecodeInstruction>(input[index]);
        AnnotatedBytecodeTypeID? pushVal = (firstInstruction.GetArgs()[0] as AnnotatedBytecodeTypeID);

        input.RemoveRange(index, 2);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.IsTypeDirect,
            new List<IAnnotatedBytecode> { pushVal }));
    }
}
