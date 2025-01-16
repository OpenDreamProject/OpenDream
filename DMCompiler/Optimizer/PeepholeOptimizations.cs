using DMCompiler.Bytecode;

// ReSharper disable UnusedType.Global

namespace DMCompiler.Optimizer;

// Append [ref]
// Pop
// -> AppendNoPush [ref]
internal sealed class AppendNoPush : IOptimization {
    public OptPass OptimizationPass => OptPass.PeepholeOptimization;

    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.Append,
            DreamProcOpcode.Pop
        ];
    }

    public void Apply(DMCompiler compiler, List<IAnnotatedBytecode> input, int index) {
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
internal sealed class AssignNoPush : IOptimization {
    public OptPass OptimizationPass => OptPass.PeepholeOptimization;

    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.Assign,
            DreamProcOpcode.Pop
        ];
    }

    public void Apply(DMCompiler compiler, List<IAnnotatedBytecode> input, int index) {
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
internal sealed class AssignNull : IOptimization {
    public OptPass OptimizationPass => OptPass.PeepholeOptimization;

    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushNull,
            DreamProcOpcode.AssignNoPush
        ];
    }

    public void Apply(DMCompiler compiler, List<IAnnotatedBytecode> input, int index) {
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
internal sealed class PushField : IOptimization {
    public OptPass OptimizationPass => OptPass.PeepholeOptimization;

    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushReferenceValue,
            DreamProcOpcode.DereferenceField
        ];
    }

    public void Apply(DMCompiler compiler, List<IAnnotatedBytecode> input, int index) {
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
// PushString [string]
// DereferenceIndex
// -> IndexRefWithString [ref, string]
internal sealed class IndexRefWithString : IOptimization {
    public OptPass OptimizationPass => OptPass.PeepholeOptimization;

    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushReferenceValue,
            DreamProcOpcode.PushString,
            DreamProcOpcode.DereferenceIndex
        ];
    }

    public void Apply(DMCompiler compiler, List<IAnnotatedBytecode> input, int index) {
        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        AnnotatedBytecodeReference pushVal = firstInstruction.GetArg<AnnotatedBytecodeReference>(0);

        AnnotatedBytecodeInstruction secondInstruction = (AnnotatedBytecodeInstruction)(input[index + 1]);
        AnnotatedBytecodeString strIndex = secondInstruction.GetArg<AnnotatedBytecodeString>(0);

        input.RemoveRange(index, 3);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.IndexRefWithString, -1,
            [pushVal, strIndex]));
    }
}

// PushReferenceValue [ref]
// Return
// -> ReturnReferenceValue [ref]
internal class ReturnReferenceValue : IOptimization {
    public OptPass OptimizationPass => OptPass.PeepholeOptimization;

    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushReferenceValue,
            DreamProcOpcode.Return
        ];
    }

    public void Apply(DMCompiler compiler, List<IAnnotatedBytecode> input, int index) {
        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        AnnotatedBytecodeReference pushVal = firstInstruction.GetArg<AnnotatedBytecodeReference>(0);
        input.RemoveRange(index, 2);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.ReturnReferenceValue, [pushVal]));
    }
}

// PushFloat [float]
// Return
// -> ReturnFloat [float]
internal class ReturnFloat : IOptimization {
    public OptPass OptimizationPass => OptPass.PeepholeOptimization;

    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.Return
        ];
    }

    public void Apply(DMCompiler compiler, List<IAnnotatedBytecode> input, int index) {
        var firstInstruction = IOptimization.GetInstructionAndValue(input[index], out var pushVal);
        IOptimization.ReplaceInstructions(input, index, 2,
            new AnnotatedBytecodeInstruction(DreamProcOpcode.ReturnFloat, [new AnnotatedBytecodeFloat(pushVal, firstInstruction.Location)]));
    }
}

// PushReferenceValue [ref]
// JumpIfFalse [label]
// -> JumpIfReferenceFalse [ref] [label]
internal sealed class JumpIfReferenceFalse : IOptimization {
    public OptPass OptimizationPass => OptPass.PeepholeOptimization;

    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushReferenceValue,
            DreamProcOpcode.JumpIfFalse
        ];
    }

    public void Apply(DMCompiler compiler, List<IAnnotatedBytecode> input, int index) {
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

// Return
// Jump [label]
// -> Return
internal sealed class RemoveJumpAfterReturn : IOptimization {
    public OptPass OptimizationPass => OptPass.PeepholeOptimization;

    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.Return,
            DreamProcOpcode.Jump
        ];
    }

    public void Apply(DMCompiler compiler, List<IAnnotatedBytecode> input, int index) {
        input.RemoveRange(index + 1, 1);
    }
}

