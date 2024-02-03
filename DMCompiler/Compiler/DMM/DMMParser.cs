using System;
using DMCompiler.DM;
using System.Collections.Generic;
using DMCompiler.Compiler.DM;
using DMCompiler.Json;

namespace DMCompiler.Compiler.DMM;

internal sealed class DMMParser(DMLexer lexer, int zOffset) : DMParser(lexer) {
    private int _cellNameLength = -1;
    private readonly HashSet<DreamPath> _skippedTypes = new();

    public DreamMapJson ParseMap() {
        DreamMapJson map = new DreamMapJson();

        _cellNameLength = -1;

        bool parsing = true;
        while (parsing) {
            CellDefinitionJson? cellDefinition = ParseCellDefinition();
            if (cellDefinition != null) {
                if (_cellNameLength == -1) _cellNameLength = cellDefinition.Name.Length;
                else if (cellDefinition.Name.Length != _cellNameLength) Error("Invalid cell definition name");

                map.CellDefinitions.Add(cellDefinition.Name, cellDefinition);
            }

            MapBlockJson? mapBlock = ParseMapBlock();
            if (mapBlock != null) {
                int maxX = mapBlock.X + mapBlock.Width - 1;
                int maxY = mapBlock.Y + mapBlock.Height - 1;
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

    public CellDefinitionJson? ParseCellDefinition() {
        Token currentToken = Current();

        if (Check(TokenType.DM_ConstantString)) {
            Consume(TokenType.DM_Equals, "Expected '='");
            Consume(TokenType.DM_LeftParenthesis, "Expected '('");

            CellDefinitionJson cellDefinition = new CellDefinitionJson((string)currentToken.Value);
            DMASTPath? objectType = Path();
            while (objectType != null) {
                bool skipType = !DMObjectTree.TryGetTypeId(objectType.Path, out int typeId);
                if (skipType && _skippedTypes.Add(objectType.Path)) {
                    Warning($"Skipping type '{objectType.Path}'");
                }

                MapObjectJson mapObject = new MapObjectJson(typeId);

                if (Check(TokenType.DM_LeftCurlyBracket)) {
                    DMASTStatement? statement = Statement(requireDelimiter: false);

                    while (statement != null) {
                        DMASTObjectVarOverride? varOverride = statement as DMASTObjectVarOverride;
                        if (varOverride == null) Error("Expected a var override");
                        if (!varOverride.ObjectPath.Equals(DreamPath.Root)) DMCompiler.ForcedError(statement.Location, $"Invalid var name '{varOverride.VarName}' in DMM on type {objectType.Path}");
                        DMExpression value = DMExpression.Create(DMObjectTree.GetDMObject(objectType.Path, false), null, varOverride.Value);
                        if (!value.TryAsJsonRepresentation(out var valueJson)) DMCompiler.ForcedError(statement.Location, $"Failed to serialize value to json ({value})");

                        if(!mapObject.AddVarOverride(varOverride.VarName, valueJson)) {
                            DMCompiler.ForcedWarning(statement.Location, $"Duplicate var override '{varOverride.VarName}' in DMM on type {objectType.Path}");
                        }

                        if (Check(TokenType.DM_Semicolon)) {
                            statement = Statement(requireDelimiter: false);
                        } else {
                            statement = null;
                        }
                    }

                    Consume(TokenType.DM_RightCurlyBracket, "Expected '}'");
                }

                if (!skipType) {
                    if (objectType.Path.IsDescendantOf(DreamPath.Turf)) {
                        cellDefinition.Turf = mapObject;
                    } else if (objectType.Path.IsDescendantOf(DreamPath.Area)) {
                        cellDefinition.Area = mapObject;
                    } else {
                        cellDefinition.Objects.Add(mapObject);
                    }
                }

                if (Check(TokenType.DM_Comma)) {
                    objectType = Path();
                } else {
                    objectType = null;
                }
            }

            Consume(TokenType.DM_RightParenthesis, "Expected ')'");
            return cellDefinition;
        }

        return null;
    }

    public MapBlockJson? ParseMapBlock() {
        (int X, int Y, int Z)? coordinates = Coordinates();

        if (coordinates.HasValue) {
            MapBlockJson mapBlock = new MapBlockJson(coordinates.Value.X, coordinates.Value.Y, coordinates.Value.Z);

            Consume(TokenType.DM_Equals, "Expected '='");
            Token blockStringToken = Current();
            Consume(TokenType.DM_ConstantString, "Expected a constant string");

            string blockString = (string)blockStringToken.Value;
            List<string> lines = new(blockString.Split("\n", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

            mapBlock.Height = lines.Count;
            for (int y = 1; y <= lines.Count; y++) {
                string line = lines[y - 1];
                int width = (line.Length / _cellNameLength);

                if (mapBlock.Width < width) mapBlock.Width = width;
                if ((line.Length % _cellNameLength) != 0) Error("Invalid map block row");

                for (int x = 1; x <= width; x++) {
                    string cell = line.Substring((x - 1) * _cellNameLength, _cellNameLength);

                    mapBlock.Cells.Add(cell);
                }
            }

            return mapBlock;
        } else {
            return null;
        }
    }

    private (int X, int Y, int Z)? Coordinates() {
        if (Check(TokenType.DM_LeftParenthesis)) {
            DMASTConstantInteger? x = Constant() as DMASTConstantInteger;
            if (x == null) Error("Expected an integer");
            Consume(TokenType.DM_Comma, "Expected ','");
            DMASTConstantInteger? y = Constant() as DMASTConstantInteger;
            if (y == null) Error("Expected an integer");
            Consume(TokenType.DM_Comma, "Expected ','");
            DMASTConstantInteger? z = Constant() as DMASTConstantInteger;
            if (z == null) Error("Expected an integer");
            Consume(TokenType.DM_RightParenthesis, "Expected ')'");

            return (x.Value, y.Value, z.Value + zOffset);
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
