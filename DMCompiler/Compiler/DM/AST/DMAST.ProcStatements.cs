using DMCompiler.DM;

namespace DMCompiler.Compiler.DM.AST;

/// <summary>
/// A statement found within procs
/// </summary>
public abstract class DMASTProcStatement(Location location) : DMASTNode(location) {
    /// <returns>
    /// Returns true if this statement is either T or an aggregation of T (stored by an <see cref="DMASTAggregate{T}"/> instance). False otherwise.
    /// </returns>
    public bool IsAggregateOr<T>() where T : DMASTProcStatement {
        return (this is T or DMASTAggregate<T>);
    }
}

/// Lone semicolon, analogous to null statements in C.
/// Main purpose is to suppress EmptyBlock emissions.
public sealed class DMASTNullProcStatement(Location location) : DMASTProcStatement(location);

/// <summary>
/// Used when there was an error parsing a statement
/// </summary>
/// <remarks>Emit an error code before creating!</remarks>
public sealed class DMASTInvalidProcStatement(Location location) : DMASTProcStatement(location);

public sealed class DMASTProcStatementExpression(Location location, DMASTExpression expression)
    : DMASTProcStatement(location) {
    public DMASTExpression Expression = expression;
}

public sealed class DMASTProcStatementVarDeclaration(Location location, DMASTPath path, DMASTExpression? value, DMComplexValueType valType)
    : DMASTProcStatement(location) {
    public DMASTExpression? Value = value;

    public DreamPath? Type => _varDecl.IsList ? DreamPath.List : _varDecl.TypePath;

    public DMComplexValueType ValType => valType;

    public string Name => _varDecl.VarName;
    public bool IsGlobal => _varDecl.IsStatic;
    public bool IsConst => _varDecl.IsConst;

    private readonly ProcVarDeclInfo _varDecl = new(path.Path);
}

/// <summary>
/// A kinda-abstract class that represents several statements that were created in unison by one "super-statement" <br/>
/// Such as, a var declaration that actually declares several vars at once (which in our parser must become "one" statement, hence this thing)
/// </summary>
/// <typeparam name="T">The DMASTProcStatement-derived class that this AST node holds.</typeparam>
public sealed class DMASTAggregate<T>(Location location, T[] statements) : DMASTProcStatement(location)
    where T : DMASTProcStatement { // Gotta be honest? I like this "where" syntax better than C++20 concepts
    public T[] Statements { get; } = statements;
}

public sealed class DMASTProcStatementReturn(Location location, DMASTExpression? value) : DMASTProcStatement(location) {
    public DMASTExpression? Value = value;
}

public sealed class DMASTProcStatementBreak(Location location, DMASTIdentifier? label = null)
    : DMASTProcStatement(location) {
    public readonly DMASTIdentifier? Label = label;
}

public sealed class DMASTProcStatementContinue(Location location, DMASTIdentifier? label = null)
    : DMASTProcStatement(location) {
    public readonly DMASTIdentifier? Label = label;
}

public sealed class DMASTProcStatementGoto(Location location, DMASTIdentifier label) : DMASTProcStatement(location) {
    public readonly DMASTIdentifier Label = label;
}

public sealed class DMASTProcStatementLabel(Location location, string name, DMASTProcBlockInner? body) : DMASTProcStatement(location) {
    public readonly string Name = name;
    public readonly DMASTProcBlockInner? Body = body;
}

public sealed class DMASTProcStatementDel(Location location, DMASTExpression value) : DMASTProcStatement(location) {
    public DMASTExpression Value = value;
}

public sealed class DMASTProcStatementSet(
    Location location,
    string attribute,
    DMASTExpression value,
    bool wasInKeyword) : DMASTProcStatement(location) {
    public readonly string Attribute = attribute;
    public readonly DMASTExpression Value = value;
    public readonly bool WasInKeyword = wasInKeyword; // Marks whether this was a "set x in y" expression, or a "set x = y" one
}

public sealed class DMASTProcStatementSpawn(Location location, DMASTExpression delay, DMASTProcBlockInner body)
    : DMASTProcStatement(location) {
    public DMASTExpression Delay = delay;
    public readonly DMASTProcBlockInner Body = body;
}

public sealed class DMASTProcStatementIf(
    Location location,
    DMASTExpression condition,
    DMASTProcBlockInner body,
    DMASTProcBlockInner? elseBody = null) : DMASTProcStatement(location) {
    public DMASTExpression Condition = condition;
    public readonly DMASTProcBlockInner Body = body;
    public readonly DMASTProcBlockInner? ElseBody = elseBody;
}

