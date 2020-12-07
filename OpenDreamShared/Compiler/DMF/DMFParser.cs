using OpenDreamShared.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDreamShared.Compiler.DMF {
    class DMFParser : Parser {
        private TokenType[] _sharedAttributeTypes = new TokenType[] {
            TokenType.DMF_Pos,
            TokenType.DMF_Size,
            TokenType.DMF_Anchor1,
            TokenType.DMF_Anchor2,
            TokenType.DMF_IsDefault
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
                } else {
                    parsing = false;
                }
            }

            Consume(TokenType.EndOfFile, "Expected EOF");
            return new InterfaceDescriptor(windowDescriptors);
        }

        public WindowDescriptor Window() {
            if (Check(TokenType.DMF_Window)) {
                Token windowName = Current();
                Consume(TokenType.DMF_String, "Expected a window name");
                Newline();

                List<ElementDescriptor> elementDescriptors = new();
                ElementDescriptor elementDescriptor;
                do {
                    elementDescriptor = Element();

                    if (elementDescriptor != null) elementDescriptors.Add(elementDescriptor);
                } while (elementDescriptor != null);

                return new WindowDescriptor((string)windowName.Value, elementDescriptors);
            } else {
                return null;
            }
        }

        public ElementDescriptor Element() {
            if (Check(TokenType.DMF_Elem)) {
                Token elementNameToken = Current();
                Consume(TokenType.DMF_String, "Expected an element name");
                Newline();
                Consume(TokenType.DMF_Type, "Expected 'type'");
                Consume(TokenType.DMF_Equals, "Expected '='");
                Token elementType = Current();
                string elementName = (string)elementNameToken.Value;

                Advance();
                Newline();
                switch (elementType.Type) {
                    case TokenType.DMF_Main: return ElementMain(elementName);
                    case TokenType.DMF_Map: return ElementMap(elementName);
                    case TokenType.DMF_Child: return ElementChild(elementName);
                    case TokenType.DMF_Output: return ElementOutput(elementName);
                    case TokenType.DMF_Info: return ElementInfo(elementName);
                    default: throw new Exception("Invalid element type '" + elementType.Text + "'");
                }
            } else {
                return null;
            }
        }

        public ElementDescriptorMain ElementMain(string elementName) {
            ElementDescriptorMain element = new ElementDescriptorMain(elementName);
            TokenType[] attributeTypes = new TokenType[] {
                TokenType.DMF_IsPane
            };

            Token attributeToken = Current();
            while (Check(_sharedAttributeTypes) || Check(attributeTypes)) {
                Consume(TokenType.DMF_Equals, "Expected '='");

                switch (attributeToken.Type) {
                    case TokenType.DMF_IsPane: element.IsPane = Boolean(); break;
                    default: SetSharedAttribute(element, attributeToken); break;
                }

                Newline();
                attributeToken = Current();
            }

            return element;
        }

        public ElementDescriptorMap ElementMap(string elementName) {
            ElementDescriptorMap element = new ElementDescriptorMap(elementName);
            TokenType[] attributeTypes = new TokenType[] {
                
            };

            Token attributeToken = Current();
            while (Check(_sharedAttributeTypes) || Check(attributeTypes)) {
                Consume(TokenType.DMF_Equals, "Expected '='");

                switch (attributeToken.Type) {
                    default: SetSharedAttribute(element, attributeToken); break;
                }

                Newline();
                attributeToken = Current();
            }

            return element;
        }

        public ElementDescriptorChild ElementChild(string elementName) {
            ElementDescriptorChild element = new ElementDescriptorChild(elementName);
            TokenType[] attributeTypes = new TokenType[] {
                TokenType.DMF_Left,
                TokenType.DMF_Right,
                TokenType.DMF_IsVert
            };

            Token attributeToken = Current();
            while (Check(_sharedAttributeTypes) || Check(attributeTypes)) {
                Consume(TokenType.DMF_Equals, "Expected '='");

                switch (attributeToken.Type) {
                    case TokenType.DMF_Left: element.Left = String(); break;
                    case TokenType.DMF_Right: element.Right = String(); break;
                    case TokenType.DMF_IsVert: element.IsVert = Boolean(); break;
                    default: SetSharedAttribute(element, attributeToken); break;
                }

                Newline();
                attributeToken = Current();
            }

            return element;
        }

        public ElementDescriptorOutput ElementOutput(string elementName) {
            ElementDescriptorOutput element = new ElementDescriptorOutput(elementName);
            TokenType[] attributeTypes = new TokenType[] {
                
            };

            Token attributeToken = Current();
            while (Check(_sharedAttributeTypes) || Check(attributeTypes)) {
                Consume(TokenType.DMF_Equals, "Expected '='");

                switch (attributeToken.Type) {
                    default: SetSharedAttribute(element, attributeToken); break;
                }

                Newline();
                attributeToken = Current();
            }

            return element;
        }

        public ElementDescriptorInfo ElementInfo(string elementName) {
            ElementDescriptorInfo element = new ElementDescriptorInfo(elementName);
            TokenType[] attributeTypes = new TokenType[] {

            };

            Token attributeToken = Current();
            while (Check(_sharedAttributeTypes) || Check(attributeTypes)) {
                Consume(TokenType.DMF_Equals, "Expected '='");

                switch (attributeToken.Type) {
                    default: SetSharedAttribute(element, attributeToken); break;
                }

                Newline();
                attributeToken = Current();
            }

            return element;
        }

        public void Newline() {
            while (Check(TokenType.Newline)) ;
        }

        public (int X, int Y) Coordinate() {
            Token xToken = Current();
            Consume(TokenType.DMF_Integer, "Expected a coordinate");
            Consume(TokenType.DMF_Comma, "Expected ','");
            Token yToken = Current();
            Consume(TokenType.DMF_Integer, "Expected a number");

            return ((int)xToken.Value, (int)yToken.Value);
        }

        public (int W, int H) Size() {
            Token wToken = Current();
            Consume(TokenType.DMF_Integer, "Expected a size");
            Consume(TokenType.DMF_X, "Expected 'x'");
            Token hToken = Current();
            Consume(TokenType.DMF_Integer, "Expected a number");

            return ((int)wToken.Value, (int)hToken.Value);
        }

        public bool Boolean() {
            if (Check(TokenType.DMF_True)) {
                return true;
            } else {
                Consume(TokenType.DMF_False, "Expected a boolean");
                return false;
            }
        }

        public string String() {
            Token stringToken = Current();
            Consume(TokenType.DMF_String, "Expected a string");

            return (string)stringToken.Value;
        }

        private void SetSharedAttribute(ElementDescriptor element, Token attributeToken) {
            switch (attributeToken.Type) {
                case TokenType.DMF_Pos: {
                    (int X, int Y) coordinate = Coordinate();

                    element.Pos = new System.Drawing.Point(coordinate.X, coordinate.Y);
                    break;
                }
                case TokenType.DMF_Size: {
                    (int W, int H) size = Size();

                    element.Size = new System.Drawing.Size(size.W, size.H);
                    break;
                }
                case TokenType.DMF_Anchor1: {
                    (int X, int Y) anchor1 = Coordinate();

                    element.Anchor1 = new System.Drawing.Point(anchor1.X, anchor1.Y);
                    break;
                }
                case TokenType.DMF_Anchor2: {
                    (int X, int Y) anchor2 = Coordinate();

                    element.Anchor2 = new System.Drawing.Point(anchor2.X, anchor2.Y);
                    break;
                }
                case TokenType.DMF_IsDefault: element.IsDefault = Boolean();  break;
                default: throw new Exception("Invalid attribute '" + attributeToken.Text + "'");
            }
        }
    }
}
