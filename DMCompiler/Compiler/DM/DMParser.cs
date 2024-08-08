using DMCompiler.Compiler.DMPreprocessor;
using System;
using System.Collections.Generic;
using System.Linq;
using DMCompiler.Compiler.DM.AST;
using DMCompiler.DM;

namespace DMCompiler.Compiler.DM {
    public partial class DMParser(DMLexer lexer) : Parser<Token>(lexer) {
        protected Location CurrentLoc => Current().Location;
        protected DreamPath CurrentPath = DreamPath.Root;

        private bool _allowVarDeclExpression;

        private static readonly TokenType[] AssignTypes = [
            TokenType.DM_Equals,
            TokenType.DM_PlusEquals,
            TokenType.DM_MinusEquals,
            TokenType.DM_BarEquals,
            TokenType.DM_BarBarEquals,
            TokenType.DM_AndAndEquals,
            TokenType.DM_AndEquals,
            TokenType.DM_AndAndEquals,
            TokenType.DM_StarEquals,
            TokenType.DM_SlashEquals,
            TokenType.DM_LeftShiftEquals,
            TokenType.DM_RightShiftEquals,
            TokenType.DM_XorEquals,
            TokenType.DM_ModulusEquals,
            TokenType.DM_ModulusModulusEquals,
            TokenType.DM_AssignInto
        ];

        /// <remarks>This (and other similar TokenType[] sets here) is public because <see cref="DMPreprocessorParser"/> needs it.</remarks>
        public static readonly TokenType[] ComparisonTypes = [
            TokenType.DM_EqualsEquals,
            TokenType.DM_ExclamationEquals,
            TokenType.DM_TildeEquals,
            TokenType.DM_TildeExclamation
        ];

        public static readonly TokenType[] LtGtComparisonTypes = [
            TokenType.DM_LessThan,
            TokenType.DM_LessThanEquals,
            TokenType.DM_GreaterThan,
            TokenType.DM_GreaterThanEquals
        ];

        private static readonly TokenType[] ShiftTypes = [
            TokenType.DM_LeftShift,
            TokenType.DM_RightShift
        ];

        public static readonly TokenType[] PlusMinusTypes = [
            TokenType.DM_Plus,
            TokenType.DM_Minus
        ];

        public static readonly TokenType[] MulDivModTypes = [
            TokenType.DM_Star,
            TokenType.DM_Slash,
            TokenType.DM_Modulus,
            TokenType.DM_ModulusModulus

        ];

        private static readonly TokenType[] DereferenceTypes = [
            TokenType.DM_Period,
            TokenType.DM_Colon,
            TokenType.DM_DoubleColon, // not a dereference, but shares the same precedence
            TokenType.DM_QuestionPeriod,
            TokenType.DM_QuestionColon,
            TokenType.DM_QuestionLeftBracket
        ];

        private static readonly TokenType[] WhitespaceTypes = [
            TokenType.DM_Whitespace,
            TokenType.DM_Indent,
            TokenType.DM_Dedent
        ];

        private static readonly TokenType[] IdentifierTypes = [TokenType.DM_Identifier, TokenType.DM_Step];

        /// <summary>
        /// Used by <see cref="PathElement"/> to determine, keywords that may actually just be identifiers of a typename within a path, in a given context.
        /// </summary>
        private static readonly TokenType[] ValidPathElementTokens = [
            TokenType.DM_Identifier,
            TokenType.DM_Var,
            TokenType.DM_Proc,
            TokenType.DM_Step,
            TokenType.DM_Throw,
            TokenType.DM_Null,
            TokenType.DM_Switch,
            TokenType.DM_Spawn,
            TokenType.DM_Do,
            TokenType.DM_While,
            TokenType.DM_For
            //BYOND fails on DM_In, don't include that
        ];

        private static readonly TokenType[] ForSeparatorTypes = [
            TokenType.DM_Semicolon,
            TokenType.DM_Comma
        ];

        private static readonly TokenType[] OperatorOverloadTypes = [
            TokenType.DM_And,
            TokenType.DM_AndEquals,
            TokenType.DM_AssignInto,
            TokenType.DM_Bar,
            TokenType.DM_BarEquals,
            TokenType.DM_DoubleSquareBracket,
            TokenType.DM_DoubleSquareBracketEquals,
            TokenType.DM_GreaterThan,
            TokenType.DM_GreaterThanEquals,
            TokenType.DM_RightShift,
            TokenType.DM_RightShiftEquals,
            TokenType.DM_LeftShift,
            TokenType.DM_LeftShiftEquals,
            TokenType.DM_LessThan,
            TokenType.DM_LessThanEquals,
            TokenType.DM_Minus,
            TokenType.DM_MinusEquals,
            TokenType.DM_MinusMinus,
            TokenType.DM_Modulus,
            TokenType.DM_ModulusEquals,
            TokenType.DM_ModulusModulus,
            TokenType.DM_ModulusModulusEquals,
            TokenType.DM_Plus,
            TokenType.DM_PlusEquals,
            TokenType.DM_PlusPlus,
            TokenType.DM_Slash,
            TokenType.DM_SlashEquals,
            TokenType.DM_Star,
            TokenType.DM_StarEquals,
            TokenType.DM_StarStar,
            TokenType.DM_Tilde,
            TokenType.DM_TildeEquals,
            TokenType.DM_TildeExclamation,
            TokenType.DM_Xor,
            TokenType.DM_XorEquals,
            TokenType.DM_ConstantString
        ];

        // TEMPORARY - REMOVE WHEN IT MATCHES THE ABOVE
        private static readonly TokenType[] ImplementedOperatorOverloadTypes = [
            TokenType.DM_Plus,
            TokenType.DM_Minus,
            TokenType.DM_Star,
            TokenType.DM_StarEquals,
            TokenType.DM_Slash,
            TokenType.DM_SlashEquals,
            TokenType.DM_Bar,
        ];

        public DMASTFile File() {
            var loc = Current().Location;
            List<DMASTStatement> statements = new();

            while (Current().Type != TokenType.EndOfFile) {
                List<DMASTStatement>? blockInner = BlockInner();
                if (blockInner != null)
                    statements.AddRange(blockInner);

                if (Current().Type != TokenType.EndOfFile) {
                    Token skipFrom = Current();
                    LocateNextTopLevel();
                    Warning($"Error recovery had to skip to {Current().Location}", token: skipFrom);
                }
            }

            Newline();
            Consume(TokenType.EndOfFile, "Expected EOF");
            return new DMASTFile(loc, new DMASTBlockInner(loc, statements.ToArray()));
        }

        private List<DMASTStatement>? BlockInner() {
            List<DMASTStatement> statements = new();

            do {
                Whitespace();
                DreamPath oldPath = CurrentPath;
                DMASTStatement? statement = Statement();

                CurrentPath = oldPath;

                if (statement != null) {
                    if (!PeekDelimiter() && Current().Type is not (TokenType.DM_Dedent or TokenType.DM_RightCurlyBracket or TokenType.EndOfFile)) {
                        Emit(WarningCode.BadToken, "Expected end of object statement");
                    }

                    Whitespace();
                    statements.Add(statement);
                } else {
                    if (statements.Count == 0) return null;
                }
            } while (Delimiter());
            Whitespace();

            return statements;
        }

        protected DMASTStatement? Statement() {
            var loc = CurrentLoc;

            DMASTPath? path = Path();
            if (path is null)
                return null;
            Whitespace();
            CurrentPath = CurrentPath.Combine(path.Path);

            //Proc definition
            if (Check(TokenType.DM_LeftParenthesis)) {
                DMCompiler.VerbosePrint($"Parsing proc {CurrentPath}()");
                BracketWhitespace();
                var parameters = DefinitionParameters(out var wasIndeterminate);

                if (Current().Type != TokenType.DM_RightParenthesis && Current().Type != TokenType.DM_Comma && !wasIndeterminate) {
                    if (parameters.Count > 0) // Separate error handling mentions the missing right-paren
                        Emit(WarningCode.BadToken, $"{parameters.Last().Name}: missing comma ',' or right-paren ')'");

                    parameters.AddRange(DefinitionParameters(out wasIndeterminate));
                }

                if (!wasIndeterminate && Current().Type != TokenType.DM_RightParenthesis && Current().Type != TokenType.EndOfFile) {
                    // BYOND doesn't specify the arg
                    Emit(WarningCode.BadToken, $"Bad argument definition '{Current().PrintableText}'");
                    Advance();
                    BracketWhitespace();
                    Check(TokenType.DM_Comma);
                    BracketWhitespace();
                    parameters.AddRange(DefinitionParameters(out _));
                }

                BracketWhitespace();
                ConsumeRightParenthesis();
                Whitespace();

                // Proc return type
                var types = AsComplexTypes();

                DMASTProcBlockInner? procBlock = ProcBlock();
                if (procBlock is null) {
                    DMASTProcStatement? procStatement = ProcStatement();

                    if (procStatement is not null) {
                        procBlock = new DMASTProcBlockInner(loc, procStatement);
                    }
                }

                if (procBlock?.Statements.Length is 0 or null) {
                    DMCompiler.Emit(WarningCode.EmptyProc, loc,
                        "Empty proc detected - add an explicit \"return\" statement");
                }

                if (path.IsOperator) {
                    List<DMASTProcStatement> procStatements = procBlock.Statements.ToList();
                    Location tokenLoc = procBlock.Location;
                    //add ". = src" as the first expression in the operator
                    DMASTProcStatementExpression assignEqSrc = new DMASTProcStatementExpression(tokenLoc,
                        new DMASTAssign(tokenLoc, new DMASTCallableSelf(tokenLoc),
                            new DMASTIdentifier(tokenLoc, "src")));
                    procStatements.Insert(0, assignEqSrc);

                    procBlock = new DMASTProcBlockInner(loc, procStatements.ToArray(), procBlock.SetStatements);
                }

                return new DMASTProcDefinition(loc, CurrentPath, parameters.ToArray(), procBlock, types);
            }

            //Object definition
            if (Block() is { } block) {
                DMCompiler.VerbosePrint($"Parsed object {CurrentPath}");
                return new DMASTObjectDefinition(loc, CurrentPath, block);
            }

            //Var definition(s)
            if (CurrentPath.FindElement("var") != -1) {
                DreamPath varPath = CurrentPath;
                List<DMASTObjectVarDefinition> varDefinitions = new();

                while (true) {
                    Whitespace();

                    DMASTExpression? value = PathArray(ref varPath);

                    if (Check(TokenType.DM_Equals)) {
                        if (value != null) Warning("List doubly initialized");

                        Whitespace();
                        value = Expression();
                        RequireExpression(ref value);
                    } else if (value == null) {
                        value = new DMASTConstantNull(loc);
                    }

                    var valType = AsComplexTypes() ?? DMValueType.Anything;
                    var varDef = new DMASTObjectVarDefinition(loc, varPath, value, valType);

                    varDefinitions.Add(varDef);
                    if (Check(TokenType.DM_Comma)) {
                        Whitespace();
                        DMASTPath? newVarPath = Path();

                        if (newVarPath == null) {
                            Emit(WarningCode.InvalidVarDefinition, "Expected a var definition");
                            break;
                        } else if (newVarPath.Path.Elements.Length > 1) { // TODO: This is valid DM
                            Emit(WarningCode.BadToken, newVarPath.Location, "Invalid var name");
                            break;
                        }

                        varPath = CurrentPath.AddToPath("../" + newVarPath.Path.PathString);
                    } else {
                        break;
                    }
                }

                return (varDefinitions.Count == 1)
                    ? varDefinitions[0]
                    : new DMASTMultipleObjectVarDefinitions(loc, varDefinitions.ToArray());
            }

            //Var override
            if (Check(TokenType.DM_Equals)) {
                Whitespace();
                DMASTExpression? value = Expression();
                RequireExpression(ref value);

                return new DMASTObjectVarOverride(loc, CurrentPath, value);
            }

            //Empty object definition
            DMCompiler.VerbosePrint($"Parsed object {CurrentPath}");
            return new DMASTObjectDefinition(loc, CurrentPath, null);
        }

