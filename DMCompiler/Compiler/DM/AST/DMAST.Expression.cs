using System.Collections.Generic;
using System.Linq;
using DMCompiler.DM;

namespace DMCompiler.Compiler.DM.AST;

public abstract class DMASTExpression(Location location) : DMASTNode(location) {
    public virtual IEnumerable<DMASTExpression> Leaves() {
        yield break;
    }

    /// <summary>
    /// If this is a <see cref="DMASTExpressionWrapped"/>, returns the expression inside.
    /// Returns this expression if not.
    /// </summary>
    public virtual DMASTExpression GetUnwrapped() {
        return this;
    }
}

/// <summary>
/// Used when there was an error parsing an expression
/// </summary>
/// <remarks>Emit an error code before creating!</remarks>
public sealed class DMASTInvalidExpression(Location location) : DMASTExpression(location);

public sealed class DMASTVoid(Location location) : DMASTExpression(location);

public sealed class DMASTIdentifier(Location location, string identifier) : DMASTExpression(location) {
    public readonly string Identifier = identifier;
}

public sealed class DMASTSwitchCaseRange(Location location, DMASTExpression rangeStart, DMASTExpression rangeEnd)
    : DMASTExpression(location) {
    public DMASTExpression RangeStart = rangeStart, RangeEnd = rangeEnd;
}

public sealed class DMASTStringFormat(Location location, string value, DMASTExpression?[] interpolatedValues)
    : DMASTExpression(location) {
    public readonly string Value = value;
    public readonly DMASTExpression?[] InterpolatedValues = interpolatedValues;
}

public sealed class DMASTList(Location location, DMASTCallParameter[] values) : DMASTExpression(location) {
    public readonly DMASTCallParameter[] Values = values;

    public bool AllValuesConstant() {
        return Values.All(
            value => (value is {
                 Key: DMASTExpressionConstant,
                 Value: DMASTExpressionConstant
            })
            ||
            (value is {
                Key: DMASTExpressionConstant,
                Value: DMASTList valueList
            } && valueList.AllValuesConstant())
        );
    }
}

/// <summary>
/// Represents the value of a var defined as <code>var/list/L[1][2][3]</code>
/// </summary>
public sealed class DMASTDimensionalList(Location location, List<DMASTExpression> sizes)
    : DMASTExpression(location) {
    public readonly List<DMASTExpression> Sizes = sizes;
}

public sealed class DMASTAddText(Location location, DMASTCallParameter[] parameters) : DMASTExpression(location) {
    public readonly DMASTCallParameter[] Parameters = parameters;
}

public sealed class DMASTNewList(Location location, DMASTCallParameter[] parameters) : DMASTExpression(location) {
    public readonly DMASTCallParameter[] Parameters = parameters;
}

public sealed class DMASTInput(
    Location location,
    DMASTCallParameter[] parameters,
    DMValueType? types,
    DMASTExpression? list) : DMASTExpression(location) {
    public readonly DMASTCallParameter[] Parameters = parameters;
    public DMValueType? Types = types;
    public readonly DMASTExpression? List = list;
}

public sealed class DMASTLocateCoordinates(
    Location location,
    DMASTExpression x,
    DMASTExpression y,
    DMASTExpression z) : DMASTExpression(location) {
    public readonly DMASTExpression X = x, Y = y, Z = z;
}

public sealed class DMASTLocate(Location location, DMASTExpression? expression, DMASTExpression? container)
    : DMASTExpression(location) {
    public readonly DMASTExpression? Expression = expression;
    public readonly DMASTExpression? Container = container;
}

public sealed class DMASTGradient(Location location, DMASTCallParameter[] parameters) : DMASTExpression(location) {
    public readonly DMASTCallParameter[] Parameters = parameters;
}

public sealed class DMASTPick(Location location, DMASTPick.PickValue[] values) : DMASTExpression(location) {
    public struct PickValue(DMASTExpression? weight, DMASTExpression value) {
        public readonly DMASTExpression? Weight = weight;
        public readonly DMASTExpression Value = value;
    }

    public readonly PickValue[] Values = values;
}

