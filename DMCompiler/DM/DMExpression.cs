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

        public DMValueType ValType = DMValueType.Anything;
        public Location Location;

        public DMExpression(Location location) {
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

        public static bool TryConstant(DMObject dmObject, DMProc proc, DMASTExpression expression, out Expressions.Constant constant) {
            var expr = Create(dmObject, proc, expression, null);
            return expr.TryAsConstant(out constant);
        }

        // Attempt to convert this expression into a Constant expression
        public virtual bool TryAsConstant(out Expressions.Constant constant) {
            constant = null;
            return false;
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
            throw new CompileErrorException(Location, $"attempt to reference r-value");
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
                var expr = DMExpression.Create(dmObject, proc, arg.Value, inferredPath);
                Expressions[idx++] = (arg.Name, expr);
            }
        }

        public void EmitPushArguments(DMObject dmObject, DMProc proc) {
            if (Expressions.Length == 0) {
                proc.PushArguments(0);
                return;
            }

            if (Expressions[0].Name == null && Expressions[0].Expr is Expressions.Arglist arglist) {
                if (Expressions.Length != 1) {
                    throw new CompileErrorException(Location, "arglist expression should be the only argument");
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
