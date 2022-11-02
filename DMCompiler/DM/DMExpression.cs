using DMCompiler.DM.Visitors;
using OpenDreamShared.Compiler;
using DMCompiler.Compiler.DM;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;
using System.Collections.Generic;

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

        public DMExpression(Location location) {
            Location = location;
        }

        public static DMExpression Create(DMObject dmObject, DMProc proc, DMASTExpression expression, DreamPath? inferredPath = null) {
            var instance = new DMVisitorExpression(dmObject, proc, inferredPath);
            expression.Visit(instance);
            if (instance.Result.TryAsConstant(out var constant)) // This is an entire const-rolling operation.
                return constant;
            return instance.Result;
        }

        /// <summary>
        /// Should be identical to <see cref="Create(DMObject, DMProc, DMASTExpression, DreamPath?)"/>, except it does not attempt constant-folding. <br/>
        /// This should be used very seldomly, and the caller should ensure that the expression still does get const-folded later on in evaluation.
        /// </summary>
        public static DMExpression CreateNoFolding(DMObject dmObject, DMProc proc, DMASTExpression expression, DreamPath? inferredPath = null) {
            var instance = new DMVisitorExpression(dmObject, proc, inferredPath);
            expression.Visit(instance);
            return instance.Result;
        }


        /// <summary>
        /// Rolls <see cref="Create(DMObject, DMProc, DMASTExpression, DreamPath?)"/> and <see cref="EmitPushValue(DMObject, DMProc)"/> into one action.
        /// </summary>
        public static void Emit(DMObject dmObject, DMProc proc, DMASTExpression expression, DreamPath? inferredPath = null) {
            var expr = Create(dmObject, proc, expression, inferredPath);
            expr.EmitPushValue(dmObject, proc);
        }

        /// <summary>
        /// Rolls <see cref="Create(DMObject, DMProc, DMASTExpression, DreamPath?)"/> and <see cref="TryAsConstant(out Expressions.Constant)"/> into one action. <br/>
        /// Tries to create a Constant Expression from the given AST. Tosses it out if it fails to be const
        /// </summary>
        /// <returns>True if the resulting expression was constant, false if not.</returns>
        public static bool TryConstant(DMObject dmObject, DMProc proc, DMASTExpression expression, out Expressions.Constant constant) {
            var expr = Create(dmObject, proc, expression, null);
            return expr.TryAsConstant(out constant);
        }

        /// <summary> Attempt to convert this expression into a Constant expression. </summary>
        public virtual bool TryAsConstant(out Expressions.Constant constant) {
            constant = null;
            return false;
        }

        public bool TryAsConstantWithLocation(out Expressions.Constant constant, Location loc) {
            bool ret = TryAsConstant(out constant);
            if(ret) {
                constant = constant.CopyWithNewLocation(loc);
            }
            return ret;
        }

        // Attempt to create a json-serializable version of this expression
        public virtual bool TryAsJsonRepresentation(out object json) {
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

        public virtual DreamPath? Path => null;
    }

    // (a, b, c, ...)
    // This isn't an expression, it's just a helper class for working with argument lists
    class ArgumentList {
        (string Name, DMExpression Expr)[] Expressions;
        public int Length => Expressions.Length;
        public Location Location;

        public ArgumentList(Location location, DMObject dmObject, DMProc proc, DMASTCallParameter[] arguments, DreamPath? inferredPath = null) {
            Location = location;
            if (arguments == null) {
                Expressions = new (string, DMExpression)[0];
                return;
            }

            Expressions = new (string, DMExpression)[arguments.Length];

            int idx = 0;
            foreach(var arg in arguments) {
                var value = DMExpression.Create(dmObject, proc, arg.Value, inferredPath);
                var key = (arg.Key != null) ? DMExpression.Create(dmObject, proc, arg.Key, inferredPath) : null;
                int argIndex = idx++;
                string name = null;

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

                Expressions[argIndex] = (name, value);
            }
        }

        public void EmitPushArguments(DMObject dmObject, DMProc proc) {
            if (Expressions.Length == 0) {
                proc.PushArguments(0);
                return;
            }

            if (Expressions[0].Name == null && Expressions[0].Expr is Expressions.Arglist arglist) {
                if (Expressions.Length != 1) {
                    DMCompiler.Emit(WarningCode.ArglistOnlyArgument, Location, "arglist expression should be the only argument");
                }

                arglist.EmitPushArglist(dmObject, proc);
                return;
            }

            List<DreamProcOpcodeParameterType> parameterTypes = new List<DreamProcOpcodeParameterType>();
            List<string> parameterNames = new List<string>();

            foreach ((string name, DMExpression expr) in Expressions) {
                expr.EmitPushValue(dmObject, proc);

                if (name != null) {
                    parameterTypes.Add(DreamProcOpcodeParameterType.Named);
                    parameterNames.Add(name);
                } else {
                    parameterTypes.Add(DreamProcOpcodeParameterType.Unnamed);
                }
            }

            proc.PushArguments(Expressions.Length, parameterTypes.ToArray(), parameterNames.ToArray());
        }
    }
}
