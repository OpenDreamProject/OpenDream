using System.Diagnostics.CodeAnalysis;
using OpenDreamClient.Interface.Descriptors;
using OpenDreamShared.Compiler;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace OpenDreamClient.Interface.DMF;

public sealed class DMFParser : Parser<char> {
    private readonly ISerializationManager _serializationManager;

    private readonly TokenType[] _attributeTokenTypes = {
        TokenType.DMF_Attribute,
        TokenType.DMF_Macro,
        TokenType.DMF_Menu
    };

    public DMFParser(DMFLexer lexer, ISerializationManager serializationManager) : base(lexer) {
        _serializationManager = serializationManager;
    }

    /// <summary>
    /// Parse the command used in a global winset()
    /// </summary>
    public List<DMFWinSet> GlobalWinSet() {
        List<DMFWinSet> winSets = new();

        while (TryGetAttribute(out var element, out var key, out var attribute)) {
            winSets.Add(new(element, key, attribute));
        }

        return winSets;
    }

    public InterfaceDescriptor Interface() {
        List<WindowDescriptor> windowDescriptors = new();
        List<MacroSetDescriptor> macroSetDescriptors = new();
        List<MenuDescriptor> menuDescriptors = new();

        bool parsing = true;
        while (parsing) {
            try {
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
            } catch (CompileErrorException) {
                //Error recovery
                Token token = Current();
                while (token.Type is not (TokenType.DMF_Window or TokenType.DMF_Macro or TokenType.DMF_Menu)) {
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
        if (Check(TokenType.DMF_Window)) {
            Token windowIdToken = Current();
            Consume(TokenType.DMF_Value, "Expected a window id");
            string windowId = windowIdToken.Text;
            Newline();

            WindowDescriptor window = new(windowId);
            while (Element(window)) {}

            return window;
        }

        return null;
    }

    private bool Element(WindowDescriptor window) {
        if (Check(TokenType.DMF_Elem)) {
            Token elementIdToken = Current();
            Consume(TokenType.DMF_Value, "Expected an element id");
            string elementId = elementIdToken.Text;
            Newline();

            var attributes = Attributes();
            attributes.Add("id", elementId);

            var control = window.CreateChildDescriptor(_serializationManager, attributes);
            if (control == null)
                Error($"Element '{elementId}' does not have a valid 'type' attribute");

            return true;
        }

        return false;
    }

    private MacroSetDescriptor? MacroSet() {
        if (Check(TokenType.DMF_Macro)) {
            Token macroSetIdToken = Current();
            Consume(TokenType.DMF_Value, "Expected a macro set id");
            Newline();

            MacroSetDescriptor macroSet = new(macroSetIdToken.Text);
            while (Macro(macroSet)) { }

            return macroSet;
        } else {
            return null;
        }
    }

    private bool Macro(MacroSetDescriptor macroSet) {
        if (Check(TokenType.DMF_Elem)) {
            Token macroIdToken = Current();
            bool hasId = Check(TokenType.DMF_Value);
            Newline();

            var attributes = Attributes();

            if (hasId) attributes.Add("id", macroIdToken.Text);
            else attributes.Add("id", attributes.Get("name"));

            macroSet.CreateChildDescriptor(_serializationManager, attributes);
            return true;
        }

        return false;
    }

    private MenuDescriptor? Menu() {
        if (Check(TokenType.DMF_Menu)) {
            Token menuIdToken = Current();
            Consume(TokenType.DMF_Value, "Expected a menu id");
            Newline();

            var menu = new MenuDescriptor(menuIdToken.Text);
            while (MenuElement(menu)) { }

            return menu;
        }

        return null;
    }

    private bool MenuElement(MenuDescriptor menu) {
        if (Check(TokenType.DMF_Elem)) {
            Token elementIdToken = Current();
            bool hasId = Check(TokenType.DMF_Value);
            Newline();

            var attributes = Attributes();

            if (hasId) attributes.Add("id", elementIdToken.Text);
            else attributes.Add("id", attributes.Get("name"));

            menu.CreateChildDescriptor(_serializationManager, attributes);
            return true;
        }

        return false;
    }

    private bool TryGetAttribute(out string? element, [NotNullWhen(true)] out string? key, [NotNullWhen(true)] out string? token) {
        element = null;
        key = null;
        token = null;
        Token attributeToken = Current();

        if (Check(_attributeTokenTypes)) {
            while(Check(TokenType.DMF_Period)) { // element.attribute=value
                element ??= "";
                if(element.Length > 0) element += ".";
                element += attributeToken.Text;
                attributeToken = Current();

                if (!Check(_attributeTokenTypes)) {
                    Error("Expected attribute id");

                    return false;
                }
            }

            if (!Check(TokenType.DMF_Equals)) {
                ReuseToken(attributeToken);

                return false;
            }

            Token attributeValue = Current();
            string valueText = attributeValue.Text;
            if (Check(TokenType.DMF_Period)) { // hidden verbs start with a period
                attributeValue = Current();
                valueText += attributeValue.Text;
                if (!Check(TokenType.DMF_Value) && !Check(TokenType.DMF_Attribute))
                    Error($"Invalid attribute value ({valueText})");
            } else if (!Check(TokenType.DMF_Value))
                Error($"Invalid attribute value ({valueText})");

            Newline();
            key = attributeToken.Text;
            token = valueText;
            return true;
        }

        return false;
    }

    public MappingDataNode Attributes() {
        var node = new MappingDataNode();

        while (TryGetAttribute(out var element, out var key, out var value)) {
            if (element != null) {
                Error($"Element id \"{element}\" is not valid here");
                continue;
            }

            if (value == "none")
                continue;

            node.Add(key, value);
        }

        return node;
    }

    private void Newline() {
        while (Check(TokenType.Newline) || Check(TokenType.DMF_Semicolon)) {
        }
    }
}