        /// <summary>
        /// Tries to read in a path. Returns null if one cannot be constructed.
        /// </summary>
        protected DMASTPath? Path(bool expression = false) {
            Token firstToken = Current();
            DreamPath.PathType pathType = DreamPath.PathType.Relative;
            bool hasPathTypeToken = true;

            if (Check(TokenType.DM_Slash)) {
                // Check if they did "/.whatever/" instead of ".whatever/"
                pathType = Check(TokenType.DM_Period) ? DreamPath.PathType.UpwardSearch : DreamPath.PathType.Absolute;
            } else if (Check(TokenType.DM_Colon)) {
                pathType = DreamPath.PathType.DownwardSearch;
            } else if (Check(TokenType.DM_Period)) {
                pathType = DreamPath.PathType.UpwardSearch;
            } else {
                hasPathTypeToken = false;

                if (expression) return null;
            }

            string? pathElement = PathElement();
            if (pathElement != null) {
                List<string> pathElements = [pathElement];
                bool operatorFlag = false;
                while (pathElement != null && Check(TokenType.DM_Slash)) {
                    pathElement = PathElement();

                    if (pathElement != null) {
                        if(pathElement == "operator") {
                            Token operatorToken = Current();
                            if(Current().Type == TokenType.DM_Slash) {
                                //Up to this point, it's ambiguous whether it's a slash to mean operator/(), like the division operator overload
                                //or "operator" just being used as a normal type name, as in a/operator/b/c/d
                                Token peekToken = Advance();
                                if (peekToken.Type == TokenType.DM_LeftParenthesis) { // Disambiguated as an overload
                                    operatorFlag = true;
                                    pathElement += operatorToken.PrintableText;
                                } else { //Otherwise it's just a normal path, resume
                                    ReuseToken(operatorToken);
                                    Emit(WarningCode.SoftReservedKeyword, "Using \"operator\" as a path element is ambiguous");
                                }
                            } else if (Check(OperatorOverloadTypes)) {
                                if (operatorToken is { Type: TokenType.DM_ConstantString, Value: not "" }) {
                                    DMCompiler.Emit(WarningCode.BadToken, operatorToken.Location,
                                        "The quotes in a stringify overload must be empty");
                                }

                                if (!ImplementedOperatorOverloadTypes.Contains(operatorToken.Type)) {
                                    DMCompiler.UnimplementedWarning(operatorToken.Location,
                                        $"operator{operatorToken.PrintableText} overloads are not implemented. They will be defined but never called.");
                                }

                                operatorFlag = true;
                                pathElement += operatorToken.PrintableText;
                            }
                        }

                        pathElements.Add(pathElement);
                    }
                }

                return new DMASTPath(firstToken.Location, new DreamPath(pathType, pathElements.ToArray()), operatorFlag);
            } else if (hasPathTypeToken) {
                if (expression) ReuseToken(firstToken);

                return null;
            }

            return null;
        }

        /// <summary>
        /// Extracts the text from this token if it is reasonable for it to appear as a typename in a path.
        /// </summary>
        /// <returns>The <see cref="Token.Text"/> if this is a valid path element, null otherwise.</returns>
        private string? PathElement() {
            Token elementToken = Current();
            if (Check(ValidPathElementTokens)) {
                return elementToken.Text;
            } else {
                return null;
            }
        }

        private DMASTDimensionalList? PathArray(ref DreamPath path) {
            if (Current().Type == TokenType.DM_LeftBracket || Current().Type == TokenType.DM_DoubleSquareBracket) {
                var loc = Current().Location;

                // Trying to use path.IsDescendantOf(DreamPath.List) here doesn't work
                if (!path.Elements[..^1].Contains("list")) {
                    var elements = path.Elements.ToList();
                    elements.Insert(elements.IndexOf("var") + 1, "list");
                    path = new DreamPath("/" + string.Join("/", elements));
                }

                List<DMASTExpression> sizes = new(2); // Most common is 1D or 2D lists

                while (true) {
                    if(Check(TokenType.DM_DoubleSquareBracket))
                        Whitespace();
                    else if(Check(TokenType.DM_LeftBracket)) {
                        Whitespace();
                        var size = Expression();
                        if (size is not null) {
                            sizes.Add(size);
                        }

                        ConsumeRightBracket();
                        Whitespace();
                    } else
                        break;
                }

                if (sizes.Count > 0) {
                    return new DMASTDimensionalList(loc, sizes);
                }
            }

            return null;
        }

        private IDMASTCallable? Callable() {
            var loc = Current().Location;
            if (Check(TokenType.DM_SuperProc)) return new DMASTCallableSuper(loc);
            if (Check(TokenType.DM_Period)) return new DMASTCallableSelf(loc);

            return null;
        }

        private DMASTExpression? ParseScopeIdentifier(DMASTExpression? expression) {
            do {
                var identifier = Identifier();
                if (identifier == null) {
                    DMCompiler.Emit(WarningCode.BadToken, Current().Location, "Identifier expected");
                    return null;
                }

                var location = expression?.Location ?? identifier.Location; // TODO: Should be on the :: token if expression is null
                var parameters = ProcCall();
                expression = new DMASTScopeIdentifier(location, expression, identifier.Identifier, parameters);
            } while (Check(TokenType.DM_DoubleColon));

            return expression;
        }

        private DMASTIdentifier? Identifier() {
            Token token = Current();
            return Check(IdentifierTypes) ? new DMASTIdentifier(token.Location, token.Text) : null;
        }

        private DMASTBlockInner? Block() {
            Token beforeBlockToken = Current();
            bool hasNewline = Newline();

            DMASTBlockInner? block = BracedBlock();
            block ??= IndentedBlock();

            if (block == null && hasNewline) {
                ReuseToken(beforeBlockToken);
            }

            return block;
        }

        private DMASTBlockInner? BracedBlock() {
            var loc = Current().Location;
            if (Check(TokenType.DM_LeftCurlyBracket)) {
                Whitespace();
                Newline();
                bool isIndented = Check(TokenType.DM_Indent);
                List<DMASTStatement>? blockInner = BlockInner();
                if (isIndented) Check(TokenType.DM_Dedent);
                Newline();
                Consume(TokenType.DM_RightCurlyBracket, "Expected '}'");

                return new DMASTBlockInner(loc, blockInner.ToArray());
            }

            return null;
        }

        private DMASTBlockInner? IndentedBlock() {
            var loc = Current().Location;
            if (Check(TokenType.DM_Indent)) {
                List<DMASTStatement>? blockInner = BlockInner();

                if (blockInner != null) {
                    Newline();
                    Consume(TokenType.DM_Dedent, "Expected dedent");

                    return new DMASTBlockInner(loc, blockInner.ToArray());
                }
            }

            return null;
        }

        private DMASTProcBlockInner? ProcBlock() {
            Token beforeBlockToken = Current();
            bool hasNewline = Newline();

            DMASTProcBlockInner? procBlock = BracedProcBlock();
            procBlock ??= IndentedProcBlock();

            if (procBlock == null && hasNewline) {
                ReuseToken(beforeBlockToken);
            }

            return procBlock;
        }

        private DMASTProcBlockInner? BracedProcBlock() {
            var loc = Current().Location;
            if (Check(TokenType.DM_LeftCurlyBracket)) {
                DMASTProcBlockInner? block;

                Whitespace();
                Newline();
                if (Current().Type == TokenType.DM_Indent) {
                    block = IndentedProcBlock();
                    Newline();
                    Consume(TokenType.DM_RightCurlyBracket, "Expected '}'");
                } else {
                    List<DMASTProcStatement> statements = new();
                    List<DMASTProcStatement> setStatements = new(); // set statements are weird and must be held separately.

                    do {
                        (List<DMASTProcStatement>? stmts, List<DMASTProcStatement>? setStmts) = ProcBlockInner(); // Hope you understand tuples
                        if (stmts is not null) statements.AddRange(stmts);
                        if (setStmts is not null) setStatements.AddRange(setStmts);

                        if (!Check(TokenType.DM_RightCurlyBracket)) {
                            Emit(WarningCode.BadToken, "Expected end of braced block");
                            Check(TokenType.DM_Dedent); // Have to do this ensure that the current token will ALWAYS move forward,
                                                        // and not get stuck once we reach this branch!
                            LocateNextStatement();
                            Delimiter();
                        } else {
                            break;
                        }
                    } while (true);

                    block = new DMASTProcBlockInner(loc, statements.ToArray(), setStatements.ToArray());
                }

                return block;
            }

            return null;
        }

        private DMASTProcBlockInner? IndentedProcBlock() {
            var loc = Current().Location;
            if (Check(TokenType.DM_Indent)) {
                List<DMASTProcStatement> statements = new();
                List<DMASTProcStatement> setStatements = new(); // set statements are weird and must be held separately.

                do {
                    (List<DMASTProcStatement>? statements, List<DMASTProcStatement>? setStatements) blockInner = ProcBlockInner();
                    if (blockInner.statements is not null)
                        statements.AddRange(blockInner.statements);
                    if (blockInner.setStatements is not null)
                        setStatements.AddRange(blockInner.setStatements);

                    if (!Check(TokenType.DM_Dedent)) {
                        Emit(WarningCode.BadToken, "Expected end of proc statement");
                        LocateNextStatement();
                        Delimiter();
                    } else {
                        break;
                    }
                } while (true);

                return new DMASTProcBlockInner(loc, statements.ToArray(), setStatements.ToArray());
            }

            return null;
        }

        private (List<DMASTProcStatement>?, List<DMASTProcStatement>?) ProcBlockInner() {
            List<DMASTProcStatement> procStatements = new();
            List<DMASTProcStatement> setStatements = new(); // We have to store them separately because they're evaluated first

            DMASTProcStatement? statement;
            do {
                Whitespace();
                statement = ProcStatement();

                if (statement is not null) {
                    Whitespace();
                    if(statement.IsAggregateOr<DMASTProcStatementSet>())
                        setStatements.Add(statement);
                    else
                        procStatements.Add(statement);
                }
            } while (Delimiter() || statement is DMASTProcStatementLabel);
            Whitespace();

            return (procStatements.Count > 0 ? procStatements : null, setStatements.Count > 0 ? setStatements : null);
        }

