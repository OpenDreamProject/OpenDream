namespace DMCompiler.Compiler.DM.AST;

public class DMASTBinary(Location location, DMASTExpression lhs, DMASTExpression rhs) : DMASTExpression(location) {
    public DMASTExpression LHS = lhs;
    public DMASTExpression RHS = rhs;

    public override IEnumerable<DMASTExpression> Leaves() {
        yield return LHS;
        yield return RHS;
    }
}

public sealed class DMASTAssign(Location location, DMASTExpression expression, DMASTExpression value) : DMASTBinary(location, expression, value);
public sealed class DMASTAssignInto(Location location, DMASTExpression expression, DMASTExpression value) : DMASTBinary(location, expression, value);
public sealed class DMASTAppend(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTRemove(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTCombine(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTMask(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTLogicalAndAssign(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTLogicalOrAssign(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTMultiplyAssign(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTDivideAssign(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTLeftShiftAssign(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTRightShiftAssign(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTXorAssign(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTModulusAssign(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTModulusModulusAssign(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTOr(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTAnd(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTBinaryAnd(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTBinaryXor(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTBinaryOr(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTLeftShift(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTRightShift(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTEqual(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTNotEqual(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTEquivalent(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTNotEquivalent(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTLessThan(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTLessThanOrEqual(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTGreaterThan(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTGreaterThanOrEqual(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTMultiply(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTDivide(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTModulus(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTModulusModulus(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTPower(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTAdd(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTSubtract(Location location, DMASTExpression a, DMASTExpression b) : DMASTBinary(location, a, b);
public sealed class DMASTArctan2(Location location, DMASTExpression xExpression, DMASTExpression yExpression) : DMASTBinary(location, xExpression, yExpression);
public sealed class DMASTAsType(Location location, DMASTExpression value, DMASTExpression type) : DMASTBinary(location, value, type);
public sealed class DMASTIsType(Location location, DMASTExpression value, DMASTExpression type) : DMASTBinary(location, value, type);
public sealed class DMASTGetStep(Location location, DMASTExpression refValue, DMASTExpression dir) : DMASTBinary(location, refValue, dir);
public sealed class DMASTGetDir(Location location, DMASTExpression loc1, DMASTExpression loc2) : DMASTBinary(location, loc1, loc2);
public sealed class DMASTExpressionIn(Location location, DMASTExpression value, DMASTExpression list) : DMASTBinary(location, value, list);
