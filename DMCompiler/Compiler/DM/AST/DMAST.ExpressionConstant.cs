namespace DMCompiler.Compiler.DM.AST;

public abstract class DMASTExpressionConstant(Location location) : DMASTExpression(location);

public sealed class DMASTConstantInteger(Location location, int value) : DMASTExpressionConstant(location) {
    public readonly int Value = value;
}

public sealed class DMASTConstantFloat(Location location, float value) : DMASTExpressionConstant(location) {
    public readonly float Value = value;
}

public sealed class DMASTConstantString(Location location, string value) : DMASTExpressionConstant(location) {
    public readonly string Value = value;
}

public sealed class DMASTConstantResource(Location location, string path) : DMASTExpressionConstant(location) {
    public readonly string Path = path;
}

public sealed class DMASTConstantNull(Location location) : DMASTExpressionConstant(location);

public sealed class DMASTConstantPath(Location location, DMASTPath value, Dictionary<string, DMASTExpression>? varOverrides) : DMASTExpressionConstant(location) {
    public readonly DMASTPath Value = value;
    public readonly Dictionary<string, DMASTExpression>? VarOverrides = varOverrides;
}

public sealed class DMASTUpwardPathSearch(Location location, DMASTExpressionConstant path, DMASTPath search)
    : DMASTExpressionConstant(location) {
    public readonly DMASTExpressionConstant Path = path;
    public readonly DMASTPath Search = search;
}