        private DMASTProcStatement? ProcStatement() {
            var loc = Current().Location;

            if (Current().Type == TokenType.DM_Semicolon) { // A lone semicolon creates a "null statement" (like C)
                // Note that we do not consume the semicolon here
                return new DMASTNullProcStatement(loc);
            }

            var leadingColon = Check(TokenType.DM_Colon);

            DMASTExpression? expression = null;
            if (Current().Type != TokenType.DM_Var) {
                expression = Expression();
            }

            if (leadingColon && expression is not DMASTIdentifier) {
                Emit(WarningCode.BadToken, expression?.Location ?? CurrentLoc, "Expected a label identifier");
                return new DMASTInvalidProcStatement(loc);
            }

            if (expression != null) {
                switch (expression) {
                    case DMASTIdentifier identifier:
                        // This could be a sleep without parentheses
                        if (!Check(TokenType.DM_Colon) && !leadingColon && identifier.Identifier == "sleep") {
                            var procIdentifier = new DMASTCallableProcIdentifier(expression.Location, "sleep");
                            // The argument is optional
                            var sleepTime = Expression() ?? new DMASTConstantNull(Location.Internal);

                            // TODO: Make sleep an opcode
                            expression = new DMASTProcCall(expression.Location, procIdentifier,
                                new[] { new DMASTCallParameter(sleepTime.Location, sleepTime) });
                            break;
                        }

                        // But it was a label
                        return Label(identifier);
                    case DMASTRightShift rightShift:
                        // A right shift on its own becomes a special "input" statement
                        return new DMASTProcStatementInput(loc, rightShift.LHS, rightShift.RHS);
                    case DMASTLeftShift leftShift: {
                        // A left shift on its own becomes a special "output" statement
                        // Or something else depending on what's on the right ( browse(), browse_rsc(), output(), etc )
                        if (leftShift.RHS.GetUnwrapped() is DMASTProcCall {Callable: DMASTCallableProcIdentifier identifier} procCall) {
                            switch (identifier.Identifier) {
                                case "browse": {
                                    if (procCall.Parameters.Length != 1 && procCall.Parameters.Length != 2) {
                                        Emit(WarningCode.InvalidArgumentCount, procCall.Location,
                                            "browse() requires 1 or 2 parameters");
                                        return new DMASTInvalidProcStatement(procCall.Location);
                                    }

                                    DMASTExpression body = procCall.Parameters[0].Value;
                                    DMASTExpression options = (procCall.Parameters.Length == 2)
                                        ? procCall.Parameters[1].Value
                                        : new DMASTConstantNull(loc);
                                    return new DMASTProcStatementBrowse(loc, leftShift.LHS, body, options);
                                }
                                case "browse_rsc": {
                                    if (procCall.Parameters.Length != 1 && procCall.Parameters.Length != 2) {
                                        Emit(WarningCode.InvalidArgumentCount, procCall.Location,
                                            "browse_rsc() requires 1 or 2 parameters");
                                        return new DMASTInvalidProcStatement(procCall.Location);
                                    }

                                    DMASTExpression file = procCall.Parameters[0].Value;
                                    DMASTExpression filepath = (procCall.Parameters.Length == 2)
                                        ? procCall.Parameters[1].Value
                                        : new DMASTConstantNull(loc);
                                    return new DMASTProcStatementBrowseResource(loc, leftShift.LHS, file, filepath);
                                }
                                case "output": {
                                    if (procCall.Parameters.Length != 2) {
                                        Emit(WarningCode.InvalidArgumentCount, procCall.Location,
                                            "output() requires 2 parameters");
                                        return new DMASTInvalidProcStatement(procCall.Location);
                                    }

                                    DMASTExpression msg = procCall.Parameters[0].Value;
                                    DMASTExpression control = procCall.Parameters[1].Value;
                                    return new DMASTProcStatementOutputControl(loc, leftShift.LHS, msg, control);
                                }
                                case "ftp": {
                                    if (procCall.Parameters.Length is not 1 and not 2) {
                                        Emit(WarningCode.InvalidArgumentCount, procCall.Location,
                                            "ftp() requires 1 or 2 parameters");
                                        return new DMASTInvalidProcStatement(procCall.Location);
                                    }

                                    DMASTExpression file = procCall.Parameters[0].Value;
                                    DMASTExpression name = (procCall.Parameters.Length == 2)
                                        ? procCall.Parameters[1].Value
                                        : new DMASTConstantNull(loc);
                                    return new DMASTProcStatementFtp(loc, leftShift.LHS, file, name);
                                }
                            }
                        }

                        return new DMASTProcStatementOutput(loc, leftShift.LHS, leftShift.RHS);
                    }
                }

                return new DMASTProcStatementExpression(loc, expression);
            } else {
                // These are sorted by frequency
                DMASTProcStatement? procStatement = If();
                procStatement ??= Return();
                procStatement ??= ProcVarDeclaration();
                procStatement ??= For();
                procStatement ??= Set();
                procStatement ??= Switch();
                procStatement ??= Continue();
                procStatement ??= Break();
                procStatement ??= Spawn();
                procStatement ??= While();
                procStatement ??= DoWhile();
                procStatement ??= Throw();
                procStatement ??= Del();
                procStatement ??= TryCatch();
                procStatement ??= Goto();

                if (procStatement != null) {
                    Whitespace();
                }

                return procStatement;
            }
        }

        private DMASTProcStatement? ProcVarDeclaration(bool allowMultiple = true) {
            Token firstToken = Current();
            bool wasSlash = Check(TokenType.DM_Slash);

            if (Check(TokenType.DM_Var)) {
                if (wasSlash) {
                    Emit(WarningCode.InvalidVarDefinition, "Unsupported root variable declaration");
                    // Go on to treat it as a normal var
                }

                Whitespace(); // We have to consume whitespace here since "var foo = 1" (for example) is valid DM code.
                DMASTProcStatementVarDeclaration[]? vars = ProcVarEnd(allowMultiple);
                if (vars == null) {
                    Emit(WarningCode.InvalidVarDefinition, "Expected a var declaration");
                    return new DMASTInvalidProcStatement(firstToken.Location);
                }

                if (vars.Length > 1)
                    return new DMASTAggregate<DMASTProcStatementVarDeclaration>(firstToken.Location, vars);
                return vars[0];

            } else if (wasSlash) {
                ReuseToken(firstToken);
            }

            return null;
        }

        /// <summary>
        /// <see langword="WARNING:"/> This proc calls itself recursively.
        /// </summary>
        private DMASTProcStatementVarDeclaration[]? ProcVarBlock(DMASTPath? varPath) {
            Token newlineToken = Current();
            bool hasNewline = Newline();

            if (Check(TokenType.DM_Indent)) {
                List<DMASTProcStatementVarDeclaration> varDeclarations = new();

                while (!Check(TokenType.DM_Dedent)) {
                    DMASTProcStatementVarDeclaration[]? varDecl = ProcVarEnd(true, path: varPath);
                    if (varDecl != null) {
                        varDeclarations.AddRange(varDecl);
                    } else {
                        Emit(WarningCode.InvalidVarDefinition, "Expected a var declaration");
                    }

                    Whitespace();
                    Delimiter();
                    Whitespace();
                }

                return varDeclarations.ToArray();
            } else if (Check(TokenType.DM_LeftCurlyBracket)) {
                Whitespace();
                Newline();
                bool isIndented = Check(TokenType.DM_Indent);

                List<DMASTProcStatementVarDeclaration> varDeclarations = new();
                TokenType type = isIndented ? TokenType.DM_Dedent : TokenType.DM_RightCurlyBracket;
                while (!Check(type)) {
                    DMASTProcStatementVarDeclaration[]? varDecl = ProcVarEnd(true, path: varPath);
                    Delimiter();
                    Whitespace();
                    if (varDecl == null) {
                        Emit(WarningCode.InvalidVarDefinition, "Expected a var declaration");
                        continue;
                    }

                    varDeclarations.AddRange(varDecl);
                }

                if (isIndented) Consume(TokenType.DM_RightCurlyBracket, "Expected '}'");
                if (isIndented) {
                    Newline();
                    Consume(TokenType.DM_RightCurlyBracket, "Expected '}'");
                }
                return varDeclarations.ToArray();
            }
            else if (hasNewline) {
                ReuseToken(newlineToken);
            }

            return null;
        }

        private DMASTProcStatementVarDeclaration[]? ProcVarEnd(bool allowMultiple, DMASTPath? path = null) {
            var loc = Current().Location;
            DMASTPath? varPath = Path();

            if (allowMultiple) {
                DMASTProcStatementVarDeclaration[]? block = ProcVarBlock(varPath);
                if (block != null) return block;
            }

            if (varPath == null) return null;
            if (path != null) varPath = new DMASTPath(loc, path.Path.Combine(varPath.Path));

            List<DMASTProcStatementVarDeclaration> varDeclarations = new();
            while (true) {
                Whitespace();
                DMASTExpression? value = PathArray(ref varPath.Path);

                if (Check(TokenType.DM_Equals)) {
                    Whitespace();
                    value = Expression();
                    RequireExpression(ref value);
                }

                var valType = AsComplexTypes() ?? DMValueType.Anything;

                varDeclarations.Add(new DMASTProcStatementVarDeclaration(loc, varPath, value, valType));
                if (allowMultiple && Check(TokenType.DM_Comma)) {
                    Whitespace();
                    varPath = Path();
                    if (varPath == null) {
                        Emit(WarningCode.InvalidVarDefinition, "Expected a var declaration");
                        break;
                    }
                } else {
                    break;
                }
            }

            return varDeclarations.ToArray();
        }

        /// <summary>
        /// Similar to <see cref="ProcVarBlock(DMASTPath)"/> except it handles blocks of set declarations. <br/>
        /// <see langword="TODO:"/> See if we can combine the repetitive code between this and ProcVarBlock.
        /// </summary>
        private DMASTProcStatementSet[]? ProcSetBlock() {
            Token newlineToken = Current();
            bool hasNewline = Newline();

            if (Check(TokenType.DM_Indent)) {
                List<DMASTProcStatementSet> setDeclarations = new();

                while (!Check(TokenType.DM_Dedent)) {
                    DMASTProcStatementSet[] setDecl = ProcSetEnd(false); // Repetitive nesting is a no-no here

                    setDeclarations.AddRange(setDecl);

                    Whitespace();
                    Delimiter();
                    Whitespace();
                }

                return setDeclarations.ToArray();
            } else if (Check(TokenType.DM_LeftCurlyBracket)) {
                Whitespace();
                Newline();
                bool isIndented = Check(TokenType.DM_Indent);

                List<DMASTProcStatementSet> setDeclarations = new();
                TokenType type = isIndented ? TokenType.DM_Dedent : TokenType.DM_RightCurlyBracket;
                while (!Check(type)) {
                    DMASTProcStatementSet[] setDecl = ProcSetEnd(true);
                    Delimiter();
                    Whitespace();

                    setDeclarations.AddRange(setDecl);
                }

                if (isIndented) Consume(TokenType.DM_RightCurlyBracket, "Expected '}'");
                if (isIndented) {
                    Newline();
                    Consume(TokenType.DM_RightCurlyBracket, "Expected '}'");
                }

                return setDeclarations.ToArray();
            } else if (hasNewline) {
                ReuseToken(newlineToken);
            }

            return null;
        }

        /// <param name="allowMultiple">This may look like a derelict of ProcVarEnd but it's not;<br/>
        /// Set does not allow path-based nesting of declarations the way var does, so we only allow nesting once, deactivating it thereafter.</param>
        private DMASTProcStatementSet[] ProcSetEnd(bool allowMultiple) {
            var loc = Current().Location;

            if (allowMultiple) {
                DMASTProcStatementSet[]? block = ProcSetBlock();
                if (block != null) return block;
            }

            List<DMASTProcStatementSet> setDeclarations = new(); // It's a list even in the non-block case because we could be comma-separated right mcfricking now
            while (true) { // x [in|=] y{, a [in|=] b} or something. I'm a comment, not a formal BNF expression.
                Whitespace();
                Token attributeToken = Current();
                if(!Check(TokenType.DM_Identifier)) {
                    Emit(WarningCode.BadToken, "Expected an identifier for set declaration");
                    return setDeclarations.ToArray();
                }
                Whitespace();
                TokenType consumed = Consume(new[] { TokenType.DM_Equals, TokenType.DM_In },"Expected a 'in' or '=' for set declaration");
                bool wasInKeyword = (consumed == TokenType.DM_In);
                Whitespace();
                DMASTExpression? value = Expression();
                RequireExpression(ref value);
                //AsTypes(); // Intentionally not done because the 'as' keyword just kinda.. doesn't work here. I dunno.

                setDeclarations.Add(new DMASTProcStatementSet(loc, attributeToken.Text, value, wasInKeyword));
                if (!allowMultiple)
                    break;
                if (!Check(TokenType.DM_Comma))
                    break;
                Whitespace();
                // and continue!
            }

            return setDeclarations.ToArray();
        }

