using System;
using DMCompiler.DM;

namespace DMCompiler.Compiler.DM.AST;

public abstract class DMASTNode(Location location) {
    public readonly Location Location = location;
}

public sealed class DMASTFile(Location location, DMASTBlockInner blockInner) : DMASTNode(location) {
    public readonly DMASTBlockInner BlockInner = blockInner;
}

public sealed class DMASTBlockInner(Location location, DMASTStatement[] statements) : DMASTNode(location) {
    public readonly DMASTStatement[] Statements = statements;
}

public sealed class DMASTProcBlockInner : DMASTNode {
    public readonly DMASTProcStatement[] Statements;

    /// <remarks>
    /// SetStatements is held separately because all set statements need to be, to borrow cursed JS terms, "hoisted" to the top of the block, before anything else.<br/>
    /// This isn't SPECIFICALLY a <see cref="DMASTProcStatementSet"/> array because some of these may be DMASTAggregate instances.
    /// </remarks>
    public readonly DMASTProcStatement[] SetStatements;

    /// <summary> Initializes an empty block. </summary>
    public DMASTProcBlockInner(Location location) : base(location) {
        Statements = Array.Empty<DMASTProcStatement>();
        SetStatements = Array.Empty<DMASTProcStatement>();
    }

    /// <summary> Initializes a block with only one statement (which may be a <see cref="DMASTProcStatementSet"/> :o) </summary>
    public DMASTProcBlockInner(Location location, DMASTProcStatement statement) : base(location) {
        if (statement.IsAggregateOr<DMASTProcStatementSet>()) {
            // If this is a Set statement or a set of Set statements
            Statements = Array.Empty<DMASTProcStatement>();
            SetStatements = new[] { statement };
        } else {
            Statements = new[] { statement };
            SetStatements = Array.Empty<DMASTProcStatement>();
        }
    }

    public DMASTProcBlockInner(Location location, DMASTProcStatement[] statements,
        DMASTProcStatement[]? setStatements)
        : base(location) {
        Statements = statements;
        SetStatements = setStatements ?? Array.Empty<DMASTProcStatement>();
    }
}

// TODO: This can probably be replaced with a DreamPath nullable
public sealed class DMASTPath(Location location, DreamPath path, bool operatorFlag = false) : DMASTNode(location) {
    public DreamPath Path = path;
    public readonly bool IsOperator = operatorFlag;
}

public sealed class DMASTCallParameter(Location location, DMASTExpression value, DMASTExpression? key = null)
    : DMASTNode(location) {
    public DMASTExpression Value = value;
    public readonly DMASTExpression? Key = key;
}

public sealed class DMASTDefinitionParameter(
    Location location,
    DMASTPath astPath,
    DMASTExpression? value,
    DMValueType? type,
    DMASTExpression possibleValues) : DMASTNode(location) {
    public DreamPath? ObjectType => _paramDecl.IsList ? DreamPath.List : _paramDecl.TypePath;
    public string Name => _paramDecl.VarName;
    public DMASTExpression? Value = value;
    public readonly DMValueType? Type = type;
    public DMASTExpression PossibleValues = possibleValues;

    private readonly ProcParameterDeclInfo _paramDecl = new(astPath.Path);
}
