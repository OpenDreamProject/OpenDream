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
                WindowDescriptor windowDescriptor = Window();
                if (windowDescriptor != null) {
                    windowDescriptors.Add(windowDescriptor);
                    Newline();
                }

                MacroSetDescriptor macroSet = MacroSet();
                if (macroSet != null) {
                    macroSetDescriptors.Add(macroSet);
                    Newline();
                }

                MenuDescriptor menu = Menu();
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

    public WindowDescriptor Window() {
        if (Check(TokenType.DMF_Window)) {
            Token windowNameToken = Current();
            Consume(TokenType.DMF_Value, "Expected a window name");
            string windowName = windowNameToken.Text;
            Newline();

            WindowDescriptor window = new(windowName);
            while (Element(window)) {}

            return window;
        }

        return null;
    }

    public bool Element(WindowDescriptor window) {
        if (Check(TokenType.DMF_Elem)) {
            Token elementNameToken = Current();
            Consume(TokenType.DMF_Value, "Expected an element name");
            string elementName = elementNameToken.Text;
            Newline();

            var attributes = Attributes();
            attributes.Add("name", elementName);

            var control = window.CreateChildDescriptor(_serializationManager, attributes);
            if (control == null)
                Error($"Element '{elementName}' does not have a valid 'type' attribute");

            return true;
        }

        return false;
    }

    public MacroSetDescriptor MacroSet() {
        if (Check(TokenType.DMF_Macro)) {
            Token macroSetNameToken = Current();
            Consume(TokenType.DMF_Value, "Expected a macro set name");
            Newline();

            MacroSetDescriptor macroSet = new(macroSetNameToken.Text);
            while (Macro(macroSet)) { }

            return macroSet;
        } else {
            return null;
        }
    }

    public bool Macro(MacroSetDescriptor macroSet) {
        if (Check(TokenType.DMF_Elem)) {
            Token macroIdToken = Current();
            bool hasId = Check(TokenType.DMF_Value);
            Newline();

            var attributes = Attributes();
            if (hasId) attributes.Add("id", macroIdToken.Text);

            macroSet.CreateChildDescriptor(_serializationManager, attributes);
            return true;
        }

        return false;
    }

    public MenuDescriptor Menu() {
        if (Check(TokenType.DMF_Menu)) {
            Token menuNameToken = Current();
            Consume(TokenType.DMF_Value, "Expected a menu name");
            Newline();

            var menu = new MenuDescriptor(menuNameToken.Text);
            while (MenuElement(menu)) { }

            return menu;
        }

        return null;
    }

    public bool MenuElement(MenuDescriptor menu) {
        if (Check(TokenType.DMF_Elem)) {
            Token elementNameToken = Current();
            bool hasId = Check(TokenType.DMF_Value);
            Newline();

            var attributes = Attributes();
            //TODO: Name and Id are separate
            if (hasId && !attributes.Has("name")) attributes.Add("name", elementNameToken.Text);

            menu.CreateChildDescriptor(_serializationManager, attributes);
            return true;
        }

        return false;
    }

    public bool TryGetAttribute(out string element, [NotNullWhen(true)] out string key, [NotNullWhen(true)] out string token) {
        element = null;
        key = null;
        token = null;
        Token attributeToken = Current();

        if (Check(_attributeTokenTypes)) {
            if (Check(TokenType.DMF_Period)) { // element.attribute=value
                element = attributeToken.Text;
                attributeToken = Current();

                if (!Check(_attributeTokenTypes)) {
                    Error("Expected attribute name");

                    return false;
                }
            }

            if (!Check(TokenType.DMF_Equals)) {
                ReuseToken(attributeToken);

                return false;
            }

            Token attributeValue = Current();
            if (!Check(TokenType.DMF_Value)) {
                if (Check(TokenType.DMF_Semicolon)) {
                    token = "none";
                } else {
                    Error($"Invalid attribute value ({attributeValue.Text})");
                }
            } else {
                token = attributeValue.Text;
            }

            Newline();
            key = attributeToken.Text;

            return true;
        }

        return false;
    }

    public MappingDataNode Attributes() {
        var node = new MappingDataNode();

        while (TryGetAttribute(out var element, out var key, out var value)) {
            if (element != null) {
                Error($"Element name \"{element}\" is not valid here");
                continue;
            }

            if (value == "none")
                continue;

            node.Add(key, value);
        }

        return node;
    }

    public void Newline() {
        while (Check(TokenType.Newline) || Check(TokenType.DMF_Semicolon)) {
        }
    }
}