        private DMASTProcStatementReturn? Return() {
            var loc = Current().Location;

            if (Check(TokenType.DM_Return)) {
                Whitespace();
                DMASTExpression? value = Expression();

                return new DMASTProcStatementReturn(loc, value);
            } else {
                return null;
            }
        }

        private DMASTProcStatementBreak? Break() {
            var loc = Current().Location;

            if (Check(TokenType.DM_Break)) {
                Whitespace();
                DMASTIdentifier? label = Identifier();

                return new DMASTProcStatementBreak(loc, label);
            } else {
                return null;
            }
        }

        private DMASTProcStatementContinue? Continue() {
            var loc = Current().Location;

            if (Check(TokenType.DM_Continue)) {
                Whitespace();
                DMASTIdentifier? label = Identifier();

                return new DMASTProcStatementContinue(loc, label);
            } else {
                return null;
            }
        }

        private DMASTProcStatement? Goto() {
            var loc = Current().Location;

            if (Check(TokenType.DM_Goto)) {
                Whitespace();
                DMASTIdentifier? label = Identifier();

                if (label == null) {
                    Emit(WarningCode.BadToken, "Expected a label");
                    return new DMASTInvalidProcStatement(loc);
                }

                return new DMASTProcStatementGoto(loc, label);
            } else {
                return null;
            }
        }

        private DMASTProcStatementDel? Del() {
            var loc = Current().Location;

            if (Check(TokenType.DM_Del)) {
                Whitespace();
                bool hasParenthesis = Check(TokenType.DM_LeftParenthesis);
                Whitespace();
                DMASTExpression? value = Expression();
                RequireExpression(ref value, "Expected value to delete");
                if (hasParenthesis) ConsumeRightParenthesis();

                return new DMASTProcStatementDel(loc, value);
            } else {
                return null;
            }
        }

        /// <returns>Either a <see cref="DMASTProcStatementSet"/> or a DMASTAggregate that acts as a container for them. May be null.</returns>
        private DMASTProcStatement? Set() {
            var loc = Current().Location;

            if (Check(TokenType.DM_Set)) {
                Whitespace();

                DMASTProcStatementSet[] sets = ProcSetEnd(true);
                if (sets.Length == 0) {
                    Emit(WarningCode.InvalidSetStatement, "Expected set declaration");
                    return new DMASTInvalidProcStatement(loc);
                }

                if (sets.Length > 1)
                    return new DMASTAggregate<DMASTProcStatementSet>(loc, sets);
                return sets[0];
            }

            return null;
        }

        private DMASTProcStatementSpawn? Spawn() {
            var loc = Current().Location;

            if (Check(TokenType.DM_Spawn)) {
                Whitespace();
                bool hasArg = Check(TokenType.DM_LeftParenthesis);
                DMASTExpression? delay = null;

                if (hasArg) {
                    Whitespace();

                    if (!Check(TokenType.DM_RightParenthesis)) {
                        delay = Expression();
                        RequireExpression(ref delay, "Expected a delay");

                        ConsumeRightParenthesis();
                    }

                    Whitespace();
                }

                Newline();

                DMASTProcBlockInner? body = ProcBlock();
                if (body == null) {
                    DMASTProcStatement? statement = ProcStatement();

                    if (statement != null) {
                        body = new DMASTProcBlockInner(loc, statement);
                    } else {
                        Emit(WarningCode.BadExpression, "Expected body or statement");
                        body = new DMASTProcBlockInner(loc);
                    }
                }

                return new DMASTProcStatementSpawn(loc, delay ?? new DMASTConstantInteger(loc, 0), body);
            } else {
                return null;
            }
        }

        private DMASTProcStatementIf? If() {
            var loc = Current().Location;

            if (Check(TokenType.DM_If)) {
                Whitespace();
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                BracketWhitespace();
                DMASTExpression? condition = Expression();
                RequireExpression(ref condition, "Expected a condition");

                if (condition is DMASTAssign) {
                    Emit(WarningCode.AssignmentInConditional, condition.Location, "Assignment in conditional");
                }

                BracketWhitespace();
                ConsumeRightParenthesis();
                Whitespace();
                Check(TokenType.DM_Colon);
                Whitespace();

                DMASTProcStatement? procStatement = ProcStatement();
                DMASTProcBlockInner? elseBody = null;
                DMASTProcBlockInner? body = (procStatement != null)
                    ? new DMASTProcBlockInner(loc, procStatement)
                    : ProcBlock();
                body ??= new DMASTProcBlockInner(loc);

                Token afterIfBody = Current();
                bool newLineAfterIf = Delimiter();
                if (newLineAfterIf) Whitespace();
                if (Check(TokenType.DM_Else)) {
                    Whitespace();
                    Check(TokenType.DM_Colon);
                    Whitespace();
                    procStatement = ProcStatement();

                    elseBody = (procStatement != null)
                        ? new DMASTProcBlockInner(loc, procStatement)
                        : ProcBlock();
                    elseBody ??= new DMASTProcBlockInner(loc);
                } else if (newLineAfterIf) {
                    ReuseToken(afterIfBody);
                }

                return new DMASTProcStatementIf(loc, condition, body, elseBody);
            } else {
                return null;
            }
        }

        private DMASTProcStatement? For() {
            var loc = Current().Location;

            if (Check(TokenType.DM_For)) {
                Whitespace();
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                Whitespace();

                if (Check(TokenType.DM_RightParenthesis)) {
                    return new DMASTProcStatementInfLoop(loc, GetForBody(loc));
                }

                _allowVarDeclExpression = true;
                DMASTExpression? expr1 = Expression();
                DMComplexValueType? dmTypes = AsComplexTypes();
                Whitespace();
                _allowVarDeclExpression = false;
                if (expr1 == null) {
                    if (!ForSeparatorTypes.Contains(Current().Type)) {
                        Emit(WarningCode.BadExpression, "Expected 1st expression in for");
                    }

                    expr1 = new DMASTConstantNull(loc);
                }

                if (Check(TokenType.DM_To)) {
                    if (expr1 is DMASTAssign assign) {
                        ExpressionTo(out var endRange, out var step);
                        Consume(TokenType.DM_RightParenthesis, "Expected ')' in for after to expression");
                        return new DMASTProcStatementFor(loc, new DMASTExpressionInRange(loc, assign.LHS, assign.RHS, endRange, step), null, null, dmTypes, GetForBody(loc));
                    } else {
                        Emit(WarningCode.BadExpression, "Expected = before to in for");
                        return new DMASTInvalidProcStatement(loc);
                    }
                }

                if (Check(TokenType.DM_In)) {
                    Whitespace();
                    DMASTExpression? listExpr = Expression();
                    Whitespace();
                    Consume(TokenType.DM_RightParenthesis, "Expected ')' in for after expression 2");
                    return new DMASTProcStatementFor(loc, new DMASTExpressionIn(loc, expr1, listExpr), null, null, dmTypes, GetForBody(loc));
                }

                if (!Check(ForSeparatorTypes)) {
                    Consume(TokenType.DM_RightParenthesis, "Expected ')' in for after expression 1");
                    return new DMASTProcStatementFor(loc, expr1, null, null, dmTypes, GetForBody(loc));
                }

                if (Check(TokenType.DM_RightParenthesis)) {
                    return new DMASTProcStatementFor(loc, expr1, null, null, dmTypes, GetForBody(loc));
                }

                Whitespace();
                DMASTExpression? expr2 = Expression();
                if (expr2 == null) {
                    if (!ForSeparatorTypes.Contains(Current().Type)) {
                        Emit(WarningCode.BadExpression, "Expected 2nd expression in for");
                    }

                    expr2 = new DMASTConstantInteger(loc, 1);
                }

                if (!Check(ForSeparatorTypes)) {
                    Consume(TokenType.DM_RightParenthesis, "Expected ')' in for after expression 2");
                    return new DMASTProcStatementFor(loc, expr1, expr2, null, dmTypes, GetForBody(loc));
                }

                if (Check(TokenType.DM_RightParenthesis)) {
                    return new DMASTProcStatementFor(loc, expr1, expr2, null, dmTypes, GetForBody(loc));
                }

                Whitespace();
                DMASTExpression? expr3 = Expression();
                if (expr3 == null) {
                    if (Current().Type != TokenType.DM_RightParenthesis) {
                        Emit(WarningCode.BadExpression, "Expected 3nd expression in for");
                    }

                    expr3 = new DMASTConstantNull(loc);
                }

                Consume(TokenType.DM_RightParenthesis, "Expected ')' in for after expression 3");
                return new DMASTProcStatementFor(loc, expr1, expr2, expr3, dmTypes, GetForBody(loc));
            }

            return null;

            DMASTProcBlockInner GetForBody(Location forLocation) {
                Whitespace();
                Newline();

                DMASTProcBlockInner? body = ProcBlock();
                if (body == null) {
                    var loc = Current().Location;
                    DMASTProcStatement? statement;
                    if (Check(TokenType.DM_Semicolon)) {
                        statement = new DMASTProcStatementExpression(loc, new DMASTConstantNull(loc));
                    } else {
                        statement = ProcStatement();
                        if (statement == null) {
                            DMCompiler.Emit(WarningCode.BadExpression, forLocation, "Expected body or statement");
                            statement = new DMASTProcStatementExpression(loc, new DMASTConstantNull(loc)); // just so we can continue compiling.
                        }
                    }
                    body = new DMASTProcBlockInner(loc, statement);
                }

                return body;
            }
        }

        private DMASTProcStatement? While() {
            var loc = Current().Location;

            if (Check(TokenType.DM_While)) {
                Whitespace();
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                Whitespace();
                DMASTExpression? conditional = Expression();
                RequireExpression(ref conditional, "Expected a condition");
                ConsumeRightParenthesis();
                Check(TokenType.DM_Semicolon);
                Whitespace();
                DMASTProcBlockInner? body = ProcBlock();

                if (body == null) {
                    DMASTProcStatement? statement = ProcStatement();

                    //Loops without a body are valid DM
                    statement ??= new DMASTProcStatementContinue(loc);

                    body = new DMASTProcBlockInner(loc, statement);
                }

                if (conditional is DMASTConstantInteger integer && integer.Value != 0) {
                    return new DMASTProcStatementInfLoop(loc, body);
                }

                return new DMASTProcStatementWhile(loc, conditional, body);
            }

            return null;
        }

        private DMASTProcStatementDoWhile? DoWhile() {
            var loc = Current().Location;

            if (Check(TokenType.DM_Do)) {
                Whitespace();
                DMASTProcBlockInner? body = ProcBlock();

                if (body == null) {
                    DMASTProcStatement? statement = ProcStatement();
                    if (statement is null) { // This is consistently fatal in BYOND
                        Emit(WarningCode.MissingBody, "Expected statement - do-while requires a non-empty block");
                        //For the sake of argument, add a statement (avoids repetitive warning emissions down the line :^) )
                        statement = new DMASTInvalidProcStatement(loc);
                    }
                    body = new DMASTProcBlockInner(loc, new[] { statement }, null);
                }

                Newline();
                Whitespace();
                Consume(TokenType.DM_While, "Expected 'while'");
                Whitespace();
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                Whitespace();
                DMASTExpression? conditional = Expression();
                RequireExpression(ref conditional, "Expected a condition");
                ConsumeRightParenthesis();
                Whitespace();

                return new DMASTProcStatementDoWhile(loc, conditional, body);
            }

            return null;
        }

