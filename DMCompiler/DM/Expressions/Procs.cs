using System.Linq;
using DMCompiler.Bytecode;
using DMCompiler.Compiler;

namespace DMCompiler.DM.Expressions;

// x() (only the identifier)
internal sealed class Proc(Location location, string identifier, DMObject theObject) : DMExpression(location) {
    public DMObject dmObject => theObject;
    public string Identifier => identifier;
    public override DMComplexValueType ValType => GetReturnType(theObject);
    public override void EmitPushValue(ExpressionContext ctx) {
        ctx.Compiler.Emit(WarningCode.BadExpression, Location, "attempt to use proc as value");
        ctx.Proc.Error();
    }

    public override DMReference EmitReference(ExpressionContext ctx, string endLabel,
        ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        if (ctx.Type.HasProc(identifier)) {
            return DMReference.CreateSrcProc(identifier);
        } else if (ctx.ObjectTree.TryGetGlobalProc(identifier, out var globalProc)) {
            return DMReference.CreateGlobalProc(globalProc.Id);
        }

        ctx.Compiler.Emit(WarningCode.ItemDoesntExist, Location, $"Type {ctx.Type.Path} does not have a proc named \"{identifier}\"");
        //Just... pretend there is one for the sake of argument.
        return DMReference.CreateSrcProc(identifier);
    }

    public DMProc? GetProc(DMCompiler compiler, DMObject dmObject) {
        var procId = dmObject.GetProcs(identifier)?[^1];
        return procId is null ? null : compiler.DMObjectTree.AllProcs[procId.Value];
    }

    public DMComplexValueType GetReturnType(DMObject dmObject) {
        return dmObject.GetReturnType(identifier);
    }
}

internal sealed class GlobalProc(Location location, DMProc globalProc) : DMExpression(location) {
    public override DMComplexValueType ValType => Proc.ReturnTypes;

    public DMProc Proc => globalProc;

    public override string ToString() {
        return $"{globalProc.Name}()";
    }

    public override void EmitPushValue(ExpressionContext ctx) {
        ctx.Compiler.Emit(WarningCode.InvalidReference, Location, $"Attempt to use proc \"{this}\" as value");
    }

    public override DMReference EmitReference(ExpressionContext ctx, string endLabel,
        ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        return DMReference.CreateGlobalProc(Proc.Id);
    }
}

/// <summary>
/// . <br/>
/// This is an LValue _and_ a proc!
/// </summary>
internal sealed class ProcSelf(Location location, DMComplexValueType? valType) : LValue(location, null) {
    public override DMComplexValueType ValType => valType ?? DMValueType.Anything;

    public override DMReference EmitReference(ExpressionContext ctx, string endLabel,
        ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        return DMReference.Self;
    }
}

// ..
internal sealed class ProcSuper(Location location, DMComplexValueType? valType) : DMExpression(location) {
    public override DMComplexValueType ValType => valType ?? DMValueType.Anything;

    public override void EmitPushValue(ExpressionContext ctx) {
        ctx.Compiler.Emit(WarningCode.InvalidReference, Location, $"Attempt to use proc \"..\" as value");
    }

    public override DMReference EmitReference(ExpressionContext ctx, string endLabel, ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        if ((ctx.Proc.Attributes & ProcAttributes.IsOverride) != ProcAttributes.IsOverride) {
            // Don't emit if lateral proc overrides exist
            if (ctx.Type.GetProcs(ctx.Proc.Name)!.Count == 1) {
                ctx.Compiler.Emit(WarningCode.PointlessParentCall, Location,
                    "Calling parents via ..() in a proc definition does nothing");
            }
        }

        return DMReference.SuperProc;
    }
}

// x(y, z, ...)
internal sealed class ProcCall(DMCompiler compiler, Location location, DMExpression target, ArgumentList arguments, DMComplexValueType valType)
    : DMExpression(location) {
    public override bool PathIsFuzzy => Path == null;
    public override DMComplexValueType ValType {
        get {
            if (!valType.IsAnything)
                return valType;
            switch (target) {
                case Proc procTarget:
                    return procTarget.dmObject.GetProcReturnTypes(procTarget.Identifier) ?? DMValueType.Anything;
                case GlobalProc procTarget:
                    if(compiler.DMObjectTree.TryGetGlobalProc(procTarget.Proc.Name, out var globalProc))
                        return globalProc.RawReturnTypes ?? DMValueType.Anything;
                    return DMValueType.Anything;
            }
            return target.ValType;
        }
    }

    public (DMObject? ProcOwner, DMProc? Proc) GetTargetProc(DMCompiler compiler, DMObject dmObject) {
        return target switch {
            Proc procTarget => (dmObject, procTarget.GetProc(compiler, dmObject)),
            GlobalProc procTarget => (null, procTarget.Proc),
            _ => (null, null)
        };
    }

    public override string ToString() {
        return target.ToString()!;
    }

    public override void EmitPushValue(ExpressionContext ctx) {
        (DMObject? procOwner, DMProc? targetProc) = GetTargetProc(ctx.Compiler, ctx.Type);
        DoCompileTimeLinting(ctx.Compiler, procOwner, targetProc);
        if ((targetProc?.Attributes & ProcAttributes.Unimplemented) == ProcAttributes.Unimplemented) {
            ctx.Compiler.UnimplementedWarning(Location, $"{procOwner?.Path.ToString() ?? "/"}.{targetProc.Name}() is not implemented");
        }

        string endLabel = ctx.Proc.NewLabelName();

        DMCallArgumentsType argumentsType;
        int argumentStackSize;
        if (arguments.Length == 0 && target is ProcSuper) {
            argumentsType = DMCallArgumentsType.FromProcArguments;
            argumentStackSize = 0;
        } else {
            (argumentsType, argumentStackSize) = arguments.EmitArguments(ctx, targetProc);
        }

        DMReference procRef = target.EmitReference(ctx, endLabel);

        ctx.Proc.Call(procRef, argumentsType, argumentStackSize);
        ctx.Proc.AddLabel(endLabel);
    }

    /// <summary>
    /// This is a good place to do some compile-time linting of any native procs that require it,
    /// such as native procs that check ahead of time if the number of arguments is correct (like matrix() or sin())
    /// </summary>
    private void DoCompileTimeLinting(DMCompiler compiler, DMObject? procOwner, DMProc? targetProc) {
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
                        if(lastArg.TryAsConstant(compiler, out var constant)) {
                            if(constant is not Number opcodeNumber) {
                                compiler.Emit(WarningCode.SuspiciousMatrixCall, arguments.Location,
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
                                compiler.Emit(WarningCode.SuspiciousMatrixCall, arguments.Location,
                                    "Arguments for matrix() are invalid - either opcode is invalid or not enough arguments");
                            }
                        }

                        break;
                    case 5: // BYOND always runtimes but DOES compile, here
                        compiler.Emit(WarningCode.SuspiciousMatrixCall, arguments.Location,
                            "Calling matrix() with 5 arguments will always error when called at runtime");
                        break;
                    default: // BYOND always compiletimes here
                        compiler.Emit(WarningCode.InvalidArgumentCount, arguments.Location,
                            $"Too many arguments to matrix() - got {arguments.Length} arguments, expecting 6 or less");
                        break;
                }
            }
        }
    }

    public override bool TryAsJsonRepresentation(DMCompiler compiler, out object? json) {
        json = null;
        compiler.UnimplementedWarning(Location, $"DMM overrides for expression {GetType()} are not implemented");
        return true; //TODO
    }
}
