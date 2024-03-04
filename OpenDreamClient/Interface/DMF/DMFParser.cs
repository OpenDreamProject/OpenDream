using System.Diagnostics.CodeAnalysis;
using System.Linq;
using OpenDreamClient.Interface.Descriptors;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Token = OpenDreamClient.Interface.DMF.DMFLexer.Token;
using TokenType = OpenDreamClient.Interface.DMF.DMFLexer.TokenType;

namespace OpenDreamClient.Interface.DMF;

public sealed class DMFParser(DMFLexer lexer, ISerializationManager serializationManager) {
    public List<string> Errors = new();

    private readonly TokenType[] _attributeTokenTypes = {
        TokenType.Attribute,
        TokenType.Macro,
        TokenType.Menu
    };

    private Token _currentToken = lexer.NextToken();
    private bool _errorMode;
    private readonly Queue<Token> _tokenQueue = new();

    /// <summary>
    /// Parse the command used in a global winset()
    /// </summary>
    public List<DMFWinSet> GlobalWinSet() {
        List<DMFWinSet> winSets = new();

        while (TryGetAttribute(out var winset)) {
            winSets.Add(winset);
        }

        return winSets;
    }

    public InterfaceDescriptor Interface() {
        List<WindowDescriptor> windowDescriptors = new();
        List<MacroSetDescriptor> macroSetDescriptors = new();
        List<MenuDescriptor> menuDescriptors = new();

        bool parsing = true;
        while (parsing) {
            WindowDescriptor? windowDescriptor = Window();
            if (windowDescriptor != null) {
                windowDescriptors.Add(windowDescriptor);
                Newline();
            }

            MacroSetDescriptor? macroSet = MacroSet();
            if (macroSet != null) {
                macroSetDescriptors.Add(macroSet);
                Newline();
            }

            MenuDescriptor? menu = Menu();
            if (menu != null) {
                menuDescriptors.Add(menu);
                Newline();
            }

            if (windowDescriptor == null && macroSet == null && menu == null) {
                parsing = false;
            }

            if (_errorMode) {
                //Error recovery
                Token token = Current();
                while (token.Type is not (TokenType.Window or TokenType.Macro or TokenType.Menu)) {
                    token = Advance();

                    if (token.Type == TokenType.EndOfFile) {
                        parsing = false;
                        break;
                    }
                }
            }
        }

        Consume(TokenType.EndOfFile, "Expected EOF");
        return new InterfaceDescriptor(windowDescriptors, macroSetDescriptors, menuDescriptors);
    }

    private WindowDescriptor? Window() {
        if (Check(TokenType.Window)) {
            Token windowIdToken = Current();
            Consume(TokenType.Value, "Expected a window id");
            string windowId = windowIdToken.Text;
            Newline();

            WindowDescriptor window = new(windowId);
            while (Element(window)) {}

            return window;
        }

        return null;
    }

    private bool Element(WindowDescriptor window) {
        if (Check(TokenType.Elem)) {
            Token elementIdToken = Current();
            Consume(TokenType.Value, "Expected an element id");
            string elementId = elementIdToken.Text;
            Newline();

            var attributes = Attributes();
            attributes.Add("id", elementId);

            var control = window.CreateChildDescriptor(serializationManager, attributes);
            if (control == null) {
                Error($"Element '{elementId}' does not have a valid 'type' attribute");
                return false;
            }

            return true;
        }

        return false;
    }

    private MacroSetDescriptor? MacroSet() {
        if (Check(TokenType.Macro)) {
            Token macroSetIdToken = Current();
            Consume(TokenType.Value, "Expected a macro set id");
            Newline();

            MacroSetDescriptor macroSet = new(macroSetIdToken.Text);
            while (Macro(macroSet)) { }

            return macroSet;
        } else {
            return null;
        }
    }

    private bool Macro(MacroSetDescriptor macroSet) {
        if (Check(TokenType.Elem)) {
            Token macroIdToken = Current();
            bool hasId = Check(TokenType.Value);
            Newline();

            var attributes = Attributes();

            if (hasId) attributes.Add("id", macroIdToken.Text);
            else attributes.Add("id", attributes.Get("name"));

            macroSet.CreateChildDescriptor(serializationManager, attributes);
            return true;
        }

        return false;
    }

