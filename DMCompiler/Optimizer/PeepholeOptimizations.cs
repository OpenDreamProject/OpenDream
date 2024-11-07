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