        private DMASTProcStatementSwitch? Switch() {
            var loc = Current().Location;

            if (Check(TokenType.DM_Switch)) {
                Whitespace();
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                Whitespace();
                DMASTExpression? value = Expression();
                ConsumeRightParenthesis();
                Whitespace();

                DMASTProcStatementSwitch.SwitchCase[]? switchCases = SwitchCases();

                if (switchCases == null) Emit(WarningCode.BadExpression, "Expected switch cases");
                return new DMASTProcStatementSwitch(loc, value, switchCases);
            }

            return null;
        }

        private DMASTProcStatementSwitch.SwitchCase[]? SwitchCases() {
            Token beforeSwitchBlock = Current();
            bool hasNewline = Newline();

            DMASTProcStatementSwitch.SwitchCase[]? switchCases = BracedSwitchInner() ?? IndentedSwitchInner();

            if (switchCases == null && hasNewline) {
                ReuseToken(beforeSwitchBlock);
            }

            return switchCases;
        }

        private DMASTProcStatementSwitch.SwitchCase[]? BracedSwitchInner() {
            if (Check(TokenType.DM_LeftCurlyBracket)) {
                Whitespace();
                Newline();
                bool isIndented = Check(TokenType.DM_Indent);
                DMASTProcStatementSwitch.SwitchCase[] switchInner = SwitchInner();
                if (isIndented) Check(TokenType.DM_Dedent);
                Newline();
                Consume(TokenType.DM_RightCurlyBracket, "Expected '}'");

                return switchInner;
            }

            return null;
        }

        private DMASTProcStatementSwitch.SwitchCase[]? IndentedSwitchInner() {
            if (Check(TokenType.DM_Indent)) {
                DMASTProcStatementSwitch.SwitchCase[] switchInner = SwitchInner();
                Consume(TokenType.DM_Dedent, "Expected \"if\" or \"else\"");

                return switchInner;
            }

            return null;
        }

        private DMASTProcStatementSwitch.SwitchCase[] SwitchInner() {
            List<DMASTProcStatementSwitch.SwitchCase> switchCases = new();
            DMASTProcStatementSwitch.SwitchCase? switchCase = SwitchCase();

            while (switchCase is not null) {
                switchCases.Add(switchCase);
                Newline();
                Whitespace();
                switchCase = SwitchCase();
            }

            return switchCases.ToArray();
        }

        private DMASTProcStatementSwitch.SwitchCase? SwitchCase() {
            if (Check(TokenType.DM_If)) {
                List<DMASTExpression> expressions = new();

                Whitespace();
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");

                do {
                    BracketWhitespace();

                    DMASTExpression? expression = Expression();
                    if (expression == null) {
                        if (expressions.Count == 0)
                            DMCompiler.Emit(WarningCode.BadExpression, Current().Location, "Expected an expression");

                        break;
                    }

                    if (Check(TokenType.DM_To)) {
                        Whitespace();
                        var loc = Current().Location;
                        DMASTExpression? rangeEnd = Expression();
                        if (rangeEnd == null) {
                            DMCompiler.Emit(WarningCode.BadExpression, loc, "Expected an upper limit");
                            rangeEnd = new DMASTConstantNull(loc); // Fallback to null
                        }

                        expressions.Add(new DMASTSwitchCaseRange(loc, expression, rangeEnd));
                    } else {
                        expressions.Add(expression);
                    }

                    Delimiter();
                } while (Check(TokenType.DM_Comma));
                Whitespace();
                ConsumeRightParenthesis();
                Whitespace();
                DMASTProcBlockInner? body = ProcBlock();

                if (body == null) {
                    DMASTProcStatement? statement = ProcStatement();

                    body = (statement != null)
                        ? new DMASTProcBlockInner(statement.Location, statement)
                        : new DMASTProcBlockInner(CurrentLoc);
                }

                return new DMASTProcStatementSwitch.SwitchCaseValues(expressions.ToArray(), body);
            } else if (Check(TokenType.DM_Else)) {
                Whitespace();
                var loc = Current().Location;
                if (Current().Type == TokenType.DM_If) {
                    //From now on, all if/elseif/else are actually part of this if's chain, not the switch's.
                    //Ambiguous, but that is parity behaviour. Ergo, the following emission.
                    DMCompiler.Emit(WarningCode.SuspiciousSwitchCase, loc,
                        "Expected \"if\" or \"else\" - \"else if\" is ambiguous as a switch case and may cause unintended flow");
                }

                DMASTProcBlockInner? body = ProcBlock();

                if (body == null) {
                    DMASTProcStatement? statement = ProcStatement();

                    body = (statement != null)
                        ? new DMASTProcBlockInner(loc, statement)
                        : new DMASTProcBlockInner(loc);
                }

                return new DMASTProcStatementSwitch.SwitchCaseDefault(body);
            }

            return null;
        }

        private DMASTProcStatementTryCatch? TryCatch() {
            var loc = Current().Location;

            if (Check(TokenType.DM_Try)) {
                Whitespace();

                DMASTProcBlockInner? tryBody = ProcBlock();
                if (tryBody == null) {
                    DMASTProcStatement? statement = ProcStatement();

                    if (statement == null) Emit(WarningCode.BadExpression, "Expected body or statement");
                    tryBody = new DMASTProcBlockInner(loc,statement);
                }

                Newline();
                Whitespace();
                Consume(TokenType.DM_Catch, "Expected catch");
                Whitespace();

                // catch(var/exception/E)
                DMASTProcStatement? parameter = null;
                if (Check(TokenType.DM_LeftParenthesis)) {
                    BracketWhitespace();
                    parameter = ProcVarDeclaration(allowMultiple: false);
                    BracketWhitespace();
                    ConsumeRightParenthesis();
                    Whitespace();
                }

                DMASTProcBlockInner? catchBody = ProcBlock();
                if (catchBody == null) {
                    DMASTProcStatement? statement = ProcStatement();

                    if (statement != null) catchBody = new DMASTProcBlockInner(loc, statement);
                }

                return new DMASTProcStatementTryCatch(loc, tryBody, catchBody, parameter);
            }

            return null;
        }

        private DMASTProcStatementThrow? Throw() {
            var loc = Current().Location;

            if (Check(TokenType.DM_Throw)) {
                Whitespace();
                DMASTExpression? value = Expression();

                return new DMASTProcStatementThrow(loc, value);
            } else {
                return null;
            }
        }

        private DMASTProcStatementLabel Label(DMASTIdentifier expression) {
            Whitespace();
            Newline();

            DMASTProcBlockInner? body = ProcBlock();

            return new DMASTProcStatementLabel(expression.Location, expression.Identifier, body);
        }

        private DMASTCallParameter[]? ProcCall() {
            if (Check(TokenType.DM_LeftParenthesis)) {
                BracketWhitespace();

                DMASTCallParameter[] callParameters = CallParameters() ?? Array.Empty<DMASTCallParameter>();
                BracketWhitespace();
                ConsumeRightParenthesis();

                return callParameters;
            }

            return null;
        }

        private DMASTPick.PickValue[]? PickArguments() {
            if (Check(TokenType.DM_LeftParenthesis)) {
                BracketWhitespace();

                DMASTPick.PickValue? arg = PickArgument();
                if (arg == null) {
                    Emit(WarningCode.MissingExpression, "Expected a pick argument");
                    arg = new(null, new DMASTInvalidExpression(CurrentLoc));
                }

                List<DMASTPick.PickValue> args = [arg.Value];

                while (Check(TokenType.DM_Comma)) {
                    BracketWhitespace();
                    arg = PickArgument();

                    if (arg != null) {
                        args.Add(arg.Value);
                    } else {
                        //A comma at the end is allowed, but the call must immediately be closed
                        if (Current().Type != TokenType.DM_RightParenthesis) {
                            Emit(WarningCode.MissingExpression, "Expected a pick argument");
                            break;
                        }
                    }
                }

                BracketWhitespace();
                ConsumeRightParenthesis();
                return args.ToArray();
            }

            return null;
        }

        private DMASTPick.PickValue? PickArgument() {
            DMASTExpression? expression = Expression();

            if (Check(TokenType.DM_Semicolon)) {
                Whitespace();
                DMASTExpression? value = Expression();
                RequireExpression(ref value);

                return new DMASTPick.PickValue(expression, value);
            } else if (expression != null) {
                return new DMASTPick.PickValue(null, expression);
            }

            return null;
        }

        private DMASTCallParameter[]? CallParameters() {
            List<DMASTCallParameter> parameters = new();
            DMASTCallParameter? parameter = CallParameter();
            BracketWhitespace();

            while (Check(TokenType.DM_Comma)) {
                BracketWhitespace();
                var loc = Current().Location;
                parameters.Add(parameter ?? new DMASTCallParameter(loc, new DMASTConstantNull(loc)));
                parameter = CallParameter();
                BracketWhitespace();
            }

            if (parameter != null) {
                parameters.Add(parameter);
            }

            if (parameters.Count > 0) {
                return parameters.ToArray();
            } else {
                return null;
            }
        }

        private DMASTCallParameter? CallParameter() {
            DMASTExpression? expression = Expression();
            if (expression == null)
                return null;

            if (expression is DMASTAssign assign) {
                DMASTExpression key = assign.LHS;
                if (key is DMASTIdentifier identifier) {
                    key = new DMASTConstantString(key.Location, identifier.Identifier);
                } else if (key is DMASTConstantNull) {
                    key = new DMASTConstantString(key.Location, "null");
                }

                return new DMASTCallParameter(assign.Location, assign.RHS, key);
            } else {
                return new DMASTCallParameter(expression.Location, expression);
            }
        }

        private List<DMASTDefinitionParameter> DefinitionParameters(out bool wasIndeterminate) {
            List<DMASTDefinitionParameter> parameters = new();
            DMASTDefinitionParameter? parameter = DefinitionParameter(out wasIndeterminate);

            if (parameter != null) parameters.Add(parameter);

            BracketWhitespace();

            while (Check(TokenType.DM_Comma)) {
                BracketWhitespace();
                parameter = DefinitionParameter(out wasIndeterminate);

                if (parameter != null) {
                    parameters.Add(parameter);
                    BracketWhitespace();
                }

                if (Check(TokenType.DM_Null)) {
                    // Breaking change - BYOND creates a var named null that overrides the keyword. No error.
                    if (Emit(WarningCode.SoftReservedKeyword, "'null' is not a valid variable name")) { // If it's an error, skip over this var instantiation.
                        Advance();
                        BracketWhitespace();
                        Check(TokenType.DM_Comma);
                        BracketWhitespace();
                        parameters.AddRange(DefinitionParameters(out wasIndeterminate));
                    }
                }
            }

            return parameters;
        }

        private DMASTDefinitionParameter? DefinitionParameter(out bool wasIndeterminate) {
            DMASTPath? path = Path();

            if (path != null) {
                var loc = Current().Location;
                Whitespace();

                DMASTExpression? value = PathArray(ref path.Path);
                DMASTExpression? possibleValues = null;

                if (Check(TokenType.DM_DoubleSquareBracketEquals)) {
                    Whitespace();
                    value = Expression();
                }

                if (Check(TokenType.DM_Equals)) {
                    Whitespace();
                    value = Expression();
                }

                var type = AsComplexTypes();
                var dmType = DMObjectTree.GetDMObject(path.Path, false);
                if (type is {Type: not DMValueType.Anything } && (value is null or DMASTConstantNull) && (dmType?.IsSubtypeOf(DreamPath.Datum) ?? false)) {
                    DMCompiler.Emit(WarningCode.ImplicitNullType, loc, $"Variable \"{path.Path}\" is null but not a subtype of atom nor explicitly typed as nullable, append \"|null\" to \"as\". It will implicitly be treated as nullable.");
                    type |= DMValueType.Null;
                }

                Whitespace();

                if (Check(TokenType.DM_In)) {
                    Whitespace();
                    possibleValues = Expression();
                }

                wasIndeterminate = false;

                return new DMASTDefinitionParameter(loc, path, value, type, possibleValues);
            }

            wasIndeterminate = Check(TokenType.DM_IndeterminateArgs);

            return null;
        }

