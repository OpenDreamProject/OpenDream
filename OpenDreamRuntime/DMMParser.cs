using OpenDreamRuntime.Objects;
using OpenDreamShared.Compiler;
using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Dream;
using System.Collections.Generic;

namespace OpenDreamRuntime {
    class DMMParser : DMParser {
        public DreamRuntime Runtime { get; }

        public class Map {
            public int MaxX, MaxY, MaxZ;
            public Dictionary<string, CellDefinition> CellDefinitions = new();
            public List<MapBlock> Blocks = new();
        }

        public class CellDefinition {
            public string Name;
            public MapObject Turf = null;
            public MapObject Area = null;
            public List<MapObject> Objects = new();

            public CellDefinition(string name) {
                Name = name;
            }
        }

        public class MapObject {
            public DreamPath Type;
            public Dictionary<string, DreamValue> VarOverrides = new();

            public MapObject(DreamPath type) {
                Type = type;
            }
        }

        public class MapBlock {
            public int X, Y, Z;
            public int Width = 0, Height = 0;
            public Dictionary<(int X, int Y), string> Cells = new();

            public MapBlock(int x, int y, int z) {
                X = x;
                Y = y;
                Z = z;
            }
        }

        private int _cellNameLength = -1;

        public DMMParser(DreamRuntime runtime, DMLexer lexer) : base(lexer) {
            Runtime = runtime;
        }

        public Map ParseMap() {
            Map map = new Map();

            _cellNameLength = -1;

            bool parsing = true;
            while (parsing) {
                CellDefinition cellDefinition = ParseCellDefinition();
                if (cellDefinition != null) {
                    if (_cellNameLength == -1) _cellNameLength = cellDefinition.Name.Length;
                    else if (cellDefinition.Name.Length != _cellNameLength) Error("Invalid cell definition name");

                    map.CellDefinitions.Add(cellDefinition.Name, cellDefinition);
                }

                MapBlock mapBlock = ParseMapBlock();
                if (mapBlock != null) {
                    int maxX = mapBlock.X + mapBlock.Width;
                    int maxY = mapBlock.Y + mapBlock.Height;
                    if (map.MaxX < maxX) map.MaxX = maxX;
                    if (map.MaxY < maxY) map.MaxY = maxY;
                    if (map.MaxZ < mapBlock.Z) map.MaxZ = mapBlock.Z;

                    map.Blocks.Add(mapBlock);
                }

                if (cellDefinition == null && mapBlock == null) parsing = false;
            }

            Consume(TokenType.EndOfFile, "Expected EOF");
            return map;
        }

