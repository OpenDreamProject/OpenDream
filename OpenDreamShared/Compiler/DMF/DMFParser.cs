using OpenDreamShared.Interface;
using System.Collections.Generic;

namespace OpenDreamShared.Compiler.DMF {
    public class DMFParser : Parser<char> {
        public static readonly List<TokenType> ValidAttributeValueTypes = new() {
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

            bool parsing = true;
            while (parsing) {
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
                    menuDescriptors.Add(menu.Name, menu);
                    Newline();
                }

                if (windowDescriptor == null && macroSet == null && menu == null) {
                    parsing = false;
                }
            }

            Consume(TokenType.EndOfFile, "Expected EOF");
            return new InterfaceDescriptor(windowDescriptors, macroSetDescriptors, menuDescriptors);
        }

        public WindowDescriptor Window() {
            if (Check(TokenType.DMF_Window)) {
                Token windowNameToken = Current();
                Consume(TokenType.DMF_String, "Expected a window name");
                string windowName = (string)windowNameToken.Value;
                Newline();

                List<ControlDescriptor> elementDescriptors = new();
                ControlDescriptor elementDescriptor;
                do {
                    elementDescriptor = Element();

                    if (elementDescriptor != null) elementDescriptors.Add(elementDescriptor);
                } while (elementDescriptor != null);

                return new WindowDescriptor(windowName, elementDescriptors);
            }

            return null;
        }

        public ControlDescriptor Element() {
            if (Check(TokenType.DMF_Elem)) {
                Token elementNameToken = Current();
                Consume(TokenType.DMF_String, "Expected an element name");
                string elementName = (string)elementNameToken.Value;
                Newline();

                Dictionary<string, Token> attributes = Attributes();

                if (!attributes.TryGetValue("type", out Token elementType)) Error("Element '" + elementName + "' does not have a 'type' attribute");
                ControlDescriptor descriptor = elementType.Type switch {
                    TokenType.DMF_Main => new ControlDescriptorMain(elementName),
                    TokenType.DMF_Map => new ControlDescriptorMap(elementName),
                    TokenType.DMF_Child => new ControlDescriptorChild(elementName),
                    TokenType.DMF_Output => new ControlDescriptorOutput(elementName),
                    TokenType.DMF_Info => new ControlDescriptorInfo(elementName),
                    TokenType.DMF_Input => new ControlDescriptorInput(elementName),
                    TokenType.DMF_Button => new ControlDescriptorButton(elementName),
                    TokenType.DMF_Browser => new ControlDescriptorBrowser(elementName),
                    TokenType.DMF_Label => new ControlDescriptorLabel(elementName),
                    _ => null
                };

                if (descriptor == null) Error("Invalid descriptor type '" + elementType.Text + "'");
                else SetAttributes(descriptor, attributes);

                return descriptor;
            }

            return null;
        }

        public MacroSetDescriptor MacroSet() {
            if (Check(TokenType.DMF_Macro)) {
                Token macroSetNameToken = Current();
                Consume(TokenType.DMF_String, "Expected a macro set name");
                Newline();

                List<MacroDescriptor> macros = new();
                MacroDescriptor macro;
                while ((macro = Macro()) != null) {
                    macros.Add(macro);
                }

                return new MacroSetDescriptor((string)macroSetNameToken.Value, macros);
            } else {
                return null;
            }
        }

        public MacroDescriptor Macro() {
            if (Check(TokenType.DMF_Elem)) {
                Token macroIdToken = Current();
                bool hasId = Check(TokenType.DMF_String);
                Newline();

                MacroDescriptor descriptor = new MacroDescriptor(hasId ? macroIdToken.Text : null);
                Dictionary<string, Token> attributes = Attributes();

                SetAttributes(descriptor, attributes);

                return descriptor;
            }

            return null;
        }

        public MenuDescriptor Menu() {
            if (Check(TokenType.DMF_Menu)) {
                Token menuNameToken = Current();
                Consume(TokenType.DMF_String, "Expected a menu name");
                Newline();

                List<MenuElementDescriptor> menuElements = new();
                MenuElementDescriptor menuElement;
                while ((menuElement = MenuElement()) != null) {
                    menuElements.Add(menuElement);
                }

                return new MenuDescriptor((string)menuNameToken.Value, menuElements);
            }

            return null;
        }

        public MenuElementDescriptor MenuElement() {
            if (Check(TokenType.DMF_Elem)) {
                Token elementNameToken = Current();
                bool hasName = Check(TokenType.DMF_String);
                Newline();

                MenuElementDescriptor descriptor = new MenuElementDescriptor(hasName ? (string)elementNameToken.Value : null);
                Dictionary<string, Token> attributes = Attributes();

                SetAttributes(descriptor, attributes);

                return descriptor;
            }

            return null;
        }

        public (string, Token)? AttributeAssign() {
            Token attributeToken = Current();

            if (Check(new[] { TokenType.DMF_Attribute, TokenType.DMF_Macro, TokenType.DMF_Menu, TokenType.DMF_Stretch, TokenType.DMF_Left, TokenType.DMF_Right })) {
                if (!Check(TokenType.DMF_Equals)) {
                    ReuseToken(attributeToken);

                    return null;
                }

                Token attributeValue = Current();
                if (!Check(ValidAttributeValueTypes)) Error("Invalid attribute value (" + attributeValue.Text + ")");

                Newline();
                return (attributeToken.Text, attributeValue);
            }

            return null;
        }

        public Dictionary<string, Token> Attributes() {
            Dictionary<string, Token> attributes = new();

            while (true) {
                (string, Token)? attributeAssign = AttributeAssign();

                if (attributeAssign.HasValue) attributes.Add(attributeAssign.Value.Item1, attributeAssign.Value.Item2);
                else break;
            }

            return attributes;
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

        private void SetAttributes(ElementDescriptor elementDescriptor, Dictionary<string, Token> attributes) {
            foreach (KeyValuePair<string, Token> attribute in attributes) {
                if (attribute.Key == "type") continue;

                if (elementDescriptor.HasAttribute(attribute.Key)) {
                    if (attribute.Value.Type == TokenType.DMF_None) {
                        elementDescriptor.SetAttribute(attribute.Key, null);
                    } else {
                        elementDescriptor.SetAttribute(attribute.Key, attribute.Value.Value);
                    }
                } else {
                    Warning("Invalid attribute '" + attribute.Key + "' on element '" + elementDescriptor.Name + "'");
                }
            }
        }
    }
}
