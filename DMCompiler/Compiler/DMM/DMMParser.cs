using DMCompiler.DM;
using DMCompiler.Compiler.DM;
using DMCompiler.Compiler.DM.AST;
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

                if (cellDefinition.Name.Length == _cellNameLength) {
                    map.CellDefinitions.Add(cellDefinition.Name, cellDefinition);
                } else {
                    Emit(WarningCode.BadToken, $"Invalid cell definition name length '{cellDefinition.Name}'");
                }
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

            CellDefinitionJson cellDefinition = new CellDefinitionJson(currentToken.ValueAsString());
            DMASTPath? objectType = Path();
            while (objectType != null) {
                if (!Compiler.DMObjectTree.TryGetDMObject(objectType.Path, out var type) && _skippedTypes.Add(objectType.Path)) {
                    Warning($"Skipping type '{objectType.Path}'");
                }

                MapObjectJson mapObject = new MapObjectJson(type?.Id ?? -1);

                if (Check(TokenType.DM_LeftCurlyBracket)) {
                    DMASTStatement? statement = Statement();

                    while (statement != null) {
                        if (statement is not DMASTObjectVarOverride varOverride) {
                            Emit(WarningCode.InvalidVarDefinition, statement.Location, "Expected a var override");
                            break;
                        }

                        if (!varOverride.ObjectPath.Equals(DreamPath.Root))
                            Compiler.ForcedError(statement.Location, $"Invalid var name '{varOverride.VarName}' in DMM on type {objectType.Path}");

                        Compiler.DMObjectTree.TryGetDMObject(objectType.Path, out var dmObject);
                        DMExpression value = Compiler.DMExpression.Create(dmObject, null, varOverride.Value);
                        if (!value.TryAsJsonRepresentation(out var valueJson))
                            Compiler.ForcedError(statement.Location, $"Failed to serialize value to json ({value})");

                        if(!mapObject.AddVarOverride(varOverride.VarName, valueJson)) {
                            Compiler.ForcedWarning(statement.Location, $"Duplicate var override '{varOverride.VarName}' in DMM on type {objectType.Path}");
                        }

                        CurrentPath = DreamPath.Root;
                        statement = Check(TokenType.DM_Semicolon) ? Statement() : null;
                    }

                    Consume(TokenType.DM_RightCurlyBracket, "Expected '}'");
                }

                if (type != null) {
                    if (type.IsSubtypeOf(DreamPath.Turf)) {
                        cellDefinition.Turf = mapObject;
                    } else if (type.IsSubtypeOf(DreamPath.Area)) {
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

            string blockString = blockStringToken.ValueAsString();
            string[] lines = blockString.Split("\n", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            mapBlock.Height = lines.Length;
            for (int y = 1; y <= lines.Length; y++) {
                string line = lines[y - 1];
                int width = (line.Length / _cellNameLength);

                if (mapBlock.Width < width) mapBlock.Width = width;
                if ((line.Length % _cellNameLength) != 0) {
                    Emit(WarningCode.BadToken, blockStringToken.Location, "Invalid map block row");
                    return null;
                }

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
        if (!Check(TokenType.DM_LeftParenthesis))
            return null;

        DMASTConstantInteger? x = Constant() as DMASTConstantInteger;
        if (x == null) {
            Emit(WarningCode.BadToken, x?.Location ?? CurrentLoc, "Expected an integer");
            return null;
        }

        Consume(TokenType.DM_Comma, "Expected ','");

        DMASTConstantInteger? y = Constant() as DMASTConstantInteger;
        if (y == null) {
            Emit(WarningCode.BadToken, y?.Location ?? CurrentLoc, "Expected an integer");
            return null;
        }

        Consume(TokenType.DM_Comma, "Expected ','");

        DMASTConstantInteger? z = Constant() as DMASTConstantInteger;
        if (z == null) {
            Emit(WarningCode.BadToken, z?.Location ?? CurrentLoc, "Expected an integer");
            return null;
        }

        Consume(TokenType.DM_RightParenthesis, "Expected ')'");
        return (x.Value, y.Value, z.Value + zOffset);
    }

    protected override Token Advance() {
        //Throw out any newlines, indents, dedents, or whitespace
        List<TokenType> ignoredTypes = new() { TokenType.Newline, TokenType.DM_Indent, TokenType.DM_Dedent, TokenType.DM_Whitespace };
        while (ignoredTypes.Contains(base.Advance().Type)) {
        }

        return Current();
    }
}
