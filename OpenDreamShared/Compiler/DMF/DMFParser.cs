using OpenDreamShared.Interface;
using System.Collections.Generic;

namespace OpenDreamShared.Compiler.DMF {
    class DMFParser : Parser<char> {
        private TokenType[] _sharedElementAttributeTypes = new TokenType[] {
            TokenType.DMF_Pos,
            TokenType.DMF_Size,
            TokenType.DMF_Anchor1,
            TokenType.DMF_Anchor2,
            TokenType.DMF_IsDefault,
            TokenType.DMF_IsVisible,
            TokenType.DMF_SavedParams,
            TokenType.DMF_Icon,
            TokenType.DMF_BackgroundColor,
            TokenType.DMF_Border,
            TokenType.DMF_FontFamily,
            TokenType.DMF_FontSize,
            TokenType.DMF_TextColor,
            TokenType.DMF_IsDisabled,
            TokenType.DMF_RightClick
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
                string windowName = String();
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
                string elementName = String();
                Newline();
                Consume(TokenType.DMF_Type, "Expected 'type'");
                Consume(TokenType.DMF_Equals, "Expected '='");
                Token elementType = Current();

                Advance();
                Newline();
                switch (elementType.Type) {
                    case TokenType.DMF_Main: return ElementMain(elementName);
                    case TokenType.DMF_Map: return ElementMap(elementName);
                    case TokenType.DMF_Child: return ElementChild(elementName);
                    case TokenType.DMF_Output: return ElementOutput(elementName);
                    case TokenType.DMF_Info: return ElementInfo(elementName);
                    case TokenType.DMF_Input: return ElementInput(elementName);
                    case TokenType.DMF_Button: return ElementButton(elementName);
                    case TokenType.DMF_Browser: return ElementBrowser(elementName);
                    default: Error("Invalid element type '" + elementType.Text + "'"); break;
                }
            }

            return null;
        }