// PushFloat [float]
// SwitchCase [label]
// -> SwitchOnFloat [float] [label]
internal sealed class SwitchOnFloat : IOptimization {
    public OptPass OptimizationPass => OptPass.PeepholeOptimization;

    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.SwitchCase
        ];
    }

    public void Apply(DMCompiler compiler, List<IAnnotatedBytecode> input, int index) {
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
internal sealed class SwitchOnString : IOptimization {
    public OptPass OptimizationPass => OptPass.PeepholeOptimization;

    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushString,
            DreamProcOpcode.SwitchCase
        ];
    }

    public void Apply(DMCompiler compiler, List<IAnnotatedBytecode> input, int index) {
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
internal sealed class RemoveJumpFollowedByJump : IOptimization {
    public OptPass OptimizationPass => OptPass.PeepholeOptimization;

    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.Jump,
            DreamProcOpcode.Jump
        ];
    }

    public void Apply(DMCompiler compiler, List<IAnnotatedBytecode> input, int index) {
        input.RemoveAt(index + 1);
    }
}

// PushType [type]
// IsType
// -> IsTypeDirect [type]
internal sealed class IsTypeDirect : IOptimization {
    public OptPass OptimizationPass => OptPass.PeepholeOptimization;

    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushType,
            DreamProcOpcode.IsType
        ];
    }

    public void Apply(DMCompiler compiler, List<IAnnotatedBytecode> input, int index) {
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
// BitNot
// -> PushFloat [result]
internal sealed class ConstFoldBitNot : IOptimization {
    public OptPass OptimizationPass => OptPass.PeepholeOptimization;

    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.BitNot
        ];
    }

    public void Apply(DMCompiler compiler, List<IAnnotatedBytecode> input, int index) {
        var firstInstruction = IOptimization.GetInstructionAndValue(input[index], out var pushVal1);

        var args = new List<IAnnotatedBytecode>(1) {new AnnotatedBytecodeFloat(((~(int)pushVal1) & 0xFFFFFF), firstInstruction.Location)};

        IOptimization.ReplaceInstructions(input, index, 2,
            new AnnotatedBytecodeInstruction(DreamProcOpcode.PushFloat, 1, args));
    }
}

// PushFloat [constant]
// PushFloat [constant]
// BitOr
// -> PushFloat [result]
internal sealed class ConstFoldBitOr : IOptimization {
    public OptPass OptimizationPass => OptPass.PeepholeOptimization;

    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.BitOr,
        ];
    }

    public void Apply(DMCompiler compiler, List<IAnnotatedBytecode> input, int index) {
        var firstInstruction = IOptimization.GetInstructionAndValue(input[index], out var pushVal1);

        IOptimization.GetInstructionAndValue(input[index + 1], out var pushVal2);

        var args = new List<IAnnotatedBytecode>(1) {new AnnotatedBytecodeFloat(((int)pushVal1 | (int)pushVal2), firstInstruction.Location)};

        IOptimization.ReplaceInstructions(input, index, 3,
            new AnnotatedBytecodeInstruction(DreamProcOpcode.PushFloat, 1, args));
    }
}

// PushFloat [constant]
// PushFloat [constant]
// BitAnd
// -> PushFloat [result]
internal sealed class ConstFoldBitAnd : IOptimization {
    public OptPass OptimizationPass => OptPass.PeepholeOptimization;

    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.BitAnd,
        ];
    }

    public void Apply(DMCompiler compiler, List<IAnnotatedBytecode> input, int index) {
        var firstInstruction = IOptimization.GetInstructionAndValue(input[index], out var pushVal1);

        IOptimization.GetInstructionAndValue(input[index + 1], out var pushVal2);

        var args = new List<IAnnotatedBytecode>(1) {new AnnotatedBytecodeFloat(((int)pushVal1 & (int)pushVal2), firstInstruction.Location)};

        IOptimization.ReplaceInstructions(input, index, 3,
            new AnnotatedBytecodeInstruction(DreamProcOpcode.PushFloat, 1, args));
    }
}

