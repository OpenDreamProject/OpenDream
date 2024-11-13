using System.Linq;
using DMCompiler.Bytecode;
using DMCompiler.Compiler;

namespace DMCompiler.DM.Expressions;

// x() (only the identifier)
internal sealed class Proc(DMCompiler compiler, Location location, string identifier) : DMExpression(compiler, location) {
    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        compiler.Emit(WarningCode.BadExpression, Location, "attempt to use proc as value");
        proc.Error();
    }

    public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        if (dmObject.HasProc(identifier)) {
            return DMReference.CreateSrcProc(identifier);
        } else if (Compiler.DMObjectTree.TryGetGlobalProc(identifier, out var globalProc)) {
            return DMReference.CreateGlobalProc(globalProc.Id);
        }

        compiler.Emit(WarningCode.ItemDoesntExist, Location, $"Type {dmObject.Path} does not have a proc named \"{identifier}\"");
        //Just... pretend there is one for the sake of argument.
        return DMReference.CreateSrcProc(identifier);
    }

    public DMProc? GetProc(DMObject dmObject) {
        var procId = dmObject.GetProcs(identifier)?[^1];
        return procId is null ? null : Compiler.DMObjectTree.AllProcs[procId.Value];
    }

    public DMComplexValueType GetReturnType(DMObject dmObject) {
        return dmObject.GetReturnType(identifier);
    }
}

internal sealed class GlobalProc(DMCompiler compiler, Location location, DMProc proc) : DMExpression(compiler, location) {
    public override DMComplexValueType ValType => Proc.ReturnTypes;

    public DMProc Proc => proc;

    public override string ToString() {
        return $"{proc.Name}()";
    }

    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        compiler.Emit(WarningCode.InvalidReference, Location, $"Attempt to use proc \"{this}\" as value");
    }

    public override DMReference EmitReference(DMObject dmObject, DMProc callingProc, string endLabel, ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        return DMReference.CreateGlobalProc(Proc.Id);
    }
}

/// <summary>
/// . <br/>
/// This is an LValue _and_ a proc!
/// </summary>
internal sealed class ProcSelf(DMCompiler compiler, Location location, DreamPath? path, DMProc proc) : LValue(compiler, location, path) {
    public override DMComplexValueType ValType => proc.ReturnTypes;

    public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        return DMReference.Self;
    }
}

// ..
internal sealed class ProcSuper(DMCompiler compiler, Location location, DMObject _dmObject, DMProc _proc) : DMExpression(compiler, location) {
    public override DMComplexValueType ValType => _dmObject.GetProcReturnTypes(_proc.Name) ?? DMValueType.Anything;

    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        compiler.Emit(WarningCode.InvalidReference, Location, $"Attempt to use proc \"..\" as value");
    }

    public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        if ((proc.Attributes & ProcAttributes.IsOverride) != ProcAttributes.IsOverride) {
            // Don't emit if lateral proc overrides exist
            if (dmObject.GetProcs(proc.Name)!.Count == 1) {
                compiler.Emit(WarningCode.PointlessParentCall, Location,
                    "Calling parents via ..() in a proc definition does nothing");
            }
        }

        return DMReference.SuperProc;
    }
}

