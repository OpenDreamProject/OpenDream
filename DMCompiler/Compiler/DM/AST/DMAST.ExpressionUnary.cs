namespace DMCompiler.Compiler.DM.AST;

public class DMASTUnary(Location location, DMASTExpression value) : DMASTExpression(location) {
    public DMASTExpression Value = value;

    public override IEnumerable<DMASTExpression> Leaves() {
        yield return Value;
    }
}

public sealed class DMASTBinaryNot(Location location, DMASTExpression value) : DMASTUnary(location, value);
public sealed class DMASTNot(Location location, DMASTExpression expression) : DMASTUnary(location, expression);
public sealed class DMASTNegate(Location location, DMASTExpression expression) : DMASTUnary(location, expression);
public sealed class DMASTPreIncrement(Location location, DMASTExpression expression) : DMASTUnary(location, expression);
public sealed class DMASTPreDecrement(Location location, DMASTExpression expression) : DMASTUnary(location, expression);
public sealed class DMASTPostIncrement(Location location, DMASTExpression expression) : DMASTUnary(location, expression);
public sealed class DMASTPostDecrement(Location location, DMASTExpression expression) : DMASTUnary(location, expression);
public sealed class DMASTPointerRef(Location location, DMASTExpression expression) : DMASTUnary(location, expression);
public sealed class DMASTPointerDeref(Location location, DMASTExpression expression) : DMASTUnary(location, expression);
public sealed class DMASTSin(Location location, DMASTExpression expression) : DMASTUnary(location, expression);
public sealed class DMASTCos(Location location, DMASTExpression expression) : DMASTUnary(location, expression);
public sealed class DMASTTan(Location location, DMASTExpression expression) : DMASTUnary(location, expression);
public sealed class DMASTArcsin(Location location, DMASTExpression expression) : DMASTUnary(location, expression);
public sealed class DMASTArccos(Location location, DMASTExpression expression) : DMASTUnary(location, expression);
public sealed class DMASTArctan(Location location, DMASTExpression expression) : DMASTUnary(location, expression);
public sealed class DMASTSqrt(Location location, DMASTExpression expression) : DMASTUnary(location, expression);
public sealed class DMASTAbs(Location location, DMASTExpression expression) : DMASTUnary(location, expression);
public sealed class DMASTProb(Location location, DMASTExpression p) : DMASTUnary(location, p);
public sealed class DMASTInitial(Location location, DMASTExpression expression) : DMASTUnary(location, expression);
public sealed class DMASTNameof(Location location, DMASTExpression expression) : DMASTUnary(location, expression);
public sealed class DMASTIsSaved(Location location, DMASTExpression expression) : DMASTUnary(location, expression);
public sealed class DMASTIsNull(Location location, DMASTExpression value) : DMASTUnary(location, value);
public sealed class DMASTLength(Location location, DMASTExpression value) : DMASTUnary(location, value);
public sealed class DMASTImplicitAsType(Location location, DMASTExpression value) : DMASTUnary(location, value);
public sealed class DMASTImplicitIsType(Location location, DMASTExpression value) : DMASTUnary(location, value);

/// <summary>
/// An expression wrapped around parentheses
/// <code>(1 + 1)</code>
/// </summary>
public sealed class DMASTExpressionWrapped(Location location, DMASTExpression expression) : DMASTUnary(location, expression) {
    public override DMASTExpression GetUnwrapped() {
        DMASTExpression expr = Value;
        while (expr is DMASTExpressionWrapped wrapped)
            expr = wrapped.Value;

        return expr;
    }
}