        private DMASTExpression? Expression() {
            return ExpressionIn();
        }

        private void ExpressionTo(out DMASTExpression endRange, out DMASTExpression? step) {
            Whitespace();
            var endRangeExpr = ExpressionAssign();
            RequireExpression(ref endRangeExpr, "Missing end range");
            Whitespace();

            endRange = endRangeExpr;
            if (Check(TokenType.DM_Step)) {
                Whitespace();
                step = ExpressionAssign();
                RequireExpression(ref step, "Missing step value");
                Whitespace();
            } else {
                step = null;
            }
        }

        private DMASTExpression? ExpressionIn() {
            DMASTExpression? value = ExpressionAssign();

            if (value != null && Check(TokenType.DM_In)) {
                var loc = Current().Location;
                Whitespace();
                DMASTExpression? list = ExpressionAssign();
                RequireExpression(ref list, "Expected a container to search in");
                Whitespace();

                if (Check(TokenType.DM_To)) {
                    ExpressionTo(out var endRange, out var step);
                    return new DMASTExpressionInRange(loc, value, list, endRange, step);
                }

                return new DMASTExpressionIn(loc, value, list);
            }

            return value;
        }

        private DMASTExpression? ExpressionAssign() {
            DMASTExpression? expression = ExpressionTernary();

            if (expression != null) {
                Token token = Current();
                if (Check(AssignTypes)) {
                    Whitespace();
                    DMASTExpression? value = ExpressionAssign();
                    RequireExpression(ref value, "Expected a value");

                    switch (token.Type) {
                        case TokenType.DM_Equals: return new DMASTAssign(token.Location, expression, value);
                        case TokenType.DM_PlusEquals: return new DMASTAppend(token.Location, expression, value);
                        case TokenType.DM_MinusEquals: return new DMASTRemove(token.Location, expression, value);
                        case TokenType.DM_BarEquals: return new DMASTCombine(token.Location, expression, value);
                        case TokenType.DM_BarBarEquals: return new DMASTLogicalOrAssign(token.Location, expression, value);
                        case TokenType.DM_AndEquals: return new DMASTMask(token.Location, expression, value);
                        case TokenType.DM_AndAndEquals: return new DMASTLogicalAndAssign(token.Location, expression, value);
                        case TokenType.DM_StarEquals: return new DMASTMultiplyAssign(token.Location, expression, value);
                        case TokenType.DM_SlashEquals: return new DMASTDivideAssign(token.Location, expression, value);
                        case TokenType.DM_LeftShiftEquals: return new DMASTLeftShiftAssign(token.Location, expression, value);
                        case TokenType.DM_RightShiftEquals: return new DMASTRightShiftAssign(token.Location, expression, value);
                        case TokenType.DM_XorEquals: return new DMASTXorAssign(token.Location, expression, value);
                        case TokenType.DM_ModulusEquals: return new DMASTModulusAssign(token.Location, expression, value);
                        case TokenType.DM_ModulusModulusEquals: return new DMASTModulusModulusAssign(token.Location, expression, value);
                        case TokenType.DM_AssignInto: return new DMASTAssignInto(token.Location, expression, value);
                    }
                }
            }

            return expression;
        }

        private DMASTExpression? ExpressionTernary(bool isTernaryB = false) {
            DMASTExpression? a = ExpressionOr(isTernaryB);

            if (a != null && Check(TokenType.DM_Question)) {
                Whitespace();
                DMASTExpression? b = ExpressionTernary(isTernaryB: true);
                RequireExpression(ref b);

                if (b is DMASTVoid) b = new DMASTConstantNull(b.Location);

                Consume(TokenType.DM_Colon, "Expected ':'");
                Whitespace();

                DMASTExpression? c = ExpressionTernary(isTernaryB);
                if (c is DMASTVoid) c = new DMASTConstantNull(c.Location);

                return new DMASTTernary(a.Location, a, b, c);
            }

            return a;
        }

        private DMASTExpression? ExpressionOr(bool isTernaryB = false) {
            DMASTExpression? a = ExpressionAnd(isTernaryB);
            if (a != null) {
                var loc = Current().Location;

                while (Check(TokenType.DM_BarBar)) {
                    Whitespace();
                    DMASTExpression? b = ExpressionAnd(isTernaryB);
                    RequireExpression(ref b, "Expected a second value");

                    a = new DMASTOr(loc, a, b);
                }
            }

            return a;
        }

        private DMASTExpression? ExpressionAnd(bool isTernaryB = false) {
            DMASTExpression? a = ExpressionBinaryOr(isTernaryB);

            if (a != null) {
                var loc = Current().Location;

                while (Check(TokenType.DM_AndAnd)) {
                    Whitespace();
                    DMASTExpression? b = ExpressionBinaryOr(isTernaryB);
                    RequireExpression(ref b, "Expected a second value");

                    a = new DMASTAnd(loc, a, b);
                }
            }

            return a;
        }

        private DMASTExpression? ExpressionBinaryOr(bool isTernaryB = false) {
            DMASTExpression? a = ExpressionBinaryXor(isTernaryB);
            if (a != null) {
                var loc = Current().Location;

                while (Check(TokenType.DM_Bar)) {
                    Whitespace();
                    DMASTExpression? b = ExpressionBinaryXor(isTernaryB);
                    RequireExpression(ref b);

                    a = new DMASTBinaryOr(loc, a, b);
                }
            }

            return a;
        }

        private DMASTExpression? ExpressionBinaryXor(bool isTernaryB = false) {
            DMASTExpression? a = ExpressionBinaryAnd(isTernaryB);
            if (a != null) {
                var loc = Current().Location;

                while (Check(TokenType.DM_Xor)) {
                    Whitespace();
                    DMASTExpression? b = ExpressionBinaryAnd(isTernaryB);
                    RequireExpression(ref b);

                    a = new DMASTBinaryXor(loc, a, b);
                }
            }

            return a;
        }

        private DMASTExpression? ExpressionBinaryAnd(bool isTernaryB = false) {
            DMASTExpression? a = ExpressionComparison(isTernaryB);
            if (a != null) {
                var loc = Current().Location;

                while (Check(TokenType.DM_And)) {
                    Whitespace();
                    DMASTExpression? b = ExpressionComparison(isTernaryB);
                    RequireExpression(ref b);

                    a = new DMASTBinaryAnd(loc, a, b);
                }
            }

            return a;
        }

        private DMASTExpression? ExpressionComparison(bool isTernaryB = false) {
            DMASTExpression? a = ExpressionBitShift(isTernaryB);

            if (a != null) {
                Token token = Current();

                while (Check(ComparisonTypes)) {
                    Whitespace();
                    DMASTExpression? b = ExpressionBitShift(isTernaryB);
                    RequireExpression(ref b, "Expected an expression to compare to");

                    switch (token.Type) {
                        case TokenType.DM_EqualsEquals: a = new DMASTEqual(token.Location, a, b); break;
                        case TokenType.DM_ExclamationEquals: a = new DMASTNotEqual(token.Location, a, b); break;
                        case TokenType.DM_TildeEquals: a = new DMASTEquivalent(token.Location, a, b); break;
                        case TokenType.DM_TildeExclamation: a = new DMASTNotEquivalent(token.Location, a, b); break;
                    }

                    token = Current();
                }
            }

            return a;
        }

        private DMASTExpression? ExpressionBitShift(bool isTernaryB = false) {
            DMASTExpression? a = ExpressionComparisonLtGt(isTernaryB);

            if (a != null) {
                Token token = Current();

                while (Check(ShiftTypes)) {
                    Whitespace();
                    DMASTExpression? b = ExpressionComparisonLtGt(isTernaryB);
                    RequireExpression(ref b);

                    switch (token.Type) {
                        case TokenType.DM_LeftShift: a = new DMASTLeftShift(token.Location, a, b); break;
                        case TokenType.DM_RightShift: a = new DMASTRightShift(token.Location, a, b); break;
                    }

                    token = Current();
                }
            }

            return a;
        }

        private DMASTExpression? ExpressionComparisonLtGt(bool isTernaryB = false) {
            DMASTExpression? a = ExpressionAdditionSubtraction(isTernaryB);

            if (a != null) {
                Token token = Current();

                while (Check(LtGtComparisonTypes)) {
                    Whitespace();
                    DMASTExpression? b = ExpressionAdditionSubtraction(isTernaryB);
                    RequireExpression(ref b);

                    switch (token.Type) {
                        case TokenType.DM_LessThan: a = new DMASTLessThan(token.Location, a, b); break;
                        case TokenType.DM_LessThanEquals: a = new DMASTLessThanOrEqual(token.Location, a, b); break;
                        case TokenType.DM_GreaterThan: a = new DMASTGreaterThan(token.Location, a, b); break;
                        case TokenType.DM_GreaterThanEquals: a = new DMASTGreaterThanOrEqual(token.Location, a, b); break;
                    }

                    token = Current();
                }
            }

            return a;
        }

        private DMASTExpression? ExpressionAdditionSubtraction(bool isTernaryB = false) {
            DMASTExpression? a = ExpressionMultiplicationDivisionModulus(isTernaryB);

            if (a != null) {
                Token token = Current();

                while (Check(PlusMinusTypes)) {
                    Whitespace();
                    DMASTExpression? b = ExpressionMultiplicationDivisionModulus(isTernaryB);
                    RequireExpression(ref b);

                    switch (token.Type) {
                        case TokenType.DM_Plus: a = new DMASTAdd(token.Location, a, b); break;
                        case TokenType.DM_Minus: a = new DMASTSubtract(token.Location, a, b); break;
                    }

                    token = Current();
                }
            }

            return a;
        }

        private DMASTExpression? ExpressionMultiplicationDivisionModulus(bool isTernaryB = false) {
            DMASTExpression? a = ExpressionPower(isTernaryB);

            if (a != null) {
                Token token = Current();

                while (Check(MulDivModTypes)) {
                    Whitespace();
                    DMASTExpression? b = ExpressionPower(isTernaryB);
                    RequireExpression(ref b);

                    switch (token.Type) {
                        case TokenType.DM_Star: a = new DMASTMultiply(token.Location, a, b); break;
                        case TokenType.DM_Slash: a = new DMASTDivide(token.Location, a, b); break;
                        case TokenType.DM_Modulus: a = new DMASTModulus(token.Location, a, b); break;
                        case TokenType.DM_ModulusModulus: a = new DMASTModulusModulus(token.Location, a, b); break;
                    }

                    token = Current();
                }
            }

            return a;
        }

        private DMASTExpression? ExpressionPower(bool isTernaryB = false) {
            DMASTExpression? a = ExpressionUnary(isTernaryB);

            if (a != null) {
                var loc = Current().Location;

                while (Check(TokenType.DM_StarStar)) {
                    Whitespace();
                    DMASTExpression? b = ExpressionPower(isTernaryB);
                    RequireExpression(ref b);

                    a = new DMASTPower(loc, a, b);
                }
            }

            return a;
        }