// x(y, z, ...)
internal sealed class ProcCall(Location location, DMExpression target, ArgumentList arguments, DMComplexValueType valType)
    : DMExpression(target.Compiler, location) {
    public override bool PathIsFuzzy => Path == null;
    public override DMComplexValueType ValType => valType.IsAnything ? target.ValType : valType;

    public (DMObject? ProcOwner, DMProc? Proc) GetTargetProc(DMObject dmObject) {
        return target switch {
            Proc procTarget => (dmObject, procTarget.GetProc(dmObject)),
            GlobalProc procTarget => (null, procTarget.Proc),
            _ => (null, null)
        };
    }

    public override string ToString() {
        return target.ToString()!;
    }

    public override void EmitPushValue(DMObject dmObject, DMProc proc) {
        (DMObject? procOwner, DMProc? targetProc) = GetTargetProc(dmObject);
        DoCompileTimeLinting(procOwner, targetProc);
        if ((targetProc?.Attributes & ProcAttributes.Unimplemented) == ProcAttributes.Unimplemented) {
            targetProc!.Compiler.UnimplementedWarning(Location, $"{procOwner?.Path.ToString() ?? "/"}.{targetProc.Name}() is not implemented");
        }

        string endLabel = proc.NewLabelName();

        DMCallArgumentsType argumentsType;
        int argumentStackSize;
        if (arguments.Length == 0 && target is ProcSuper) {
            argumentsType = DMCallArgumentsType.FromProcArguments;
            argumentStackSize = 0;
        } else {
            (argumentsType, argumentStackSize) = arguments.EmitArguments(dmObject, proc, targetProc);
        }

        DMReference procRef = target.EmitReference(dmObject, proc, endLabel);

        proc.Call(procRef, argumentsType, argumentStackSize);
        proc.AddLabel(endLabel);
    }

    /// <summary>
    /// This is a good place to do some compile-time linting of any native procs that require it,
    /// such as native procs that check ahead of time if the number of arguments is correct (like matrix() or sin())
    /// </summary>
    private void DoCompileTimeLinting(DMObject? procOwner, DMProc? targetProc) {
        if(procOwner is null || procOwner.Path == DreamPath.Root) {
            if (targetProc is null)
                return;
            if(targetProc.Name == "matrix") {
                switch(arguments.Length) {
                    case 0:
                    case 1: // NOTE: 'case 1' also ends up referring to the arglist situation. FIXME: Make this lint work for that, too?
                    case 6:
                        break; // Normal cases
                    case 2:
                    case 3: // These imply that they're trying to use the undocumented matrix signatures.
                    case 4: // The lint is to just check that the last argument is a numeric constant that is a valid matrix "opcode."
                        var lastArg = arguments.Expressions.Last().Expr;
                        if(lastArg.TryAsConstant(out var constant)) {
                            if(constant is not Number opcodeNumber) {
                                targetProc.Compiler.Emit(WarningCode.SuspiciousMatrixCall, arguments.Location,
                                    "Arguments for matrix() are invalid - either opcode is invalid or not enough arguments");
                                break;
                            }

                            //Note that it is possible for the numeric value to not be an opcode itself,
                            //but the call is still valid.
                            //This is because of MATRIX_MODIFY; things like MATRIX_INVERT | MATRIX_MODIFY are okay!
                            const int notModifyBits = ~(int)MatrixOpcode.Modify;
                            if (!Enum.IsDefined((MatrixOpcode) ((int)opcodeNumber.Value & notModifyBits))) {
                                //NOTE: This still does let some certain weird opcodes through,
                                //like a MODIFY with no other operation present.
                                //Not sure if that is a parity behaviour or not!
                                targetProc.Compiler.Emit(WarningCode.SuspiciousMatrixCall, arguments.Location,
                                    "Arguments for matrix() are invalid - either opcode is invalid or not enough arguments");
                            }
                        }

                        break;
                    case 5: // BYOND always runtimes but DOES compile, here
                        targetProc.Compiler.Emit(WarningCode.SuspiciousMatrixCall, arguments.Location,
                            $"Calling matrix() with 5 arguments will always error when called at runtime");
                        break;
                    default: // BYOND always compiletimes here
                        targetProc.Compiler.Emit(WarningCode.InvalidArgumentCount, arguments.Location,
                            $"Too many arguments to matrix() - got {arguments.Length} arguments, expecting 6 or less");
                        break;
                }
            }
        }
    }

    public override bool TryAsJsonRepresentation(out object? json) {
        json = null;
        target.Compiler.UnimplementedWarning(Location, $"DMM overrides for expression {GetType()} are not implemented");
        return true; //TODO
    }
}
