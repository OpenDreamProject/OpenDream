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
            TokenType.DMF_Distort,

            TokenType.DMF_Main,
            TokenType.DMF_Input,
            TokenType.DMF_Button,
            TokenType.DMF_Child,
            TokenType.DMF_Output,
            TokenType.DMF_Info,
            TokenType.DMF_Map,
            TokenType.DMF_Browser
        };

        public DMFParser(DMFLexer lexer) : base(lexer) { }

        public InterfaceDescriptor Interface() {
            List<WindowDescriptor> windowDescriptors = new();
            Dictionary<string, MenuDescriptor> menuDescriptors = new();

            bool parsing = true;
            while (parsing) {
                WindowDescriptor windowDescriptor = Window();
                if (windowDescriptor != null) {
                    windowDescriptors.Add(windowDescriptor);
                    Newline();
                }

                bool macro = Macro();
                if (macro) {
                    Newline();
                }

                MenuDescriptor menu = Menu();
                if (menu != null) {
                    menuDescriptors.Add(menu.Name, menu);
                    Newline();
                }

                if (windowDescriptor == null && !macro && menu == null) {
                    parsing = false;
                }
            }

            Consume(TokenType.EndOfFile, "Expected EOF");
            return new InterfaceDescriptor(windowDescriptors, menuDescriptors);
        }

        public WindowDescriptor Window() {
            if (Check(TokenType.DMF_Window)) {
                Token windowNameToken = Current();
                Consume(TokenType.DMF_String, "Expected a window name");
                string windowName = (string)windowNameToken.Value;
                Newline();

                List<WindowElementDescriptor> elementDescriptors = new();
                WindowElementDescriptor elementDescriptor;
                do {
                    elementDescriptor = Element();

                    if (elementDescriptor != null) elementDescriptors.Add(elementDescriptor);
                } while (elementDescriptor != null);

                return new WindowDescriptor(windowName, elementDescriptors);
            }

            return null;
        }

        public WindowElementDescriptor Element() {
            if (Check(TokenType.DMF_Elem)) {
                Token elementNameToken = Current();
                Consume(TokenType.DMF_String, "Expected an element name");
                string elementName = (string)elementNameToken.Value;
                Newline();

                Dictionary<string, Token> attributes = Attributes();

                if (!attributes.TryGetValue("type", out Token elementType)) Error("Element '" + elementName + "' does not have a 'type' attribute");
                WindowElementDescriptor descriptor = elementType.Type switch {
                    TokenType.DMF_Main => new ElementDescriptorMain(elementName),
                    TokenType.DMF_Map => new ElementDescriptorMap(elementName),
                    TokenType.DMF_Child => new ElementDescriptorChild(elementName),
                    TokenType.DMF_Output => new ElementDescriptorOutput(elementName),
                    TokenType.DMF_Info => new ElementDescriptorInfo(elementName),
                    TokenType.DMF_Input => new ElementDescriptorInput(elementName),
                    TokenType.DMF_Button => new ElementDescriptorButton(elementName),
                    TokenType.DMF_Browser => new ElementDescriptorBrowser(elementName),
                    _ => null
                };

                if (descriptor == null) Error("Invalid descriptor type '" + elementType.Text + "'");
                else SetAttributes(descriptor, attributes);

                return descriptor;
            }

            return null;
        }

        public bool Macro() {
            if (Check(TokenType.DMF_Macro)) {
                Consume(TokenType.DMF_String, "Expected a macro name");

                return true;
            } else {
                return false;
            }
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

            if (Check(TokenType.DMF_Attribute) || Check(TokenType.DMF_Macro) || Check(TokenType.DMF_Menu)) {
                Consume(TokenType.DMF_Equals, "Expected '='");

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