        private DMASTExpression? ExpressionUnary(bool isTernaryB = false) {
            var loc = CurrentLoc;

            if (Check(stackalloc[] {
                    TokenType.DM_Exclamation,
                    TokenType.DM_Tilde,
                    TokenType.DM_PlusPlus,
                    TokenType.DM_MinusMinus,
                    TokenType.DM_And,
                    TokenType.DM_Star
                }, out var unaryToken)) {
                Whitespace();
                DMASTExpression? expression = ExpressionUnary(isTernaryB);
                RequireExpression(ref expression);

                switch (unaryToken.Type) {
                    case TokenType.DM_Exclamation: return new DMASTNot(loc, expression);
                    case TokenType.DM_Tilde: return new DMASTBinaryNot(loc, expression);
                    case TokenType.DM_PlusPlus: return new DMASTPreIncrement(loc, expression);
                    case TokenType.DM_MinusMinus: return new DMASTPreDecrement(loc, expression);
                    case TokenType.DM_And: return new DMASTPointerRef(loc, expression);
                    case TokenType.DM_Star: return new DMASTPointerDeref(loc, expression);
                }

                Emit(WarningCode.BadToken, loc, $"Problem while handling unary '{unaryToken.PrintableText}'");
                return new DMASTInvalidExpression(loc);
            } else {
                DMASTExpression? expression = ExpressionSign(isTernaryB);

                if (expression != null) {
                    if (Check(TokenType.DM_PlusPlus)) {
                        Whitespace();
                        expression = new DMASTPostIncrement(loc, expression);
                    } else if (Check(TokenType.DM_MinusMinus)) {
                        Whitespace();
                        expression = new DMASTPostDecrement(loc, expression);
                    }
                }

                return expression;
            }
        }

        private DMASTExpression? ExpressionSign(bool isTernaryB = false) {
            Token token = Current();

            if (Check(PlusMinusTypes)) {
                Whitespace();
                DMASTExpression? expression = ExpressionSign();
                RequireExpression(ref expression);

                if (token.Type == TokenType.DM_Minus) {
                    switch (expression) {
                        case DMASTConstantInteger integer:
                            return new DMASTConstantInteger(token.Location, -integer.Value);
                        case DMASTConstantFloat constantFloat:
                            return new DMASTConstantFloat(token.Location, -constantFloat.Value);
                        default:
                            return new DMASTNegate(token.Location, expression);
                    }
                } else {
                    return expression;
                }
            }

            return ExpressionNew(isTernaryB);
        }

        private DMASTExpression? ExpressionNew(bool isTernaryB = false) {
            var loc = Current().Location;

            if (Check(TokenType.DM_New)) {
                Whitespace();
                DMASTExpression? type = ExpressionPrimary(allowParentheses: false);
                type = ParseDereference(type, allowCalls: false);
                DMASTCallParameter[]? parameters = ProcCall();

                DMASTExpression? newExpression = type switch {
                    DMASTConstantPath path => new DMASTNewPath(loc, path, parameters),
                    not null => new DMASTNewExpr(loc, type, parameters),
                    null => new DMASTNewInferred(loc, parameters),
                };

                newExpression = ParseDereference(newExpression);
                return newExpression;
            }

            return ParseDereference(ExpressionPrimary(), true, isTernaryB);
        }

        private DMASTExpression? ExpressionPrimary(bool allowParentheses = true) {
            var token = Current();
            var loc = token.Location;

            if (allowParentheses && Check(TokenType.DM_LeftParenthesis)) {
                BracketWhitespace();
                DMASTExpression? inner = Expression();
                BracketWhitespace();
                ConsumeRightParenthesis();

                if (inner is null) {
                    inner = new DMASTVoid(loc);
                } else {
                    inner = new DMASTExpressionWrapped(inner.Location, inner);
                }

                return inner;
            }

            if (token.Type == TokenType.DM_Var && _allowVarDeclExpression)
                return new DMASTVarDeclExpression( loc, Path() );

            if (Constant() is { } constant)
                return constant;

            if (Path(true) is { } path) {
                DMASTExpressionConstant pathConstant = new DMASTConstantPath(loc, path);

                while (Check(TokenType.DM_Period)) {
                    DMASTPath? search = Path();
                    if (search == null) {
                        Emit(WarningCode.MissingExpression, "Expected a path for an upward search");
                        break;
                    }

                    pathConstant = new DMASTUpwardPathSearch(loc, pathConstant, search);
                }

                Whitespace(); // whitespace between path and modified type

                //TODO actual modified type support
                if (Check(TokenType.DM_LeftCurlyBracket)) {
                    DMCompiler.UnimplementedWarning(path.Location, "Modified types are currently not supported and modified values will be ignored.");

                    while (Current().Type != TokenType.DM_RightCurlyBracket && !Check(TokenType.EndOfFile)) Advance();
                    Consume(TokenType.DM_RightCurlyBracket, "Expected '}'");
                    //The lexer tosses in a newline after '}', but we avoid Newline() because we only want to remove the extra newline, not all of them
                    Check(TokenType.Newline);
                }

                return pathConstant;
            }

            if (Identifier() is { } identifier)
                return identifier;

            if ((DMASTExpression?)Callable() is { } callable)
                return callable;

            if (Check(TokenType.DM_DoubleColon))
                return ParseScopeIdentifier(null);

            if (Check(TokenType.DM_Call)) {
                Whitespace();
                DMASTCallParameter[]? callParameters = ProcCall();
                if (callParameters == null || callParameters.Length < 1 || callParameters.Length > 2) {
                    Emit(WarningCode.InvalidArgumentCount, "call()() must have 2 parameters");
                    return new DMASTInvalidExpression(loc);
                }

                Whitespace();
                DMASTCallParameter[]? procParameters = ProcCall();
                if (procParameters == null) {
                    Emit(WarningCode.InvalidArgumentCount, "Expected proc parameters");
                    procParameters = Array.Empty<DMASTCallParameter>();
                }

                return new DMASTCall(loc, callParameters, procParameters);
            }

            return null;
        }

        protected DMASTExpression? Constant() {
            Token constantToken = Current();

            switch (constantToken.Type) {
                case TokenType.DM_Integer: Advance(); return new DMASTConstantInteger(constantToken.Location, (int)constantToken.Value);
                case TokenType.DM_Float: Advance(); return new DMASTConstantFloat(constantToken.Location, (float)constantToken.Value);
                case TokenType.DM_Resource: Advance(); return new DMASTConstantResource(constantToken.Location, (string)constantToken.Value);
                case TokenType.DM_Null: Advance(); return new DMASTConstantNull(constantToken.Location);
                case TokenType.DM_RawString: Advance(); return new DMASTConstantString(constantToken.Location, (string)constantToken.Value);
                case TokenType.DM_ConstantString:
                case TokenType.DM_StringBegin:
                    // Don't advance, ExpressionFromString() will handle it
                    return ExpressionFromString();
                default: return null;
            }
        }

        private bool Newline() {
            bool hasNewline = Check(TokenType.Newline);

            while (Check(TokenType.Newline)) {
            }
            return hasNewline;
        }

        private void Whitespace(bool includeIndentation = false) {
            if (includeIndentation) {
                while (Check(WhitespaceTypes)) { }
            } else {
                Check(TokenType.DM_Whitespace);
            }
        }

        //Inside brackets/parentheses, whitespace can include delimiters in select areas
        private void BracketWhitespace() {
            Whitespace();
            Delimiter();
            Whitespace();
        }

        private DMASTExpression? ParseDereference(DMASTExpression? expression, bool allowCalls = true, bool isTernaryB = false) {
            // We don't compile expression-calls as dereferences, but they have very similar precedence
            if (allowCalls) {
                expression = ParseProcCall(expression);
            }

            if (expression != null) {
                List<DMASTDereference.Operation> operations = new();
                bool ternaryBHasPriority = expression is not DMASTIdentifier;

                while (true) {
                    Token token = Current();

                    // Check for a valid deref operation token
                    if (!Check(DereferenceTypes)) {
                        Whitespace();

                        token = Current();

                        if (!Check(TokenType.DM_LeftBracket)) {
                            break;
                        }
                    }

                    // Cancel this operation chain (and potentially fall back to ternary behaviour) if this looks more like part of a ternary expression than a deref
                    if (token.Type == TokenType.DM_Colon) {
                        bool invalidDereference = (expression is DMASTExpressionConstant);

                        if (!invalidDereference) {
                            Token innerToken = Current();

                            if (Check(IdentifierTypes)) {
                                ReuseToken(innerToken);
                            } else {
                                invalidDereference = true;
                            }
                        }

                        if (invalidDereference) {
                            ReuseToken(token);
                            break;
                        }
                    }

                    // `:` token should preemptively end our dereference when inside the `b` operand of a ternary
                    // but not for the first dereference if the base expression is an identifier!
                    if (isTernaryB && ternaryBHasPriority && token.Type == TokenType.DM_Colon) {
                        ReuseToken(token);
                        break;
                    }

                    DMASTDereference.Operation operation;

                    switch (token.Type) {
                        case TokenType.DM_Period:
                        case TokenType.DM_QuestionPeriod:
                        case TokenType.DM_Colon:
                        case TokenType.DM_QuestionColon: {
                            var identifier = Identifier();

                            if (identifier == null) {
                                DMCompiler.Emit(WarningCode.BadToken, token.Location, "Identifier expected");
                                return new DMASTConstantNull(token.Location);
                            }

                            operation = new DMASTDereference.FieldOperation {
                                Location = identifier.Location,
                                Safe = token.Type is TokenType.DM_QuestionPeriod or TokenType.DM_QuestionColon,
                                Identifier = identifier.Identifier,
                                NoSearch = token.Type is TokenType.DM_Colon or TokenType.DM_QuestionColon
                            };
                            break;
                        }

                        case TokenType.DM_DoubleColon: {
                            if (operations.Count != 0) {
                                expression = new DMASTDereference(expression.Location, expression, operations.ToArray());
                                operations.Clear();
                            }
                            expression = ParseScopeIdentifier(expression);
                            continue;
                        }

                        case TokenType.DM_LeftBracket:
                        case TokenType.DM_QuestionLeftBracket: {
                            ternaryBHasPriority = true;

                            Whitespace();
                            var index = Expression();
                            ConsumeRightBracket();

                            if (index == null) {
                                DMCompiler.Emit(WarningCode.BadToken, token.Location, "Expression expected");
                                return new DMASTConstantNull(token.Location);
                            }

                            operation = new DMASTDereference.IndexOperation {
                                Index = index,
                                Location = index.Location,
                                Safe = token.Type is TokenType.DM_QuestionLeftBracket
                            };
                            break;
                        }

                        default:
                            throw new InvalidOperationException("unhandled dereference token");
                    }

                    // Attempt to upgrade this operation to a call
                    if (allowCalls) {
                        Whitespace();

                        var parameters = ProcCall();

                        if (parameters != null) {
                            ternaryBHasPriority = true;

                            switch (operation) {
                                case DMASTDereference.FieldOperation fieldOperation:
                                    operation = new DMASTDereference.CallOperation {
                                        Parameters = parameters,
                                        Location = fieldOperation.Location,
                                        Safe = fieldOperation.Safe,
                                        Identifier = fieldOperation.Identifier,
                                        NoSearch = fieldOperation.NoSearch
                                    };
                                    break;

                                case DMASTDereference.IndexOperation:
                                    DMCompiler.Emit(WarningCode.BadToken, token.Location, "Attempt to call an invalid l-value");
                                    return new DMASTConstantNull(token.Location);

                                default:
                                    throw new InvalidOperationException("unhandled dereference operation kind");
                            }
                        }
                    }

                    operations.Add(operation);
                }

                if (operations.Count != 0) {
                    Whitespace();
                    return new DMASTDereference(expression.Location, expression, operations.ToArray());
                }
            }

            Whitespace();
            return expression;
        }