// PushFloat [constant]
// PushFloat [constant]
// Multiply
// -> PushFloat [result]
internal sealed class ConstFoldMultiply : IOptimization {
    public OptPass OptimizationPass => OptPass.PeepholeOptimization;

    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.Multiply,
        ];
    }

    public void Apply(DMCompiler compiler, List<IAnnotatedBytecode> input, int index) {
        var firstInstruction = IOptimization.GetInstructionAndValue(input[index], out var pushVal1);

        IOptimization.GetInstructionAndValue(input[index + 1], out var pushVal2);

        var args = new List<IAnnotatedBytecode>(1) {new AnnotatedBytecodeFloat(pushVal1 * pushVal2, firstInstruction.Location)};

        IOptimization.ReplaceInstructions(input, index, 3,
            new AnnotatedBytecodeInstruction(DreamProcOpcode.PushFloat, 1, args));
    }
}

// PushFloat [constant]
// PushFloat [constant]
// Divide
// -> PushFloat [result]
internal sealed class ConstFoldDivide : IOptimization {
    public OptPass OptimizationPass => OptPass.PeepholeOptimization;

    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.Divide,
        ];
    }

    public void Apply(DMCompiler compiler, List<IAnnotatedBytecode> input, int index) {
        var firstInstruction = IOptimization.GetInstructionAndValue(input[index], out var pushVal1);

        IOptimization.GetInstructionAndValue(input[index + 1], out var pushVal2);

        // At runtime, given "A / B" we pop B then A
        // In the peephole optimizer, index is "A", index+1 is "B"
        var args = new List<IAnnotatedBytecode>(1) {new AnnotatedBytecodeFloat(pushVal1 / pushVal2, firstInstruction.Location)};

        IOptimization.ReplaceInstructions(input, index, 3,
            new AnnotatedBytecodeInstruction(DreamProcOpcode.PushFloat, 1, args));
    }
}

// PushFloat [constant]
// PushFloat [constant]
// Add
// -> PushFloat [result]
internal sealed class ConstFoldAdd : IOptimization {
    public OptPass OptimizationPass => OptPass.PeepholeOptimization;

    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.Add,
        ];
    }

    public void Apply(DMCompiler compiler, List<IAnnotatedBytecode> input, int index) {
        var firstInstruction = IOptimization.GetInstructionAndValue(input[index], out var pushVal1);

        IOptimization.GetInstructionAndValue(input[index + 1], out var pushVal2);

        var args = new List<IAnnotatedBytecode>(1) {new AnnotatedBytecodeFloat(pushVal1 + pushVal2, firstInstruction.Location)};

        IOptimization.ReplaceInstructions(input, index, 3,
            new AnnotatedBytecodeInstruction(DreamProcOpcode.PushFloat, 1, args));
    }
}

// PushString [constant]
// PushString [constant]
// Add
// -> PushString [result]
internal sealed class ConstFoldAddStrings : IOptimization {
    public OptPass OptimizationPass => OptPass.PeepholeOptimization;

    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushString,
            DreamProcOpcode.PushString,
            DreamProcOpcode.Add,
        ];
    }

    public void Apply(DMCompiler compiler, List<IAnnotatedBytecode> input, int index) {
        var firstInstruction = (AnnotatedBytecodeInstruction)input[index];
        var firstString = firstInstruction.GetArg<AnnotatedBytecodeString>(0);
        var secondString = ((AnnotatedBytecodeInstruction)input[index+1]).GetArg<AnnotatedBytecodeString>(0);

        var combinedId = compiler.DMObjectTree.AddString(firstString.ResolveString(compiler) + secondString.ResolveString(compiler)); // TODO: Currently doesn't handle removing strings from the string tree that have no other references

        var args = new List<IAnnotatedBytecode>(1) {new AnnotatedBytecodeString(combinedId, firstInstruction.Location)};

        IOptimization.ReplaceInstructions(input, index, 3,
            new AnnotatedBytecodeInstruction(DreamProcOpcode.PushString, 1, args));
    }
}

