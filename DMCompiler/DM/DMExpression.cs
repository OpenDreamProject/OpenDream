using System;
using DMCompiler.DM.Visitors;
using OpenDreamShared.Compiler;
using DMCompiler.Compiler.DM;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;
using System.Diagnostics.CodeAnalysis;

namespace DMCompiler.DM {
    abstract class DMExpression {
        public enum ProcPushResult {
            // The emitted code has pushed the proc onto the stack
            Unconditional,

            // The emitted code has pushed either null or the proc onto the stack
            // If null was pushed, any calls to this proc should silently evaluate to null
            Conditional,
        }

        public enum IdentifierPushResult {
            // The emitted code has pushed the identifier onto the stack
            Unconditional,

            // The emitted code has pushed either null or the identifier onto the stack
            // If null was pushed, any assignments to this identifier should silently evaluate to null
            Conditional,
        }

        public Location Location;

        protected DMExpression(Location location) {
            Location = location;
        }

        public static DMExpression Create(DMObject dmObject, DMProc proc, DMASTExpression expression, DreamPath? inferredPath = null) {
            var instance = new DMVisitorExpression(dmObject, proc, inferredPath);
            expression.Visit(instance);
            return instance.Result;
        }

        public static void Emit(DMObject dmObject, DMProc proc, DMASTExpression expression, DreamPath? inferredPath = null) {
            var expr = Create(dmObject, proc, expression, inferredPath);
            expr.EmitPushValue(dmObject, proc);
        }

        public static bool TryConstant(DMObject dmObject, DMProc proc, DMASTExpression expression, out Expressions.Constant? constant) {
            var expr = Create(dmObject, proc, expression);
            return expr.TryAsConstant(out constant);
        }

        // Attempt to convert this expression into a Constant expression
        public virtual bool TryAsConstant([NotNullWhen(true)] out Expressions.Constant? constant) {
            constant = null;
            return false;
        }

        // Attempt to create a json-serializable version of this expression
        public virtual bool TryAsJsonRepresentation(out object? json) {
            json = null;
            return false;
        }

        // Emits code that pushes the result of this expression to the proc's stack
        // May throw if this expression is unable to be pushed to the stack
        public abstract void EmitPushValue(DMObject dmObject, DMProc proc);

        // Emits a reference that is to be used in an opcode that assigns/gets a value
        // May throw if this expression is unable to be referenced
        public virtual (DMReference Reference, bool Conditional) EmitReference(DMObject dmObject, DMProc proc) {
            throw new CompileAbortException(Location, $"Cannot reference r-value");
        }

        public virtual string GetNameof(DMObject dmObject, DMProc proc) {
            throw new CompileAbortException(Location, "nameof: requires a var, proc reference, or type path");
        }

        public virtual DreamPath? Path => null;
    }

    // (a, b, c, ...)
    // This isn't an expression, it's just a helper class for working with argument lists
    class ArgumentList {
        public readonly (string? Name, DMExpression Expr)[] Expressions;
        public int Length => Expressions.Length;
        public Location Location;

        // Whether or not this has named arguments
        private readonly bool _isKeyed;

        public ArgumentList(Location location, DMObject dmObject, DMProc proc, DMASTCallParameter[]? arguments, DreamPath? inferredPath = null) {
            Location = location;
            if (arguments == null) {
                Expressions = Array.Empty<(string?, DMExpression)>();
                return;
            }

            Expressions = new (string?, DMExpression)[arguments.Length];

            int idx = 0;
            foreach(var arg in arguments) {
                var value = DMExpression.Create(dmObject, proc, arg.Value, inferredPath);
                var key = (arg.Key != null) ? DMExpression.Create(dmObject, proc, arg.Key, inferredPath) : null;
                int argIndex = idx++;
                string? name = null;

                switch (key) {
                    case Expressions.String keyStr:
                        name = keyStr.Value;
                        break;
                    case Expressions.Number keyNum:
                        //Replaces an ordered argument
                        argIndex = (int)keyNum.Value;
                        break;
                    case Expressions.Resource _:
                    case Expressions.Path _:
                        //The key becomes the value
                        value = key;
                        break;

                    default:
                        if (key != null) {
                            DMCompiler.Emit(WarningCode.InvalidArgumentKey, key.Location, "Invalid argument key");
                        }

                        break;
                }

                if (name != null)
                    _isKeyed = true;

                Expressions[argIndex] = (name, value);
            }
        }

        public (DMCallArgumentsType Type, int StackSize) EmitArguments(DMObject dmObject, DMProc proc) {
            if (Expressions.Length == 0) {
                return (DMCallArgumentsType.None, 0);
            }

            if (Expressions[0].Expr is Expressions.Arglist arglist) {
                if (Expressions[0].Name != null)
                    DMCompiler.Emit(WarningCode.BadArgument, arglist.Location, "arglist cannot be a named argument");

                arglist.EmitPushArglist(dmObject, proc);
                return (DMCallArgumentsType.FromArgumentList, 1);
            }

            // TODO: Named arguments must come after all ordered arguments
            int stackCount = 0;
            foreach ((string name, DMExpression expr) in Expressions) {
                if (_isKeyed) {
                    if (name != null) {
                        proc.PushString(name);
                    } else {
                        proc.PushNull();
                    }
                }

                expr.EmitPushValue(dmObject, proc);
                stackCount += _isKeyed ? 2 : 1;
            }

            return (_isKeyed ? DMCallArgumentsType.FromStackKeyed : DMCallArgumentsType.FromStack, stackCount);
        }
    }
}
