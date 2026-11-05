using Token = OpenDreamClient.Interface.Css.CssLexer.Token;
using TokenType = OpenDreamClient.Interface.Css.CssLexer.TokenType;

namespace OpenDreamClient.Interface.Css;

// https://www.w3.org/TR/CSS2/grammar.html used for reference
public sealed class CssParser(CssLexer lexer) {
    private Token _current = lexer.GetNextToken();

    public List<CssRuleset> Stylesheet() {
        var rulesets = new List<CssRuleset>();

        while (true) {
            Whitespace();

            // TODO: Media(), Page()
            if (Ruleset() is { } ruleset) {
                rulesets.Add(ruleset);
            } else {
                break;
            }
        }

        return rulesets;
    }

    // selector { property: expression }
    private CssRuleset? Ruleset() {
        var selector = Selector();
        if (selector is null)
            return null;

        var selectors = new List<CssSelector>();
        while (true) {
            selectors.Add(selector);

            if (!Check(TokenType.Comma))
                break;

            selector =  Selector();
            if (selector is null)
                throw new Exception("Expected a selector");
        }

        Whitespace();
        Consume(TokenType.OpeningBrace, "Expected '{'");
        Whitespace();

        var declarations = new List<CssDeclaration>();
        var declaration = Declaration();
        while (true) {
            if (declaration is not null)
                declarations.Add(declaration);

            Whitespace();
            if (!Check(TokenType.Semicolon))
                break;

            Whitespace();
            declaration = Declaration();
        }

        if (!Check(TokenType.ClosingBrace))
            throw new Exception("Expected '}'");

        Whitespace();
        return new CssRuleset(selectors, declarations);
    }

    #region Selectors

    private CssSelector? Selector() {
        // TODO: Combinators
        return SimpleSelector();
    }

    private CssSelector? SimpleSelector() {
        var selectors = new Stack<CssSelector>();
        var elementName = ElementName();
        if (elementName is not null)
            selectors.Push(new CssNameSelector(elementName.Value));

        while (true) {
            if (_current.Type is TokenType.Hash) {
                selectors.Push(new CssIdSelector(_current.Text));
                Next();
            } else if (Class() is { } classSelector) {
                selectors.Push(classSelector);
            } else if (Attribute() is { } attributeSelector) {
                selectors.Push(attributeSelector);
            } else if (Pseudo() is { } pseudoSelector) {
                selectors.Push(pseudoSelector);
            } else {
                break;
            }
        }

        if (selectors.Count == 0)
            return null;

        var selector = selectors.Pop();
        while (selectors.TryPop(out var parentSelector)) {
            parentSelector.SubSelector = selector;
            selector = parentSelector;
        }

        return selector;
    }

    private ReadOnlyMemory<char>? ElementName() {
        if (_current.Type is TokenType.Identifier or TokenType.Asterisk) {
            var name = _current.Text;

            Next();
            return name;
        }

        return null;
    }

    // .class
    private CssClassSelector? Class() {
        if (!Check(TokenType.Period))
            return null;
        if (_current.Type is not TokenType.Identifier)
            throw new Exception("Expected a class name");

        var @class = _current.Text;

        Next();
        return new CssClassSelector(@class);
    }

    // identifier = value
    // identifier includes "string"
    // identifier |= value
    private CssSelector? Attribute() {
        // TODO
        return null;
    }

    // :identifier
    // :function()
    private CssSelector? Pseudo() {
        // TODO
        return null;
    }

    #endregion

    // property: expression
    // property: expression !important
    private CssDeclaration? Declaration() {
        if (Property() is not { } property)
            return null;

        Whitespace();
        Consume(TokenType.Colon, "Expected ':'");
        Whitespace();

        var expr = Expression();
        if (expr is null)
            throw new Exception("Expected an expression");

        // TODO: !important

        return new CssDeclaration(property, expr);
    }

    private ReadOnlyMemory<char>? Property() {
        if (_current.Type is TokenType.Identifier) {
            var property = _current.Text;

            Next();
            Whitespace();
            return property;
        }

        return null;
    }

    private ICssExpression? Expression() {
        if (ExpressionTerm() is not { } firstTerm)
            return null;

        // TODO: Operators

        if (ExpressionTerm() is not { } secondaryTerm)
            return firstTerm;

        var terms = new List<ICssExpression> { firstTerm };

        while (secondaryTerm is not null) {
            terms.Add(secondaryTerm);
            secondaryTerm = ExpressionTerm();
        }

        return new CssExpressionGroup(terms);
    }

    private ICssExpression? ExpressionTerm() {
        // TODO: Unary operator

        ICssExpression? term = _current.Type switch {
            TokenType.String => new CssString(_current.Text),
            TokenType.Identifier => new CssIdentifier(_current.Text),
            TokenType.Hash => new CssHexColor(_current.Text),
            TokenType.Number => new CssNumber(_current.Text),
            TokenType.Dimension => new CssDimension(_current.Text), // TODO: Split into Length, Angle, Time, Frequency
            // TODO: Percentage, em, ex, URI, Function
            _ => null
        };

        if (term is null)
            return null;

        Next();
        Whitespace();
        return term;
    }

    #region Helpers

    private void Next() {
        _current = lexer.GetNextToken();
    }

    private bool Check(TokenType type) {
        if (_current.Type != type)
            return false;

        Next();
        return true;
    }

    private void Consume(TokenType type, string errorMessage) {
        if (!Check(type))
            throw new Exception(errorMessage);
    }

    private void Whitespace() {
        while (Check(TokenType.Whitespace)) { }
    }

    #endregion
}
