using System.Text;

namespace OpenDreamClient.Interface.Css;

public interface ICssAst;
public interface ICssExpression;

public sealed class CssRuleset(List<CssSelector> selectors, List<CssDeclaration> declarations) : ICssAst {
    public readonly List<CssSelector> Selectors = selectors;
    public readonly List<CssDeclaration> Declarations = declarations;
}

#region Selectors

public abstract class CssSelector : ICssAst {
    public CssSelector? SubSelector { get; set; }
}

public sealed class CssNameSelector(ReadOnlyMemory<char> name) : CssSelector {
    public readonly ReadOnlyMemory<char> Name = name;

    public override string ToString() => Name.ToString();
}

public sealed class CssIdSelector(ReadOnlyMemory<char> id) : CssSelector {
    public readonly ReadOnlyMemory<char> Id = id;

    public override string ToString() => $"#{Id}";
}

public sealed class CssClassSelector(ReadOnlyMemory<char> @class) : CssSelector {
    public readonly ReadOnlyMemory<char> Class = @class;

    public override string ToString() => $".{Class}";
}

#endregion

public sealed class CssDeclaration(ReadOnlyMemory<char> property, ICssExpression value) : ICssAst {
    public readonly ReadOnlyMemory<char> Property = property;
    public readonly ICssExpression Value = value;

    public override string ToString() {
        return $"{Property}: {Value}";
    }
}

#region Expressions

public sealed class CssString(ReadOnlyMemory<char> value) : ICssExpression {
    public readonly ReadOnlyMemory<char> Value = value;

    public override string ToString() => $"\"{Value}\"";
}

public sealed class CssIdentifier(ReadOnlyMemory<char> value) : ICssExpression {
    public readonly ReadOnlyMemory<char> Value = value;

    public override string ToString() => Value.ToString();
}

public sealed class CssHexColor(ReadOnlyMemory<char> value) : ICssExpression {
    public readonly ReadOnlyMemory<char> Value = value;

    public override string ToString() => Value.ToString();
}

public sealed class CssNumber(ReadOnlyMemory<char> value) : ICssExpression {
    public readonly ReadOnlyMemory<char> Value = value;

    public override string ToString() => Value.ToString();
}

public sealed class CssDimension(ReadOnlyMemory<char> value) : ICssExpression {
    public readonly ReadOnlyMemory<char> Value = value;

    public override string ToString() => Value.ToString();
}

public sealed class CssExpressionGroup(List<ICssExpression> expressions) : ICssExpression {
    public readonly List<ICssExpression> Expressions = expressions;

    public override string ToString() {
        var strBuilder = new StringBuilder();
        foreach (var expr in Expressions) {
            strBuilder.Append(expr);
            strBuilder.Append(' ');
        }

        return strBuilder.ToString();
    }
}

#endregion
