using System;
using System.Linq;
using DMCompiler.Bytecode;
using DMCompiler.Compiler;

namespace DMCompiler.DM.Expressions {
    // x() (only the identifier)
    sealed class Proc : DMExpression {
        private readonly string _identifier;

        public Proc(Location location, string identifier) : base(location) {
            _identifier = identifier;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            throw new CompileErrorException(Location, "attempt to use proc as value");
        }

        public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode) {
            if (dmObject.HasProc(_identifier)) {
                return DMReference.CreateSrcProc(_identifier);
            } else if (DMObjectTree.TryGetGlobalProc(_identifier, out var globalProc)) {
                return DMReference.CreateGlobalProc(globalProc.Id);
            }

            DMCompiler.Emit(WarningCode.ItemDoesntExist, Location, $"Type {dmObject.Path} does not have a proc named \"{_identifier}\"");
            //Just... pretend there is one for the sake of argument.
            return DMReference.CreateSrcProc(_identifier);
        }

        public DMProc? GetProc(DMObject dmObject) {
            var procId = dmObject.GetProcs(_identifier)?[^1];
            return procId is null ? null : DMObjectTree.AllProcs[procId.Value];
        }

        public DMValueType GetReturnType(DMObject dmObject) {
            return dmObject.GetReturnType(_identifier);
        }
    }

    /// <remarks>
    /// This doesn't actually contain the GlobalProc itself;
    /// this is just a hopped-up string that we eventually deference to get the real global proc during compilation.
    /// </remarks>
    internal sealed class GlobalProc : DMExpression {
        private readonly string _name;
        public override DMValueType ValType { get; init; }


        public GlobalProc(Location location, string name) : base(location) {
            _name = name;
            ValType = GetReturnType();
        }

        public override string ToString() {
            return $"{_name}()";
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            DMCompiler.Emit(WarningCode.InvalidReference, Location, $"Attempt to use proc \"{_name}\" as value");
        }

        public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode) {
            DMProc globalProc = GetProc();
            return DMReference.CreateGlobalProc(globalProc.Id);
        }

        public DMProc GetProc() {
            if (!DMObjectTree.TryGetGlobalProc(_name, out var globalProc)) {
                DMCompiler.Emit(WarningCode.ItemDoesntExist, Location, $"No global proc named \"{_name}\"");
                return DMObjectTree.GlobalInitProc; // Just give this, who cares
            }

            return globalProc;
        }

        public DMValueType GetReturnType() {
            return GetProc().ReturnTypes;
        }
    }

    /// <summary>
    /// . <br/>
    /// This is an LValue _and_ a proc!
    /// </summary>
    sealed class ProcSelf : LValue {
        public override DMValueType ValType => DMValueType.Anything; //TODO: figure out how to deal with procself static typing

        public ProcSelf(Location location)
            : base(location, null) {}

        public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode) {
            return DMReference.Self;
        }
    }

    // ..
    sealed class ProcSuper : DMExpression {
        public ProcSuper(Location location) : base(location) { }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            DMCompiler.Emit(WarningCode.InvalidReference, Location, $"Attempt to use proc \"..\" as value");
        }

        public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode) {
            if ((proc.Attributes & ProcAttributes.IsOverride) != ProcAttributes.IsOverride)
            {
                DMCompiler.Emit(WarningCode.PointlessParentCall, Location, "Calling parents via ..() in a proc definition does nothing");
            }
            return DMReference.SuperProc;
        }
    }

    // x(y, z, ...)
    sealed class ProcCall(Location location, DMExpression target, ArgumentList arguments, DMValueType valType = DMValueType.Anything) : DMExpression(location) {
        public override bool PathIsFuzzy => Path == null;
        public override DMValueType ValType => valType == DMValueType.Anything ? target.ValType : valType;

        public (DMObject? ProcOwner, DMProc? Proc) GetTargetProc(DMObject dmObject) {
            switch (target) {
                case Proc procTarget: {
                    return (dmObject, procTarget.GetProc(dmObject));
                }

                case GlobalProc procTarget: {
                    return (null, procTarget.GetProc());
                }
                default:
                    return (null, null);
            }
        }

        public override string ToString() {
            return target.ToString()!;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            (DMObject? procOwner, DMProc? targetProc) = GetTargetProc(dmObject);
            DoCompileTimeLinting(procOwner, targetProc);
            if ((targetProc?.Attributes & ProcAttributes.Unimplemented) == ProcAttributes.Unimplemented) {
                DMCompiler.UnimplementedWarning(Location, $"{procOwner?.Path.ToString() ?? "/"}.{targetProc.Name}() is not implemented");
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
                                    DMCompiler.Emit(WarningCode.SuspiciousMatrixCall, arguments.Location,
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
                                    DMCompiler.Emit(WarningCode.SuspiciousMatrixCall, arguments.Location,
                                    "Arguments for matrix() are invalid - either opcode is invalid or not enough arguments");
                                }
                            }
                            break;
                        case 5: // BYOND always runtimes but DOES compile, here
                            DMCompiler.Emit(WarningCode.SuspiciousMatrixCall, arguments.Location,
                                $"Calling matrix() with 5 arguments will always error when called at runtime");
                            break;
                        default: // BYOND always compiletimes here
                            DMCompiler.Emit(WarningCode.TooManyArguments, arguments.Location,
                                $"Too many arguments to matrix() - got {arguments.Length} arguments, expecting 6 or less");
                            break;

                    }
                }
            }
        }

        public override bool TryAsJsonRepresentation(out object? json) {
            json = null;
            DMCompiler.UnimplementedWarning(Location, $"DMM overrides for expression {GetType()} are not implemented");
            return true; //TODO
        }
    }
}