        public CellDefinition ParseCellDefinition() {
            Token currentToken = Current();

            if (Check(TokenType.DM_String)) {
                Consume(TokenType.DM_Equals, "Expected '='");
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");

                CellDefinition cellDefinition = new CellDefinition((string)currentToken.Value);
                DMASTPath objectType = Path();
                while (objectType != null) {
                    MapObject mapObject = new MapObject(objectType.Path);

                    if (Check(TokenType.DM_LeftCurlyBracket)) {
                        DMASTStatement statement = Statement();

                        while (statement != null) {
                            DMASTObjectVarOverride varOverride = statement as DMASTObjectVarOverride;
                            DreamValue varValue;

                            if (varOverride == null) Error("Expected a var override");
                            if (!varOverride.ObjectPath.Equals(DreamPath.Root)) Error("Invalid var name");
                            if (varOverride.Value is DMASTConstantString dmastString) {
                                varValue = new DreamValue(dmastString.Value);
                            } else if (varOverride.Value is DMASTConstantResource dmastResource) {
                                varValue = new DreamValue(Runtime.ResourceManager.LoadResource(dmastResource.Path));
                            } else if (varOverride.Value is DMASTConstantInteger dmastInteger) {
                                varValue = new DreamValue(dmastInteger.Value);
                            } else if (varOverride.Value is DMASTConstantFloat dmastFloat) {
                                varValue = new DreamValue(dmastFloat.Value);
                            } else if (varOverride.Value is DMASTConstantPath dmastPath) {
                                varValue = new DreamValue(dmastPath.Value.Path);
                            } else if (varOverride.Value is DMASTConstantNull) {
                                varValue = DreamValue.Null;
                            } else if (varOverride.Value is DMASTList dmastList) {
                                DreamList list = DreamList.Create(Runtime);

                                //TODO: Fill list

                                varValue = new DreamValue(list);
                            } else if (varOverride.Value is DMASTNewList dmastNewList) {
                                DreamList list = DreamList.Create(Runtime);

                                //TODO: Fill list

                                varValue = new DreamValue(list);
                            } else {
                                Error("Invalid var value (" + varOverride.Value + ")");

                                varValue = DreamValue.Null;
                            }

                            mapObject.VarOverrides.Add(varOverride.VarName, varValue);
                            if (Check(TokenType.DM_Semicolon)) {
                                statement = Statement();
                            } else {
                                statement = null;
                            }
                        }

                        Consume(TokenType.DM_RightCurlyBracket, "Expected '}'");
                    }

                    if (mapObject.Type.IsDescendantOf(DreamPath.Turf)) {
                        cellDefinition.Turf = mapObject;
                    } else if (mapObject.Type.IsDescendantOf(DreamPath.Area)) {
                        cellDefinition.Area = mapObject;
                    } else {
                        cellDefinition.Objects.Add(mapObject);
                    }

                    if (Check(TokenType.DM_Comma)) {
                        objectType = Path();
                    } else {
                        objectType = null;
                    }
                }

                Consume(TokenType.DM_RightParenthesis, "Expected ')'");
                return cellDefinition;
            } else {
                return null;
            }
        }

        public MapBlock ParseMapBlock() {
            (int X, int Y, int Z)? coordinates = Coordinates();

            if (coordinates.HasValue) {
                MapBlock mapBlock = new MapBlock(coordinates.Value.X, coordinates.Value.Y, coordinates.Value.Z);

                Consume(TokenType.DM_Equals, "Expected '='");
                Token blockStringToken = Current();
                Consume(TokenType.DM_String, "Expected a string");

                string blockString = (string)blockStringToken.Value;
                List<string> lines = new(blockString.Split("\n"));
                lines.RemoveAll(string.IsNullOrEmpty);

                mapBlock.Height = lines.Count;
                for (int y = 1; y <= mapBlock.Height; y++) {
                    string line = lines[y - 1];
                    int width = (line.Length / _cellNameLength);

                    if (mapBlock.Width < width) mapBlock.Width = width;
                    if ((line.Length % _cellNameLength) != 0) Error("Invalid map block row");

                    for (int x = 1; x <= width; x++) {
                        mapBlock.Cells.Add((x, mapBlock.Height - y + 1), line.Substring((x - 1) * _cellNameLength, _cellNameLength));
                    }
                }

                return mapBlock;
            } else {
                return null;
            }
        }

        private (int X, int Y, int Z)? Coordinates() {
            if (Check(TokenType.DM_LeftParenthesis)) {
                DMASTConstantInteger x = Constant() as DMASTConstantInteger;
                if (x == null) Error("Expected an integer");
                Consume(TokenType.DM_Comma, "Expected ','");
                DMASTConstantInteger y = Constant() as DMASTConstantInteger;
                if (y == null) Error("Expected an integer");
                Consume(TokenType.DM_Comma, "Expected ','");
                DMASTConstantInteger z = Constant() as DMASTConstantInteger;
                if (z == null) Error("Expected an integer");
                Consume(TokenType.DM_RightParenthesis, "Expected ')'");

                return (x.Value, y.Value, z.Value);
            } else {
                return null;
            }
        }

        protected override Token Advance() {
            //Throw out any newlines, indents, dedents, or whitespace
            List<TokenType> ignoredTypes = new() { TokenType.Newline, TokenType.DM_Indent, TokenType.DM_Dedent, TokenType.DM_Whitespace };
            while (ignoredTypes.Contains(base.Advance().Type)) {
            }

            return Current();
        }
    }
}