// PushFloat [constant]
// PushFloat [constant]
// Subtract
// -> PushFloat [result]
internal sealed class ConstFoldSubtract : IOptimization {
    public OptPass OptimizationPass => OptPass.PeepholeOptimization;

    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.Subtract,
        ];
    }

    public void Apply(DMCompiler compiler, List<IAnnotatedBytecode> input, int index) {
        var firstInstruction = IOptimization.GetInstructionAndValue(input[index], out var pushVal1);

        IOptimization.GetInstructionAndValue(input[index + 1], out var pushVal2);

        // At runtime, given "A - B" we pop B then A
        // In the peephole optimizer, index is "A", index+1 is "B"
        var args = new List<IAnnotatedBytecode>(1) {new AnnotatedBytecodeFloat(pushVal1 - pushVal2, firstInstruction.Location)};

        IOptimization.ReplaceInstructions(input, index, 3,
            new AnnotatedBytecodeInstruction(DreamProcOpcode.PushFloat, 1, args));
    }
}

// PushFloat [constant]
// PushFloat [constant]
// Modulus
// -> PushFloat [result]
internal sealed class ConstFoldModulus : IOptimization {
    public OptPass OptimizationPass => OptPass.PeepholeOptimization;

    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.Modulus,
        ];
    }

    public void Apply(DMCompiler compiler, List<IAnnotatedBytecode> input, int index) {
        var firstInstruction = IOptimization.GetInstructionAndValue(input[index], out var pushVal1);

        IOptimization.GetInstructionAndValue(input[index + 1], out var pushVal2);

        // At runtime, given "A % B" we pop B then A
        // In the peephole optimizer, index is "A", index+1 is "B"
        var args = new List<IAnnotatedBytecode>(1) {new AnnotatedBytecodeFloat((int)pushVal1 % (int)pushVal2, firstInstruction.Location)};

        IOptimization.ReplaceInstructions(input, index, 3,
            new AnnotatedBytecodeInstruction(DreamProcOpcode.PushFloat, 1, args));
    }
}

// PushFloat [constant]
// PushFloat [constant]
// Power
// -> PushFloat [result]
internal sealed class ConstFoldPower : IOptimization {
    public OptPass OptimizationPass => OptPass.PeepholeOptimization;

    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.Power,
        ];
    }

    public void Apply(DMCompiler compiler, List<IAnnotatedBytecode> input, int index) {
        var firstInstruction = IOptimization.GetInstructionAndValue(input[index], out var pushVal1);

        IOptimization.GetInstructionAndValue(input[index + 1], out var pushVal2);

        // At runtime, given "A ** B" we pop B then A
        // In the peephole optimizer, index is "A", index+1 is "B"
        var args = new List<IAnnotatedBytecode>(1) {new AnnotatedBytecodeFloat(MathF.Pow(pushVal1, pushVal2), firstInstruction.Location)};

        IOptimization.ReplaceInstructions(input, index, 3,
            new AnnotatedBytecodeInstruction(DreamProcOpcode.PushFloat, 1, args));
    }
}

// AssignNoPush [ref]
// PushReferenceValue [ref]
// -> Assign [ref]
// These opcodes can be reduced to a single Assign as long as the [ref]s are the same
internal sealed class AssignAndPushReferenceValue : IOptimization {
    public OptPass OptimizationPass => OptPass.PeepholeOptimization;

    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.AssignNoPush,
            DreamProcOpcode.PushReferenceValue
        ];
    }

    /// <summary>
    /// We can only apply this optimization if both opcodes refer to the same reference
    /// </summary>
    public bool CheckPreconditions(List<IAnnotatedBytecode> input, int index) {
        if (index + 1 >= input.Count) {
            throw new ArgumentOutOfRangeException(nameof(index), "Index plus one is outside the bounds of the input list.");
        }

        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        AnnotatedBytecodeInstruction secondInstruction = (AnnotatedBytecodeInstruction)(input[index + 1]);

        AnnotatedBytecodeReference assignTarget = firstInstruction.GetArg<AnnotatedBytecodeReference>(0);
        AnnotatedBytecodeReference pushTarget = secondInstruction.GetArg<AnnotatedBytecodeReference>(0);

        return assignTarget.Equals(pushTarget);
    }

    public void Apply(DMCompiler compiler, List<IAnnotatedBytecode> input, int index) {
        // We check the input bounds in CheckPreconditions, so we can skip doing it again here

        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        AnnotatedBytecodeReference assignTarget = firstInstruction.GetArg<AnnotatedBytecodeReference>(0);

        input.RemoveRange(index, 2);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.Assign, [assignTarget]));
    }
}