    private MenuDescriptor? Menu() {
        if (Check(TokenType.Menu)) {
            Token menuIdToken = Current();
            Consume(TokenType.Value, "Expected a menu id");
            Newline();

            var menu = new MenuDescriptor(menuIdToken.Text);
            while (MenuElement(menu)) { }

            return menu;
        }

        return null;
    }

    private bool MenuElement(MenuDescriptor menu) {
        if (Check(TokenType.Elem)) {
            Token elementIdToken = Current();
            bool hasId = Check(TokenType.Value);
            Newline();

            var attributes = Attributes();

            if (hasId) attributes.Add("id", elementIdToken.Text);
            else attributes.Add("id", attributes.Get("name"));

            menu.CreateChildDescriptor(serializationManager, attributes);
            return true;
        }

        return false;
    }

    private bool TryGetAttribute([NotNullWhen(true)] out DMFWinSet? winSet) {
        string? element = null;
        winSet = null;
        DMFWinSet? condition;
        DMFWinSet? elseValue;
        Token attributeToken = Current();

        if (Check(_attributeTokenTypes)) {
            while(Check(TokenType.Period)) { // element.attribute=value
                element ??= "";
                if(element.Length > 0) element += ".";
                element += attributeToken.Text;
                attributeToken = Current();

                if (!Check(_attributeTokenTypes)) {
                    Error("Expected attribute id");

                    return false;
                }
            }

            if (!Check(TokenType.Equals)) {
                // Ew
                _tokenQueue.Enqueue(_currentToken);
                _currentToken = attributeToken;

                return false;
            }

            Token attributeValue = Current();
            string valueText = attributeValue.Text;
            if (Check(TokenType.Period)) { // hidden verbs start with a period
                attributeValue = Current();
                valueText += attributeValue.Text;
                if (!Check(TokenType.Value) && !Check(TokenType.Attribute))
                    Error($"Invalid attribute value ({valueText})");
            } else if (!Check(TokenType.Value))
                if(Check(TokenType.Semicolon) || Check(TokenType.EndOfFile)) //thing.attribute=; means thing.attribute=empty string
                    valueText = "";
                else
                    Error($"Invalid attribute value ({valueText})");
            else if (Check(TokenType.Ternary)) {
                List<DMFWinSet> ifValues = new();
                List<DMFWinSet> elseValues = new();
                while(TryGetAttribute(out var ifValue)){
                    ifValues.Add(ifValue);
                }
                if(Check(TokenType.Colon)){ //not all ternarys have an else
                    while(TryGetAttribute(out elseValue)) {
                        elseValues.Add(elseValue);
                    }
                }
                winSet = new DMFWinSet(element, attributeToken.Text, valueText, ifValues, elseValues);
                return true;
            }

            Newline();
            winSet = new DMFWinSet(element, attributeToken.Text, valueText);
            return true;
        }

        return false;
    }

    public MappingDataNode Attributes() {
        var node = new MappingDataNode();

        while (TryGetAttribute(out var winset)) {
            if (winset.Element != null) {
                Error($"Element id \"{winset.Element}\" is not valid here");
                continue;
            }

            //TODO implement the conditional check
            if (winset.Value == "none")
                continue;

            node.Add(winset.Attribute, winset.Value);
        }

        return node;
    }

    private void Newline() {
        while (Check(TokenType.Newline) || Check(TokenType.Semicolon)) {
        }
    }

    private void Error(string errorMessage) {
        if (_errorMode)
            return;

        _errorMode = true;
        Errors.Add(errorMessage);
    }

    private Token Current() {
        return _currentToken;
    }

    private Token Advance() {
        _currentToken = (_tokenQueue.Count > 0) ? _tokenQueue.Dequeue() : lexer.NextToken();
        while (_currentToken.Type is TokenType.Error) {
            Error(_currentToken.Text);
            _currentToken = (_tokenQueue.Count > 0) ? _tokenQueue.Dequeue() : lexer.NextToken();
        }

        return Current();
    }

    private bool Check(TokenType type) {
        if (_currentToken.Type != type)
            return false;

        Advance();
        return true;
    }

    private bool Check(TokenType[] types) {
        if (!types.Contains(_currentToken.Type))
            return false;

        Advance();
        return true;
    }

    private void Consume(TokenType type, string errorMessage) {
        if (Check(type))
            return;

        Error(errorMessage);
    }
}
