using OpenDreamShared.Interface;
using System.Collections.Generic;

namespace OpenDreamShared.Compiler.DMF {
    public class DMFParser : Parser<char> {
        private static readonly List<TokenType> _validAttributeValueTypes = new() {
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
            TokenType.DMF_Distort
        };

        public DMFParser(DMFLexer lexer) : base(lexer) { }

        public InterfaceDescriptor Interface() {
            List<WindowDescriptor> windowDescriptors = new();

            bool parsing = true;
            while (parsing) {
                WindowDescriptor windowDescriptor = Window();
                if (windowDescriptor != null) {
                    Newline();
                    windowDescriptors.Add(windowDescriptor);
                }

                bool macro = Macro();
                if (macro) {
                    Newline();
                }

                bool menu = Menu();
                if (menu) {
                    Newline();
                }

                if (windowDescriptor == null && !macro && !menu) {
                    parsing = false;
                }
            }

            Consume(TokenType.EndOfFile, "Expected EOF");
            return new InterfaceDescriptor(windowDescriptors);
        }

        public WindowDescriptor Window() {
            if (Check(TokenType.DMF_Window)) {
                Token windowNameToken = Current();
                Consume(TokenType.DMF_String, "Expected a window name");
                string windowName = (string)windowNameToken.Value;
                Newline();

                List<ElementDescriptor> elementDescriptors = new();
                ElementDescriptor elementDescriptor;
                do {
                    elementDescriptor = Element();

                    if (elementDescriptor != null) elementDescriptors.Add(elementDescriptor);
                } while (elementDescriptor != null);

                return new WindowDescriptor(windowName, elementDescriptors);
            } else {
                return null;
            }
        }

        public ElementDescriptor Element() {
            if (Check(TokenType.DMF_Elem)) {
                Token elementNameToken = Current();
                Consume(TokenType.DMF_String, "Expected an element name");
                string elementName = (string)elementNameToken.Value;
                Newline();

                Dictionary<string, Token> attributes = new();
                Token attributeToken;

                while ((attributeToken = Current()).Type == TokenType.DMF_Attribute ||
                        attributeToken.Type == TokenType.DMF_Macro ||
                        attributeToken.Type == TokenType.DMF_Menu
                ) {
                    Advance();
                    Consume(TokenType.DMF_Equals, "Expected '='");

                    attributes.Add(attributeToken.Text, Current());
                    Advance();
                    Newline();
                }

                if (!attributes.TryGetValue("type", out Token elementType)) Error("Element '" + elementName + "' does not have a 'type' attribute");
                ElementDescriptor descriptor = elementType.Type switch {
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

        public bool Menu() {
            if (Check(TokenType.DMF_Menu)) {
                Consume(TokenType.DMF_String, "Expected a menu name");
                Newline();

                while (MenuElement()) {
                }

                return true;
            } else {
                return false;
            }
        }

        public bool MenuElement() {
            if (Check(TokenType.DMF_Elem)) {
                Check(TokenType.DMF_String);

                Newline();
                while (Check(TokenType.DMF_Attribute)) {
                    Consume(TokenType.DMF_Equals, "Expected '='");
                    Advance(); //Attribute value
                    Newline();
                }

                return true;
            } else {
                return false;
            }
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
                if (!_validAttributeValueTypes.Contains(attribute.Value.Type)) Error("Invalid attribute value (" + attribute.Value.Text + ")");

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