        private DMASTExpression? ParseProcCall(DMASTExpression? expression) {
            if (expression is IDMASTCallable callable) {
                Whitespace();

                return ProcCall() is { } parameters
                    ? new DMASTProcCall(expression.Location, callable, parameters)
                    : expression; // Not a proc call
            }

            if (expression is not DMASTIdentifier identifier)
                return expression;

            Whitespace();

            if (identifier.Identifier == "pick") {
                DMASTPick.PickValue[]? pickValues = PickArguments();

                if (pickValues != null) {
                    return new DMASTPick(identifier.Location, pickValues);
                }
            }

            DMASTCallParameter[]? callParameters = ProcCall();
            if (callParameters != null) {
                var procName = identifier.Identifier;
                var callLoc = identifier.Location;

                switch (procName) {
                    // Any number of arguments
                    case "list": return new DMASTList(callLoc, callParameters);
                    case "newlist": return new DMASTNewList(callLoc, callParameters);
                    case "addtext": return new DMASTAddText(callLoc, callParameters);
                    case "gradient": return new DMASTGradient(callLoc, callParameters);

                    // 1 argument
                    case "prob":
                    case "initial":
                    case "nameof":
                    case "issaved":
                    case "sin":
                    case "cos":
                    case "arcsin":
                    case "tan":
                    case "arccos":
                    case "abs":
                    case "sqrt":
                    case "isnull":
                    case "length": {
                        if (callParameters.Length != 1) {
                            Emit(WarningCode.InvalidArgumentCount, callLoc, $"{procName}() takes 1 argument");
                            return new DMASTInvalidExpression(callLoc);
                        }

                        var arg = callParameters[0];

                        if (arg.Key != null)
                            Emit(WarningCode.InvalidArgumentKey, arg.Key.Location,
                                $"{procName}() does not take a named argument");

                        switch (procName) {
                            case "prob": return new DMASTProb(callLoc, arg.Value);
                            case "initial": return new DMASTInitial(callLoc, arg.Value);
                            case "nameof": return new DMASTNameof(callLoc, arg.Value);
                            case "issaved": return new DMASTIsSaved(callLoc, arg.Value);
                            case "sin": return new DMASTSin(callLoc, arg.Value);
                            case "cos": return new DMASTCos(callLoc, arg.Value);
                            case "arcsin": return new DMASTArcsin(callLoc, arg.Value);
                            case "tan": return new DMASTTan(callLoc, arg.Value);
                            case "arccos": return new DMASTArccos(callLoc, arg.Value);
                            case "abs": return new DMASTAbs(callLoc, arg.Value);
                            case "sqrt": return new DMASTSqrt(callLoc, arg.Value);
                            case "isnull": return new DMASTIsNull(callLoc, arg.Value);
                            case "length": return new DMASTLength(callLoc, arg.Value);
                        }

                        Emit(WarningCode.BadExpression, callLoc, $"Problem while handling {procName}");
                        return new DMASTInvalidExpression(callLoc);
                    }

                    // 2 arguments
                    case "get_step":
                    case "get_dir": {
                        if (callParameters.Length != 2) {
                            Emit(WarningCode.InvalidArgumentCount, callLoc, $"{procName}() takes 2 arguments");
                            return new DMASTInvalidExpression(callLoc);
                        }

                        return (procName == "get_step")
                            ? new DMASTGetStep(callLoc, callParameters[0].Value, callParameters[1].Value)
                            : new DMASTGetDir(callLoc, callParameters[0].Value, callParameters[1].Value);
                    }

                    case "input": {
                        Whitespace();
                        DMValueType? types = AsTypes();
                        Whitespace();
                        DMASTExpression? list = null;

                        if (Check(TokenType.DM_In)) {
                            Whitespace();
                            list = Expression();
                        }

                        return new DMASTInput(callLoc, callParameters, types, list);
                    }
                    case "arctan": {
                        if (callParameters.Length != 1 && callParameters.Length != 2) {
                            Emit(WarningCode.InvalidArgumentCount, callLoc, "arctan() requires 1 or 2 arguments");
                            return new DMASTInvalidExpression(callLoc);
                        }

                        return callParameters.Length == 1
                            ? new DMASTArctan(callLoc, callParameters[0].Value)
                            : new DMASTArctan2(callLoc, callParameters[0].Value, callParameters[1].Value);
                    }
                    case "log": {
                        if (callParameters.Length != 1 && callParameters.Length != 2) {
                            Emit(WarningCode.InvalidArgumentCount, callLoc, "log() requires 1 or 2 arguments");
                            return new DMASTInvalidExpression(callLoc);
                        }

                        return callParameters.Length == 1
                            ? new DMASTLog(callLoc, callParameters[0].Value, null)
                            : new DMASTLog(callLoc, callParameters[1].Value, callParameters[0].Value);
                    }
                    case "istype": {
                        if (callParameters.Length != 1 && callParameters.Length != 2) {
                            Emit(WarningCode.InvalidArgumentCount, callLoc, "istype() requires 1 or 2 arguments");
                            return new DMASTInvalidExpression(callLoc);
                        }

                        return callParameters.Length == 1
                            ? new DMASTImplicitIsType(callLoc, callParameters[0].Value)
                            : new DMASTIsType(callLoc, callParameters[0].Value, callParameters[1].Value);
                    }
                    case "text": {
                        if (callParameters.Length == 0) {
                            Emit(WarningCode.InvalidArgumentCount, callLoc, "text() requires at least 1 argument");
                            return new DMASTInvalidExpression(callLoc);
                        }

                        switch (callParameters[0].Value) {
                            case DMASTConstantString constantString: {
                                if (callParameters.Length > 1)
                                    Emit(WarningCode.InvalidArgumentCount, callLoc, "text() expected 1 argument");

                                return constantString;
                            }
                            case DMASTStringFormat formatText: {
                                List<int> emptyValueIndices = new();
                                for (int i = 0; i < formatText.InterpolatedValues.Length; i++) {
                                    if (formatText.InterpolatedValues[i] == null) emptyValueIndices.Add(i);
                                }

                                if (callParameters.Length != emptyValueIndices.Count + 1) {
                                    Emit(WarningCode.InvalidArgumentCount, callLoc,
                                        "text() was given an invalid amount of arguments for the string");
                                    return new DMASTInvalidExpression(callLoc);
                                }

                                for (int i = 0; i < emptyValueIndices.Count; i++) {
                                    int emptyValueIndex = emptyValueIndices[i];

                                    formatText.InterpolatedValues[emptyValueIndex] = callParameters[i + 1].Value;
                                }

                                return formatText;
                            }
                            default:
                                Emit(WarningCode.BadArgument, callParameters[0].Location,
                                    "text() expected a string as the first argument");
                                return new DMASTInvalidExpression(callLoc);
                        }
                    }
                    case "locate": {
                        if (callParameters.Length > 3) {
                            Emit(WarningCode.InvalidArgumentCount, callLoc,
                                "locate() was given too many arguments");
                            return new DMASTInvalidExpression(callLoc);
                        }

                        if (callParameters.Length == 3) { //locate(X, Y, Z)
                            return new DMASTLocateCoordinates(callLoc, callParameters[0].Value, callParameters[1].Value, callParameters[2].Value);
                        } else {
                            Whitespace();

                            DMASTExpression? container = null;
                            if (Check(TokenType.DM_In)) {
                                Whitespace();

                                container = Expression();
                                RequireExpression(ref container, "Expected a container for locate()");
                            }

                            DMASTExpression? type = null;
                            if (callParameters.Length == 2) {
                                type = callParameters[0].Value;
                                container = callParameters[1].Value;
                            } else if (callParameters.Length == 1) {
                                type = callParameters[0].Value;
                            }

                            return new DMASTLocate(callLoc, type, container);
                        }
                    }
                    case "rgb": {
                        if (callParameters.Length is < 3 or > 5)
                            Emit(WarningCode.InvalidArgumentCount, callLoc,
                                "Expected 3 to 5 arguments for rgb()");

                        return new DMASTRgb(identifier.Location, callParameters);
                    }
                    default:
                        return new DMASTProcCall(callLoc, new DMASTCallableProcIdentifier(callLoc, identifier.Identifier), callParameters);
                }
            }

            return expression;
        }

        private DMValueType? AsTypes() {
            if (!AsTypesStart(out var parenthetical))
                return null;
            if (parenthetical && Check(TokenType.DM_RightParenthesis)) // as ()
                return DMValueType.Anything; // TODO: BYOND doesn't allow this for proc return types

            DMValueType type = DMValueType.Anything;

            do {
                Whitespace();
                type |= SingleAsType(out _);
                Whitespace();
            } while (Check(TokenType.DM_Bar));

            if (parenthetical) {
                ConsumeRightParenthesis();
            }

            return type;
        }

        /// <summary>
        /// AsTypes(), but can handle more complex types such as type paths
        /// </summary>
        private DMComplexValueType? AsComplexTypes() {
            if (!AsTypesStart(out var parenthetical))
                return null;
            if (parenthetical && Check(TokenType.DM_RightParenthesis)) // as ()
                return DMValueType.Anything; // TODO: BYOND doesn't allow this for proc return types

            DMValueType type = DMValueType.Anything;
            DreamPath? path = null;

            do {
                Whitespace();
                type |= SingleAsType(out var pathType, allowPath: true);
                Whitespace();

                if (pathType != null) {
                    if (path == null)
                        path = pathType;
                    else
                        DMCompiler.Emit(WarningCode.BadToken, CurrentLoc,
                            $"Only one type path can be used, ignoring {pathType}");
                }

            } while (Check(TokenType.DM_Bar));

            if (parenthetical) {
                ConsumeRightParenthesis();
            }

            return new(type, path);
        }

        private bool AsTypesStart(out bool parenthetical) {
            if (Check(TokenType.DM_As)) {
                Whitespace();
                parenthetical = Check(TokenType.DM_LeftParenthesis);
                return true;
            }

            parenthetical = false;
            return false;
        }

        private DMValueType SingleAsType(out DreamPath? path, bool allowPath = false) {
            Token typeToken = Current();

            if (!Check(new[] { TokenType.DM_Identifier, TokenType.DM_Null })) {
                // Proc return types
                path = Path()?.Path;
                if (allowPath) {
                    if (path is null) {
                        DMCompiler.Emit(WarningCode.BadToken, typeToken.Location, "Expected value type or path");
                    }

                    return DMValueType.Path;
                }

                DMCompiler.Emit(WarningCode.BadToken, typeToken.Location, "Expected value type");
                return 0;
            }

            path = null;
            switch (typeToken.Text) {
                case "anything": return DMValueType.Anything;
                case "null": return DMValueType.Null;
                case "text": return DMValueType.Text;
                case "obj": return DMValueType.Obj;
                case "mob": return DMValueType.Mob;
                case "turf": return DMValueType.Turf;
                case "num": return DMValueType.Num;
                case "message": return DMValueType.Message;
                case "area": return DMValueType.Area;
                case "color": return DMValueType.Color;
                case "file": return DMValueType.File;
                case "command_text": return DMValueType.CommandText;
                case "sound": return DMValueType.Sound;
                case "icon": return DMValueType.Icon;
                case "path": return DMValueType.Path;
                case "opendream_unimplemented": return DMValueType.Unimplemented;
                case "opendream_compiletimereadonly": return DMValueType.CompiletimeReadonly;
                default:
                    Emit(WarningCode.BadToken, typeToken.Location, $"Invalid value type '{typeToken.Text}'");
                    return 0;
            }
        }

        private bool Delimiter() {
            bool hasDelimiter = false;
            while (Check(TokenType.DM_Semicolon) || Newline()) {
                hasDelimiter = true;
            }

            return hasDelimiter;
        }
    }
}