public class DMASTLog(Location location, DMASTExpression expression, DMASTExpression? baseExpression)
    : DMASTExpression(location) {
    public readonly DMASTExpression Expression = expression;
    public readonly DMASTExpression? BaseExpression = baseExpression;
}

public sealed class DMASTCall(
    Location location,
    DMASTCallParameter[] callParameters,
    DMASTCallParameter[] procParameters) : DMASTExpression(location) {
    public readonly DMASTCallParameter[] CallParameters = callParameters, ProcParameters = procParameters;
}

public class DMASTVarDeclExpression(Location location, DMASTPath path) : DMASTExpression(location) {
    public readonly DMASTPath DeclPath = path;
}

public sealed class DMASTNewPath(Location location, DMASTConstantPath path, DMASTCallParameter[]? parameters)
    : DMASTExpression(location) {
    public readonly DMASTConstantPath Path = path;
    public readonly DMASTCallParameter[]? Parameters = parameters;
}

public sealed class DMASTNewExpr(Location location, DMASTExpression expression, DMASTCallParameter[]? parameters)
    : DMASTExpression(location) {
    public readonly DMASTExpression Expression = expression;
    public readonly DMASTCallParameter[]? Parameters = parameters;
}

public sealed class DMASTNewInferred(Location location, DMASTCallParameter[]? parameters)
    : DMASTExpression(location) {
    public readonly DMASTCallParameter[]? Parameters = parameters;
}

public sealed class DMASTTernary(Location location, DMASTExpression a, DMASTExpression b, DMASTExpression c)
    : DMASTExpression(location) {
    public readonly DMASTExpression A = a, B = b, C = c;

    public override IEnumerable<DMASTExpression> Leaves() {
        yield return A;
        yield return B;
        yield return C;
    }
}

public sealed class DMASTExpressionInRange(
    Location location,
    DMASTExpression value,
    DMASTExpression startRange,
    DMASTExpression endRange,
    DMASTExpression? step = null) : DMASTExpression(location) {
    public DMASTExpression Value = value;
    public DMASTExpression StartRange = startRange;
    public DMASTExpression EndRange = endRange;
    public readonly DMASTExpression? Step = step;

    public override IEnumerable<DMASTExpression> Leaves() {
        yield return Value;
        yield return StartRange;
        yield return EndRange;
    }
}

public sealed class DMASTProcCall(Location location, IDMASTCallable callable, DMASTCallParameter[] parameters)
    : DMASTExpression(location) {
    public readonly IDMASTCallable Callable = callable;
    public readonly DMASTCallParameter[] Parameters = parameters;
}

public sealed class DMASTDereference(
    Location location,
    DMASTExpression expression,
    DMASTDereference.Operation[] operations) : DMASTExpression(location) {
    public abstract class Operation {
        /// <summary>
        /// The location of the operation.
        /// </summary>
        public required Location Location;
        /// <summary>
        /// Whether we should short circuit if the expression we are accessing is null.
        /// </summary>
        public required bool Safe; // x?.y, x?.y() etc
    }

    public abstract class NamedOperation : Operation {
        /// <summary>
        /// Name of the identifier.
        /// </summary>
        public required string Identifier;
        /// <summary>
        /// Whether we should check if the variable exists or not.
        /// </summary>
        public required bool NoSearch; // x:y, x:y()
    }

    public sealed class FieldOperation : NamedOperation;

    public sealed class IndexOperation : Operation {
        /// <summary>
        /// The index expression that we use to index this expression (constant or otherwise).
        /// </summary>
        public required DMASTExpression Index; // x[y], x?[y]
    }

    public sealed class CallOperation : NamedOperation {
        /// <summary>
        /// The parameters that we call this proc with.
        /// </summary>
        public required DMASTCallParameter[] Parameters; // x.y(),
    }

    public readonly DMASTExpression Expression = expression;

    // Always contains at least one operation
    public readonly Operation[] Operations = operations;
}

public interface IDMASTCallable;

public sealed class DMASTCallableProcIdentifier(Location location, string identifier) : DMASTExpression(location), IDMASTCallable {
    public readonly string Identifier = identifier;
}

public sealed class DMASTCallableSuper(Location location) : DMASTExpression(location), IDMASTCallable;

public sealed class DMASTCallableSelf(Location location) : DMASTExpression(location), IDMASTCallable;