public sealed class DMASTProcStatementFor(
    Location location,
    DMASTExpression? expr1,
    DMASTExpression? expr2,
    DMASTExpression? expr3,
    DMComplexValueType? dmTypes,
    DMASTProcBlockInner body) : DMASTProcStatement(location) {
    public DMASTExpression? Expression1 = expr1, Expression2 = expr2, Expression3 = expr3;
    public DMComplexValueType? DMTypes = dmTypes;
    public readonly DMASTProcBlockInner Body = body;
}

public sealed class DMASTProcStatementInfLoop(Location location, DMASTProcBlockInner body) : DMASTProcStatement(location) {
    public readonly DMASTProcBlockInner Body = body;
}

public sealed class DMASTProcStatementWhile(
    Location location,
    DMASTExpression conditional,
    DMASTProcBlockInner body) : DMASTProcStatement(location) {
    public DMASTExpression Conditional = conditional;
    public readonly DMASTProcBlockInner Body = body;
}

public sealed class DMASTProcStatementDoWhile(
    Location location,
    DMASTExpression conditional,
    DMASTProcBlockInner body) : DMASTProcStatement(location) {
    public DMASTExpression Conditional = conditional;
    public readonly DMASTProcBlockInner Body = body;
}

public sealed class DMASTProcStatementSwitch(
    Location location,
    DMASTExpression value,
    DMASTProcStatementSwitch.SwitchCase[] cases) : DMASTProcStatement(location) {
    public class SwitchCase {
        public readonly DMASTProcBlockInner Body;

        protected SwitchCase(DMASTProcBlockInner body) {
            Body = body;
        }
    }

    public sealed class SwitchCaseDefault(DMASTProcBlockInner body) : SwitchCase(body);

    public sealed class SwitchCaseValues(DMASTExpression[] values, DMASTProcBlockInner body) : SwitchCase(body) {
        public readonly DMASTExpression[] Values = values;
    }

    public DMASTExpression Value = value;
    public readonly SwitchCase[] Cases = cases;
}

public sealed class DMASTProcStatementBrowse(
    Location location,
    DMASTExpression receiver,
    DMASTExpression body,
    DMASTExpression options) : DMASTProcStatement(location) {
    public DMASTExpression Receiver = receiver;
    public DMASTExpression Body = body;
    public DMASTExpression Options = options;
}

public sealed class DMASTProcStatementBrowseResource(
    Location location,
    DMASTExpression receiver,
    DMASTExpression file,
    DMASTExpression filename) : DMASTProcStatement(location) {
    public DMASTExpression Receiver = receiver;
    public DMASTExpression File = file;
    public DMASTExpression Filename = filename;
}

public sealed class DMASTProcStatementOutputControl(
    Location location,
    DMASTExpression receiver,
    DMASTExpression message,
    DMASTExpression control) : DMASTProcStatement(location) {
    public DMASTExpression Receiver = receiver;
    public DMASTExpression Message = message;
    public DMASTExpression Control = control;
}

public sealed class DMASTProcStatementLink(
    Location location,
    DMASTExpression receiver,
    DMASTExpression url) : DMASTProcStatement(location) {
    public readonly DMASTExpression Receiver = receiver;
    public readonly DMASTExpression Url = url;
}

public sealed class DMASTProcStatementFtp(
    Location location,
    DMASTExpression receiver,
    DMASTExpression file,
    DMASTExpression name) : DMASTProcStatement(location) {
    public readonly DMASTExpression Receiver = receiver;
    public readonly DMASTExpression File = file;
    public readonly DMASTExpression Name = name;
}

public sealed class DMASTProcStatementOutput(Location location, DMASTExpression a, DMASTExpression b)
    : DMASTProcStatement(location) {
    public readonly DMASTExpression A = a, B = b;
}

public sealed class DMASTProcStatementInput(Location location, DMASTExpression a, DMASTExpression b)
    : DMASTProcStatement(location) {
    public readonly DMASTExpression A = a, B = b;
}

public sealed class DMASTProcStatementTryCatch(
    Location location,
    DMASTProcBlockInner tryBody,
    DMASTProcBlockInner? catchBody,
    DMASTProcStatement? catchParameter) : DMASTProcStatement(location) {
    public readonly DMASTProcBlockInner TryBody = tryBody;
    public readonly DMASTProcBlockInner? CatchBody = catchBody;
    public readonly DMASTProcStatement? CatchParameter = catchParameter;
}

public sealed class DMASTProcStatementThrow(Location location, DMASTExpression value) : DMASTProcStatement(location) {
    public DMASTExpression Value = value;
}
