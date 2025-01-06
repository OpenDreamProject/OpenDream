using DMCompiler.DM;

namespace DMCompiler.Compiler.DM.AST;

/// <summary>
/// A statement used in object definitions (outside of procs)
/// </summary>
public abstract class DMASTStatement(Location location) : DMASTNode(location);

public sealed class DMASTObjectDefinition(Location location, DreamPath path, DMASTBlockInner? innerBlock)
    : DMASTStatement(location) {
    /// <summary> Unlike other Path variables stored by AST nodes, this path is guaranteed to be the real, absolute path of this object definition block. <br/>
    /// That includes any inherited pathing from being tabbed into a different, base definition.
    /// </summary>
    public DreamPath Path = path;

    public readonly DMASTBlockInner? InnerBlock = innerBlock;
}

/// <remarks> Also includes proc overrides; see the <see cref="IsOverride"/> member. Verbs too.</remarks>
public sealed class DMASTProcDefinition : DMASTStatement {
    public readonly DreamPath ObjectPath;
    public readonly string Name;
    public readonly bool IsOverride;
    public readonly bool IsVerb;
    public readonly DMASTDefinitionParameter[] Parameters;
    public readonly DMASTProcBlockInner? Body;
    public readonly DMComplexValueType? ReturnTypes;

    public DMASTProcDefinition(Location location, DreamPath path, DMASTDefinitionParameter[] parameters,
        DMASTProcBlockInner? body, DMComplexValueType? returnType) : base(location) {
        int procElementIndex = path.FindElement("proc");

        if (procElementIndex == -1) {
            procElementIndex = path.FindElement("verb");

            if (procElementIndex != -1) IsVerb = true;
            else IsOverride = true;
        }

        if (procElementIndex != -1) path = path.RemoveElement(procElementIndex);

        ObjectPath = (path.Elements.Length > 1) ? path.FromElements(0, -2) : DreamPath.Root;
        Name = path.LastElement;
        Parameters = parameters;
        Body = body;
        ReturnTypes = returnType;
    }
}

public sealed class DMASTObjectVarDefinition(
    Location location,
    DreamPath path,
    DMASTExpression value,
    DMComplexValueType valType,
    DreamPath? valPath = null) : DMASTStatement(location) {
    /// <summary>The path of the object that we are a property of.</summary>
    public DreamPath ObjectPath => _varDecl.ObjectPath;

    /// <summary>The actual type of the variable itself.</summary>
    public DreamPath? Type => _varDecl.IsList ? DreamPath.List : _varDecl.TypePath;

    public string Name => _varDecl.VarName;
    public DMASTExpression Value = value;

    private readonly ObjVarDeclInfo _varDecl = new(path);

    public bool IsStatic => _varDecl.IsStatic;

    public bool IsConst => _varDecl.IsConst;
    public bool IsFinal => _varDecl.IsFinal;
    public bool IsTmp => _varDecl.IsTmp;

    public readonly DMComplexValueType ValType = valType;
}

public sealed class DMASTMultipleObjectVarDefinitions(Location location, DMASTObjectVarDefinition[] varDefinitions)
    : DMASTStatement(location) {
    public readonly DMASTObjectVarDefinition[] VarDefinitions = varDefinitions;
}

public sealed class DMASTObjectVarOverride : DMASTStatement {
    public readonly DreamPath ObjectPath;
    public readonly string VarName;
    public DMASTExpression Value;

    public DMASTObjectVarOverride(Location location, DreamPath path, DMASTExpression value) : base(location) {
        ObjectPath = path.FromElements(0, -2);
        VarName = path.LastElement;
        Value = value;
    }
}