        public ElementDescriptorMain ElementMain(string elementName) {
            ElementDescriptorMain element = new ElementDescriptorMain(elementName);
            TokenType[] attributeTypes = new TokenType[] {
                TokenType.DMF_IsPane,
                TokenType.DMF_Macro,
                TokenType.DMF_Menu,
                TokenType.DMF_StatusBar
            };

            Token attributeToken = Current();
            while (Check(_sharedElementAttributeTypes) || Check(attributeTypes)) {
                Consume(TokenType.DMF_Equals, "Expected '='");

                switch (attributeToken.Type) {
                    case TokenType.DMF_IsPane: element.IsPane = Boolean(); break;
                    case TokenType.DMF_Macro: String(); break;
                    case TokenType.DMF_Menu: String(); break;
                    case TokenType.DMF_StatusBar: Boolean(); break;
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
                TokenType.DMF_ZoomMode
            };

            Token attributeToken = Current();
            while (Check(_sharedElementAttributeTypes) || Check(attributeTypes)) {
                Consume(TokenType.DMF_Equals, "Expected '='");

                switch (attributeToken.Type) {
                    case TokenType.DMF_ZoomMode: {
                        Check(TokenType.DMF_Distort);

                        break;
                    }
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
            while (Check(_sharedElementAttributeTypes) || Check(attributeTypes)) {
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
            while (Check(_sharedElementAttributeTypes) || Check(attributeTypes)) {
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
            while (Check(_sharedElementAttributeTypes) || Check(attributeTypes)) {
                Consume(TokenType.DMF_Equals, "Expected '='");

                switch (attributeToken.Type) {
                    default: SetSharedAttribute(element, attributeToken); break;
                }

                Newline();
                attributeToken = Current();
            }

            return element;
        }

        public ElementDescriptorInput ElementInput(string elementName) {
            ElementDescriptorInput element = new ElementDescriptorInput(elementName);
            TokenType[] attributeTypes = new TokenType[] {

            };

            Token attributeToken = Current();
            while (Check(_sharedElementAttributeTypes) || Check(attributeTypes)) {
                Consume(TokenType.DMF_Equals, "Expected '='");

                switch (attributeToken.Type) {
                    default: SetSharedAttribute(element, attributeToken); break;
                }

                Newline();
                attributeToken = Current();
            }

            return element;
        }

        public ElementDescriptorButton ElementButton(string elementName) {
            ElementDescriptorButton element = new ElementDescriptorButton(elementName);
            TokenType[] attributeTypes = new TokenType[] {
                TokenType.DMF_ButtonType,
                TokenType.DMF_Text,
                TokenType.DMF_Command
            };

            Token attributeToken = Current();
            while (Check(_sharedElementAttributeTypes) || Check(attributeTypes)) {
                Consume(TokenType.DMF_Equals, "Expected '='");

                switch (attributeToken.Type) {
                    case TokenType.DMF_ButtonType:{
                        Check(TokenType.DMF_PushBox);

                        break;
                    }
                    case TokenType.DMF_Text: element.Text = String(); break;
                    case TokenType.DMF_Command: String(); break;
                    default: SetSharedAttribute(element, attributeToken); break;
                }

                Newline();
                attributeToken = Current();
            }

            return element;
        }

        public ElementDescriptorBrowser ElementBrowser(string elementName) {
            ElementDescriptorBrowser element = new ElementDescriptorBrowser(elementName);
            TokenType[] attributeTypes = new TokenType[] {
                TokenType.DMF_AutoFormat
            };

            Token attributeToken = Current();
            while (Check(_sharedElementAttributeTypes) || Check(attributeTypes)) {
                Consume(TokenType.DMF_Equals, "Expected '='");

                switch (attributeToken.Type) {
                    case TokenType.DMF_AutoFormat: Boolean(); break;
                    default: SetSharedAttribute(element, attributeToken); break;
                }

                Newline();
                attributeToken = Current();
            }

            return element;
        }

        public bool Macro() {
            if (Check(TokenType.DMF_Macro)) {
                string macroName = String();

                return true;
            } else {
                return false;
            }
        }

        public bool Menu() {
            if (Check(TokenType.DMF_Menu)) {
                string menuName = String();
                Newline();

                while (MenuElement()) ;

                return true;
            } else {
                return false;
            }
        }

        public bool MenuElement() {
            if (Check(TokenType.DMF_Elem)) {
                TokenType[] attributeTypes = new TokenType[] {
                    TokenType.DMF_Name,
                    TokenType.DMF_Command,
                    TokenType.DMF_Category,
                    TokenType.DMF_SavedParams
                };

                Check(TokenType.DMF_String);
                Newline();
                while (Check(attributeTypes)) {
                    Consume(TokenType.DMF_Equals, "Expected '='");
                    Token attributeToken = Current();
                    Advance();
                    Newline();

                    switch (attributeToken.Type) {
                        case TokenType.DMF_Name: String(); break;
                        case TokenType.DMF_Command: String(); break;
                        case TokenType.DMF_Category: String(); break;
                        case TokenType.DMF_SavedParams: String(); break;
                    }
                }

                return true;
            } else {
                return false;
            }
        }

        public void Newline() {
            while (Check(TokenType.Newline)) ;
        }

        public (int X, int Y)? Coordinate() {
            if (Check(TokenType.DMF_None)) {
                return null;
            } else {
                Token xToken = Current();
                Consume(TokenType.DMF_Integer, "Expected a coordinate");
                Consume(TokenType.DMF_Comma, "Expected ','");
                Token yToken = Current();
                Consume(TokenType.DMF_Integer, "Expected a number");

                return ((int)xToken.Value, (int)yToken.Value);
            }
        }

        public (int W, int H) Size() {
            Token wToken = Current();
            Consume(TokenType.DMF_Integer, "Expected a size");
            Consume(TokenType.DMF_X, "Expected 'x'");
            Token hToken = Current();
            Consume(TokenType.DMF_Integer, "Expected a number");

            return ((int)wToken.Value, (int)hToken.Value);
        }

        public int Integer() {
            Token integerToken = Current();
            Consume(TokenType.DMF_Integer, "Expected an integer");

            return (int)integerToken.Value;
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

        public string Resource() {
            Token resourceToken = Current();
            Consume(TokenType.DMF_Resource, "Expected a resource");

            return (string)resourceToken.Value;
        }

        public int? Color() {
            if (Check(TokenType.DMF_None)) {
                return null;
            } else {
                Token colorToken = Current();
                Consume(TokenType.DMF_Color, "Expected a color");

                return (int)colorToken.Value;
            }
        }

        private void SetSharedAttribute(ElementDescriptor element, Token attributeToken) {
            switch (attributeToken.Type) {
                case TokenType.DMF_Pos: {
                    (int X, int Y)? coordinate = Coordinate();

                    if (coordinate.HasValue) element.Pos = new System.Drawing.Point(coordinate.Value.X, coordinate.Value.Y);
                    break;
                }
                case TokenType.DMF_Size: {
                    (int W, int H) size = Size();

                    element.Size = new System.Drawing.Size(size.W, size.H);
                    break;
                }
                case TokenType.DMF_Anchor1: {
                    (int X, int Y)? anchor1 = Coordinate();

                    if (anchor1.HasValue) element.Anchor1 = new System.Drawing.Point(anchor1.Value.X, anchor1.Value.Y);
                    break;
                }
                case TokenType.DMF_Anchor2: {
                    (int X, int Y)? anchor2 = Coordinate();

                    if (anchor2.HasValue) element.Anchor2 = new System.Drawing.Point(anchor2.Value.X, anchor2.Value.Y);
                    break;
                }
                case TokenType.DMF_Border: {
                    Check(TokenType.DMF_Sunken);

                    break;
                }
                case TokenType.DMF_BackgroundColor: {
                    int? color = Color();

                    if (color.HasValue) element.BackgroundColor = System.Drawing.Color.FromArgb(color.Value);
                    break;
                }
                case TokenType.DMF_IsDefault: element.IsDefault = Boolean(); break;
                case TokenType.DMF_SavedParams: String(); break;
                case TokenType.DMF_Icon: Resource(); break;
                case TokenType.DMF_IsVisible: element.IsVisible = Boolean(); break;
                case TokenType.DMF_FontFamily: String(); break;
                case TokenType.DMF_FontSize: Integer(); break;
                case TokenType.DMF_TextColor: Color(); break;
                case TokenType.DMF_IsDisabled: Boolean(); break;
                case TokenType.DMF_RightClick: Boolean(); break;
                default: Error("Invalid attribute '" + attributeToken.Text + "'"); break;
            }
        }
    }
}