// AppendNoPush [ref]
// PushReferenceValue [ref]
// -> Append [ref]
// These opcodes can be reduced to a single Append as long as the [ref]s are the same
internal sealed class AppendAndPushReferenceValue : IOptimization {
    public OptPass OptimizationPass => OptPass.PeepholeOptimization;

    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.AppendNoPush,
            DreamProcOpcode.PushReferenceValue
        ];
    }

    /// <summary>
    /// We can only apply this optimization if both opcodes refer to the same reference
    /// </summary>
    public bool CheckPreconditions(List<IAnnotatedBytecode> input, int index) {
        if (index + 1 >= input.Count) {
            throw new ArgumentOutOfRangeException(nameof(index), "Index plus one is outside the bounds of the input list.");
        }

        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        AnnotatedBytecodeInstruction secondInstruction = (AnnotatedBytecodeInstruction)(input[index + 1]);

        AnnotatedBytecodeReference appendTarget = firstInstruction.GetArg<AnnotatedBytecodeReference>(0);
        AnnotatedBytecodeReference pushTarget = secondInstruction.GetArg<AnnotatedBytecodeReference>(0);

        return appendTarget.Equals(pushTarget);
    }

    public void Apply(DMCompiler compiler, List<IAnnotatedBytecode> input, int index) {
        // We check the input bounds in CheckPreconditions, so we can skip doing it again here

        AnnotatedBytecodeInstruction firstInstruction = (AnnotatedBytecodeInstruction)(input[index]);
        AnnotatedBytecodeReference appendTarget = firstInstruction.GetArg<AnnotatedBytecodeReference>(0);

        input.RemoveRange(index, 2);
        input.Insert(index, new AnnotatedBytecodeInstruction(DreamProcOpcode.Append, [appendTarget]));
    }
}

// PushFloat [constant]
// PushFloat [constant]
// BitshiftLeft
// -> PushFloat [result]
internal sealed class ConstFoldBitshiftLeft : IOptimization {
    public OptPass OptimizationPass => OptPass.PeepholeOptimization;

    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.BitShiftLeft,
        ];
    }

    public void Apply(DMCompiler compiler, List<IAnnotatedBytecode> input, int index) {
        var firstInstruction = IOptimization.GetInstructionAndValue(input[index], out var pushVal1);
        IOptimization.GetInstructionAndValue(input[index + 1], out var pushVal2);

        // At runtime, given "A << B" we pop B then A
        // In the peephole optimizer, index is "A", index+1 is "B"
        var args = new List<IAnnotatedBytecode>(1) {new AnnotatedBytecodeFloat(((int)pushVal1 << (int)pushVal2), firstInstruction.Location)};

        IOptimization.ReplaceInstructions(input, index, 3,
            new AnnotatedBytecodeInstruction(DreamProcOpcode.PushFloat, 1, args));
    }
}

// PushFloat [constant]
// PushFloat [constant]
// BitshiftRight
// -> PushFloat [result]
internal sealed class ConstFoldBitshiftRight : IOptimization {
    public OptPass OptimizationPass => OptPass.PeepholeOptimization;

    public ReadOnlySpan<DreamProcOpcode> GetOpcodes() {
        return [
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.PushFloat,
            DreamProcOpcode.BitShiftRight,
        ];
    }

    public void Apply(DMCompiler compiler, List<IAnnotatedBytecode> input, int index) {
        var firstInstruction = IOptimization.GetInstructionAndValue(input[index], out var pushVal1);
        IOptimization.GetInstructionAndValue(input[index + 1], out var pushVal2);

        // At runtime, given "A >> B" we pop B then A
        // In the peephole optimizer, index is "A", index+1 is "B"
        var args = new List<IAnnotatedBytecode>(1) {new AnnotatedBytecodeFloat(((int)pushVal1 >> (int)pushVal2), firstInstruction.Location)};

        IOptimization.ReplaceInstructions(input, index, 3,
            new AnnotatedBytecodeInstruction(DreamProcOpcode.PushFloat, 1, args));
    }
}

#endregion
