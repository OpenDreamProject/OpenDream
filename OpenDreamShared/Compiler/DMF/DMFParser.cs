using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using OpenDreamShared.Interface;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;

namespace OpenDreamShared.Compiler.DMF {
    public sealed class DMFParser : Parser<char> {
        public static readonly TokenType[] ValidAttributeValueTypes = {
            TokenType.DMF_None,
            TokenType.DMF_String,
            TokenType.DMF_Integer,
            TokenType.DMF_Boolean,
            TokenType.DMF_Position,
            TokenType.DMF_Dimension,
            TokenType.DMF_Color,
            TokenType.DMF_Resource,
            TokenType.DMF_Sunken,
            TokenType.DMF_PushBox,
            TokenType.DMF_PushButton,
            TokenType.DMF_Distort,
            TokenType.DMF_Stretch,
            TokenType.DMF_Center,
            TokenType.DMF_Left,
            TokenType.DMF_Right,
            TokenType.DMF_Top,
            TokenType.DMF_TopLeft,
            TokenType.DMF_TopRight,
            TokenType.DMF_Bottom,
            TokenType.DMF_BottomLeft,
            TokenType.DMF_BottomRight,
            TokenType.DMF_Vertical,
            TokenType.DMF_Line,

            TokenType.DMF_Main,
            TokenType.DMF_Input,
            TokenType.DMF_Button,
            TokenType.DMF_Child,
            TokenType.DMF_Output,
            TokenType.DMF_Info,
            TokenType.DMF_Map,
            TokenType.DMF_Browser,
            TokenType.DMF_Label
        };

        public DMFParser(DMFLexer lexer) : base(lexer) { }

        public InterfaceDescriptor Interface() {
            List<WindowDescriptor> windowDescriptors = new();
            List<MacroSetDescriptor> macroSetDescriptors = new();
            Dictionary<string, MenuDescriptor> menuDescriptors = new();

            var serializationManager = IoCManager.Resolve<ISerializationManager>();
            bool parsing = true;
            while (parsing) {
                try {
                    WindowDescriptor windowDescriptor = Window(serializationManager);
                    if (windowDescriptor != null) {
                        windowDescriptors.Add(windowDescriptor);
                        Newline();
                    }

                    MacroSetDescriptor macroSet = MacroSet(serializationManager);
                    if (macroSet != null) {
                        macroSetDescriptors.Add(macroSet);
                        Newline();
                    }

                    MenuDescriptor menu = Menu(serializationManager);
                    if (menu != null) {
                        menuDescriptors.Add(menu.Name, menu);
                        Newline();
                    }

                    if (windowDescriptor == null && macroSet == null && menu == null) {
                        parsing = false;
                    }
                } catch (CompileErrorException) { //Error recovery
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

        public WindowDescriptor Window(ISerializationManager serializationManager) {
            if (Check(TokenType.DMF_Window)) {
                Token windowNameToken = Current();
                Consume(TokenType.DMF_String, "Expected a window name");
                string windowName = (string)windowNameToken.Value;
                Newline();

                List<ControlDescriptor> elementDescriptors = new();
                ControlDescriptor elementDescriptor;
                do {
                    elementDescriptor = Element(serializationManager);

                    if (elementDescriptor != null) elementDescriptors.Add(elementDescriptor);
                } while (elementDescriptor != null);

                return new WindowDescriptor(windowName, elementDescriptors);
            }

            return null;
        }

        public ControlDescriptor Element(ISerializationManager serializationManager) {
            if (Check(TokenType.DMF_Elem)) {
                Token elementNameToken = Current();
                Consume(TokenType.DMF_String, "Expected an element name");
                string elementName = (string)elementNameToken.Value;
                Newline();

                var attributes = Attributes();
                attributes.Add("name", elementName);

                if (!attributes.TryGet("type", out var elementType) || elementType is not ValueDataNode elementTypeValue)
                {
                    Error("Element '" + elementName + "' does not have a valid 'type' attribute");
                    return null;
                }

                Type descriptorType = elementTypeValue.Value switch {
                    "MAIN" => typeof(ControlDescriptorMain),
                    "MAP" => typeof(ControlDescriptorMap),
                    "CHILD" => typeof(ControlDescriptorChild),
                    "OUTPUT" => typeof(ControlDescriptorOutput),
                    "INFO" => typeof(ControlDescriptorInfo),
                    "INPUT" => typeof(ControlDescriptorInput),
                    "BUTTON" => typeof(ControlDescriptorButton),
                    "BROWSER" => typeof(ControlDescriptorBrowser),
                    "LABEL" => typeof(ControlDescriptorLabel),
                    _ => null
                };

                if (descriptorType == null)
                {
                    Error("Invalid descriptor type '" + elementTypeValue.Value + "'");
                    return null;
                }

                return (ControlDescriptor) serializationManager.ReadValue(descriptorType, attributes);
            }

            return null;
        }

        public MacroSetDescriptor MacroSet(ISerializationManager serializationManager) {
            if (Check(TokenType.DMF_Macro)) {
                Token macroSetNameToken = Current();
                Consume(TokenType.DMF_String, "Expected a macro set name");
                Newline();

                List<MacroDescriptor> macros = new();
                MacroDescriptor macro;
                while ((macro = Macro(serializationManager)) != null) {
                    macros.Add(macro);
                }

                return new MacroSetDescriptor((string)macroSetNameToken.Value, macros);
            } else {
                return null;
            }
        }

        public MacroDescriptor Macro(ISerializationManager serializationManager) {
            if (Check(TokenType.DMF_Elem)) {
                Token macroIdToken = Current();
                bool hasId = Check(TokenType.DMF_String);
                Newline();

                var attributes = Attributes();
                if (hasId) attributes.Add("id", macroIdToken.Text);

                return serializationManager.ReadValue<MacroDescriptor>(attributes);
            }

            return null;
        }

        public MenuDescriptor Menu(ISerializationManager serializationManager) {
            if (Check(TokenType.DMF_Menu)) {
                Token menuNameToken = Current();
                Consume(TokenType.DMF_String, "Expected a menu name");
                Newline();

                List<MenuElementDescriptor> menuElements = new();
                MenuElementDescriptor menuElement;
                while ((menuElement = MenuElement(serializationManager)) != null) {
                    menuElements.Add(menuElement);
                }

                return new MenuDescriptor((string)menuNameToken.Value, menuElements);
            }

            return null;
        }

        public MenuElementDescriptor MenuElement(ISerializationManager serializationManager) {
            if (Check(TokenType.DMF_Elem)) {
                Token elementNameToken = Current();
                bool hasId = Check(TokenType.DMF_String);
                Newline();

                var attributes = Attributes();
                //TODO: Name and Id are separate
                if (hasId && !attributes.Has("name")) attributes.Add("name", (string) elementNameToken.Value);

                return serializationManager.ReadValue<MenuElementDescriptor>(attributes);
            }

            return null;
        }

        public bool TryGetAttribute([NotNullWhen(true)] out string key, [NotNullWhen(true)] out string token)
        {
            key = null;
            token = null;
            Token attributeToken = Current();

            if (Check(new[] { TokenType.DMF_Attribute, TokenType.DMF_Macro, TokenType.DMF_Menu, TokenType.DMF_Stretch, TokenType.DMF_Left, TokenType.DMF_Right })) {
                if (!Check(TokenType.DMF_Equals)) {
                    ReuseToken(attributeToken);

                    return false;
                }

                Token attributeValue = Current();
                if (!Check(ValidAttributeValueTypes)) Error("Invalid attribute value (" + attributeValue.Text + ")");

                Newline();
                key = attributeToken.Text;
                token = attributeValue.Text;
                return true;
            }

            return false;
        }

        public MappingDataNode Attributes()
        {
            var node = new MappingDataNode();

            while (TryGetAttribute(out var key, out var value))
            {
                if (value == "none") continue;

                if (value[0] == '"')
                {
                    value = value.Substring(1, value.Length - 2);
                }
                node.Add(key, value);
            }

            return node;
        }

        public void Newline() {
            while (Check(TokenType.Newline)) {
            }
        }

        public string Resource() {
            Token resourceToken = Current();
            Consume(TokenType.DMF_Resource, "Expected a resource");

            return (string)resourceToken.Value;
        }
    }
}
