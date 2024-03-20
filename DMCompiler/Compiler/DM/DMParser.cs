using DMCompiler.Compiler.DMPreprocessor;
using System;
using System.Collections.Generic;
using System.Linq;
using DMCompiler.Compiler.DM.AST;
using DMCompiler.DM;

namespace DMCompiler.Compiler.DM {
    public partial class DMParser : Parser<Token> {
        private DreamPath _currentPath = DreamPath.Root;
        private bool _allowVarDeclExpression;

        private static readonly TokenType[] AssignTypes = {
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
        };

        /// <remarks>This (and other similar TokenType[] sets here) is public because <see cref="DMPreprocessorParser"/> needs it.</remarks>
        public static readonly TokenType[] ComparisonTypes = {
            TokenType.DM_EqualsEquals,
            TokenType.DM_ExclamationEquals,
            TokenType.DM_TildeEquals,
            TokenType.DM_TildeExclamation
        };

        public static readonly TokenType[] LtGtComparisonTypes = {
            TokenType.DM_LessThan,
            TokenType.DM_LessThanEquals,
            TokenType.DM_GreaterThan,
            TokenType.DM_GreaterThanEquals
        };

        private static readonly TokenType[] ShiftTypes = {
            TokenType.DM_LeftShift,
            TokenType.DM_RightShift
        };

        public static readonly TokenType[] PlusMinusTypes = {
            TokenType.DM_Plus,
            TokenType.DM_Minus,
        };

        public static readonly TokenType[] MulDivModTypes = {
            TokenType.DM_Star,
            TokenType.DM_Slash,
            TokenType.DM_Modulus,
            TokenType.DM_ModulusModulus

        };

        private static readonly TokenType[] DereferenceTypes = {
            TokenType.DM_Period,
            TokenType.DM_Colon,
            TokenType.DM_DoubleColon, // not a dereference, but shares the same precedence
            TokenType.DM_QuestionPeriod,
            TokenType.DM_QuestionColon,
            TokenType.DM_QuestionLeftBracket,
        };

        private static readonly TokenType[] WhitespacedDereferenceTypes = {
            TokenType.DM_LeftBracket,
        };

        private static readonly TokenType[] WhitespaceTypes = {
            TokenType.DM_Whitespace,
            TokenType.DM_Indent,
            TokenType.DM_Dedent
        };

        private static readonly TokenType[] IdentifierTypes = {TokenType.DM_Identifier, TokenType.DM_Step};

        /// <summary>
        /// Used by <see cref="PathElement"/> to determine, keywords that may actually just be identifiers of a typename within a path, in a given context.
        /// </summary>
        private static readonly TokenType[] ValidPathElementTokens = {
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
            TokenType.DM_For,
            //BYOND fails on DM_In, don't include that
        };

        private static readonly TokenType[] ForSeparatorTypes = {
            TokenType.DM_Semicolon,
            TokenType.DM_Comma
        };

        private static readonly TokenType[] OperatorOverloadTypes = {
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
        };

        public DMParser(DMLexer lexer) : base(lexer) {
        }

        public DMASTFile File() {
            var loc = Current().Location;
            List<DMASTStatement> statements = new();

            while (Current().Type != TokenType.EndOfFile) {
                try {
                    List<DMASTStatement>? blockInner = BlockInner();

                    if (blockInner != null) statements.AddRange(blockInner);
                } catch (CompileErrorException) { }

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

        public List<DMASTStatement>? BlockInner() {
            List<DMASTStatement> statements = new();

            do {
                Whitespace();

                try {
                    DMASTStatement? statement = Statement();

                    if (statement != null) {
                        Whitespace();
                        statements.Add(statement);
                    } else {
                        if (statements.Count == 0) return null;
                    }
                } catch (CompileErrorException) {
                    LocateNextStatement();
                }
            } while (Delimiter());
            Whitespace();

            return statements;
        }

        public DMASTStatement? Statement(bool requireDelimiter = true) {
            var loc = Current().Location;

            DMASTPath? path = Path();
            if (path is null)
                return null;
            DreamPath oldPath = _currentPath;
            Whitespace();
            _currentPath = _currentPath.Combine(path.Path);

            try {
                DMASTStatement? statement = null;

                //Proc definition
                if (Check(TokenType.DM_LeftParenthesis)) {
                    DMCompiler.VerbosePrint($"Parsing proc {_currentPath}()");
                    BracketWhitespace();
                    var parameters = DefinitionParameters(out var wasIndeterminate);

                    if (Current().Type != TokenType.DM_RightParenthesis && Current().Type != TokenType.DM_Comma &&
                        !wasIndeterminate) {
                        if (parameters.Count > 0) // Separate error handling mentions the missing right-paren
                            Error($"error: {parameters.Last().Name}: missing comma ',' or right-paren ')'", false);
                        parameters.AddRange(DefinitionParameters(out wasIndeterminate));
                    }

                    if (!wasIndeterminate && Current().Type != TokenType.DM_RightParenthesis && Current().Type != TokenType.EndOfFile) {
                        // BYOND doesn't specify the arg
                        Error($"error: bad argument definition '{Current().PrintableText}'", false);
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
                    // TODO: Currently we parse it but don't do anything with this information
                    AsTypes(out _, true);

                    DMASTProcBlockInner? procBlock = ProcBlock();
                    if (procBlock is null) {
                        DMASTProcStatement? procStatement = ProcStatement();

                        if (procStatement is not null) {
                            procBlock = new DMASTProcBlockInner(loc, procStatement);
                        }
                    }

                    if(path.IsOperator) {
                        DMCompiler.UnimplementedWarning(procBlock.Location, "Operator overloads are not implemented. They will be defined but never called.");

                        List<DMASTProcStatement> procStatements = procBlock.Statements.ToList();
                        Location tokenLoc = procBlock.Location;
                        //add ". = src" as the first expression in the operator
                        DMASTProcStatementExpression assignEqSrc = new DMASTProcStatementExpression(tokenLoc, new DMASTAssign(tokenLoc,new DMASTCallableSelf(tokenLoc), new DMASTIdentifier(tokenLoc, "src")));
                        procStatements.Insert(0, assignEqSrc);

                        procBlock = new DMASTProcBlockInner(loc, procStatements.ToArray(), procBlock.SetStatements);
                    }

                    statement = new DMASTProcDefinition(loc, _currentPath, parameters.ToArray(), procBlock);
                }

                //Object definition
                if (statement == null) {
                    DMASTBlockInner? block = Block();

                    if (block != null) {
                        DMCompiler.VerbosePrint($"Parsed object {_currentPath}");
                        statement = new DMASTObjectDefinition(loc, _currentPath, block);
                    }
                }

                //Var definition(s)
                if (statement == null && _currentPath.FindElement("var") != -1) {
                    DreamPath varPath = _currentPath;
                    List<DMASTObjectVarDefinition> varDefinitions = new();

                    while (true) {
                        Whitespace();

                        DMASTExpression? value = PathArray(ref varPath);

                        if (Check(TokenType.DM_Equals)) {
                            if (value != null) Warning("List doubly initialized");

                            Whitespace();
                            value = Expression();
                            if (value == null) Error("Expected an expression");
                        }

                        value ??= new DMASTConstantNull(loc);

                        var valType = AsTypes(out _) ?? DMValueType.Anything;
                        var varDef = new DMASTObjectVarDefinition(loc, varPath, value, valType);

                        varDefinitions.Add(varDef);
                        if (Check(TokenType.DM_Comma)) {
                            Whitespace();
                            DMASTPath? newVarPath = Path();
                            if (newVarPath == null) Error("Expected a var definition");
                            if (newVarPath.Path.Elements.Length > 1) Error("Invalid var name"); //TODO: This is valid DM

                            varPath = _currentPath.AddToPath("../" + newVarPath.Path.PathString);
                        } else {
                            break;
                        }
                    }

                    if (varDefinitions.Count == 1) {
                        statement = varDefinitions[0];
                    } else {
                        statement = new DMASTMultipleObjectVarDefinitions(loc, varDefinitions.ToArray());
                    }
                }

                //Var override
                if (statement == null && Check(TokenType.DM_Equals)) {
                    Whitespace();
                    DMASTExpression? value = Expression();
                    if (value == null) Error("Expected an expression");

                    statement = new DMASTObjectVarOverride(loc, _currentPath, value);
                }

                //Empty object definition
                if (statement == null) {
                    DMCompiler.VerbosePrint($"Parsed object {_currentPath}");
                    statement = new DMASTObjectDefinition(loc, _currentPath, null);
                }

                if (requireDelimiter && !PeekDelimiter() && Current().Type != TokenType.DM_Dedent && Current().Type != TokenType.DM_RightCurlyBracket && Current().Type != TokenType.EndOfFile) {
                    Error("Expected end of object statement");
                }

                return statement;
            } finally {
                _currentPath = oldPath;
            }
            //no return here because this is technically unreachable
        }

        /// <summary>
        /// Tries to read in a path. Returns null if one cannot be constructed.
        /// </summary>
        public DMASTPath? Path(bool expression = false) {
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
                List<string> pathElements = new() { pathElement };
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
                                    Error(WarningCode.SoftReservedKeyword, "Using \"operator\" as a path element is ambiguous");
                                }
                            } else if (Check(OperatorOverloadTypes)) {
                                if (operatorToken is { Type: TokenType.DM_ConstantString, Value: not "" }) {
                                    DMCompiler.Emit(WarningCode.BadToken, operatorToken.Location,
                                        "The quotes in a stringify overload must be empty");
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
        public string? PathElement() {
            Token elementToken = Current();
            if (Check(ValidPathElementTokens)) {
                return elementToken.Text;
            } else {
                return null;
            }
        }

        public DMASTDimensionalList? PathArray(ref DreamPath path) {
            if (Current().Type == TokenType.DM_LeftBracket || Current().Type == TokenType.DM_DoubleSquareBracket) {
                var loc = Current().Location;

                // Trying to use path.IsDescendantOf(DreamPath.List) here doesn't work
                if (!path.Elements[..^1].Contains("list")) {
                    var elements = path.Elements.ToList();
                    elements.Insert(elements.IndexOf("var") + 1, "list");
                    path = new DreamPath("/" + String.Join("/", elements));
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

        public IDMASTCallable? Callable() {
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

        public DMASTIdentifier? Identifier() {
            Token token = Current();
            return Check(IdentifierTypes) ? new DMASTIdentifier(token.Location, token.Text) : null;
        }

        public DMASTBlockInner? Block() {
            Token beforeBlockToken = Current();
            bool hasNewline = Newline();

            DMASTBlockInner? block = BracedBlock();
            block ??= IndentedBlock();

            if (block == null && hasNewline) {
                ReuseToken(beforeBlockToken);
            }

            return block;
        }

        public DMASTBlockInner? BracedBlock() {
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

        public DMASTBlockInner? IndentedBlock() {
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

        public DMASTProcBlockInner? ProcBlock() {
            Token beforeBlockToken = Current();
            bool hasNewline = Newline();

            DMASTProcBlockInner? procBlock = BracedProcBlock();
            procBlock ??= IndentedProcBlock();

            if (procBlock == null && hasNewline) {
                ReuseToken(beforeBlockToken);
            }

            return procBlock;
        }

        public DMASTProcBlockInner? BracedProcBlock() {
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
                            Error(WarningCode.BadToken, "Expected end of braced block");
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

        public DMASTProcBlockInner? IndentedProcBlock() {
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
                        Error("Expected end of proc statement", throwException: false);
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

        public (List<DMASTProcStatement>?, List<DMASTProcStatement>?) ProcBlockInner() {
            List<DMASTProcStatement> procStatements = new();
            List<DMASTProcStatement> setStatements = new(); // We have to store them separately because they're evaluated first

            DMASTProcStatement? statement = null;
            do {
                Whitespace();

                try {
                    statement = ProcStatement();
                    if (statement is not null) {
                        Whitespace();
                        if(statement.IsAggregateOr<DMASTProcStatementSet>())
                            setStatements.Add(statement);
                        else
                            procStatements.Add(statement);
                    }
                } catch (CompileErrorException) {
                    LocateNextStatement();

                    //LocateNextStatement() may have landed us on another indented/braced block
                    DMASTProcBlockInner? blockInner = ProcBlock();
                    if (blockInner != null) procStatements.AddRange(blockInner.Statements);
                }
            } while (Delimiter() || statement is DMASTProcStatementLabel);
            Whitespace();

            if (procStatements.Count == 0) return (null,null);
            return (procStatements, setStatements);
        }

        public DMASTProcStatement? ProcStatement() {
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
                Error("Expected a label identifier");
            }

            if (expression != null) {
                switch (expression) {
                    case DMASTIdentifier identifier:
                        // This could be a sleep without parentheses
                        if (!Check(TokenType.DM_Colon) && !leadingColon && identifier.Identifier == "sleep") {
                            var procIdentifier = new DMASTCallableProcIdentifier(expression.Location, "sleep");
                            var sleepTime = Expression();
                            if (sleepTime == null) // The argument is optional
                                sleepTime = new DMASTConstantNull(Location.Internal);

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
                                    if (procCall.Parameters.Length != 1 && procCall.Parameters.Length != 2)
                                        Error("browse() requires 1 or 2 parameters");

                                    DMASTExpression body = procCall.Parameters[0].Value;
                                    DMASTExpression options = (procCall.Parameters.Length == 2)
                                        ? procCall.Parameters[1].Value
                                        : new DMASTConstantNull(loc);
                                    return new DMASTProcStatementBrowse(loc, leftShift.LHS, body, options);
                                }
                                case "browse_rsc": {
                                    if (procCall.Parameters.Length != 1 && procCall.Parameters.Length != 2)
                                        Error("browse_rsc() requires 1 or 2 parameters");

                                    DMASTExpression file = procCall.Parameters[0].Value;
                                    DMASTExpression filepath = (procCall.Parameters.Length == 2)
                                        ? procCall.Parameters[1].Value
                                        : new DMASTConstantNull(loc);
                                    return new DMASTProcStatementBrowseResource(loc, leftShift.LHS, file, filepath);
                                }
                                case "output": {
                                    if (procCall.Parameters.Length != 2)
                                        Error("output() requires 2 parameters");

                                    DMASTExpression msg = procCall.Parameters[0].Value;
                                    DMASTExpression control = procCall.Parameters[1].Value;
                                    return new DMASTProcStatementOutputControl(loc, leftShift.LHS, msg, control);
                                }
                                case "ftp": {
                                    if (procCall.Parameters.Length is not 1 and not 2)
                                        Error("ftp() requires 1 or 2 parameters");

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

        public DMASTProcStatement? ProcVarDeclaration(bool allowMultiple = true) {
            Token firstToken = Current();
            bool wasSlash = Check(TokenType.DM_Slash);

            if (Check(TokenType.DM_Var)) {
                if (wasSlash) Error("Unsupported root variable declaration");

                Whitespace(); // We have to consume whitespace here since "var foo = 1" (for example) is valid DM code.
                DMASTProcStatementVarDeclaration[]? vars = ProcVarEnd(allowMultiple);
                if (vars == null) Error("Expected a var declaration");
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
                    if (varDecl == null) Error("Expected a var declaration");

                    varDeclarations.AddRange(varDecl);

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
                    if (varDecl == null) Error("Expected a var declaration");

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

                    if (value == null) Error("Expected an expression");
                }

                AsTypes(out _);

                varDeclarations.Add(new DMASTProcStatementVarDeclaration(loc, varPath, value));
                if (allowMultiple && Check(TokenType.DM_Comma)) {
                    Whitespace();
                    varPath = Path();
                    if (varPath == null) Error("Expected a var declaration");
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
                    Error("Expected an identifier for set declaration");
                    return setDeclarations.ToArray();
                }
                Whitespace();
                TokenType consumed = Consume(new TokenType[] { TokenType.DM_Equals, TokenType.DM_In },"Expected a 'in' or '=' for set declaration");
                bool wasInKeyword = (consumed == TokenType.DM_In);
                Whitespace();
                DMASTExpression? value = Expression();
                if (value == null) Error("Expected an expression");
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

        public DMASTProcStatementReturn? Return() {
            var loc = Current().Location;

            if (Check(TokenType.DM_Return)) {
                Whitespace();
                DMASTExpression? value = Expression();

                return new DMASTProcStatementReturn(loc, value);
            } else {
                return null;
            }
        }

        public DMASTProcStatementBreak? Break() {
            var loc = Current().Location;

            if (Check(TokenType.DM_Break)) {
                Whitespace();
                DMASTIdentifier? label = Identifier();

                return new DMASTProcStatementBreak(loc, label);
            } else {
                return null;
            }
        }

        public DMASTProcStatementContinue? Continue() {
            var loc = Current().Location;

            if (Check(TokenType.DM_Continue)) {
                Whitespace();
                DMASTIdentifier? label = Identifier();

                return new DMASTProcStatementContinue(loc, label);
            } else {
                return null;
            }
        }

        public DMASTProcStatementGoto? Goto() {
            var loc = Current().Location;

            if (Check(TokenType.DM_Goto)) {
                Whitespace();
                DMASTIdentifier? label = Identifier();

                return new DMASTProcStatementGoto(loc, label);
            } else {
                return null;
            }
        }

        public DMASTProcStatementDel? Del() {
            var loc = Current().Location;

            if (Check(TokenType.DM_Del)) {
                Whitespace();
                bool hasParenthesis = Check(TokenType.DM_LeftParenthesis);
                Whitespace();
                DMASTExpression? value = Expression();
                if (value == null) Error("Expected value to delete");
                if (hasParenthesis) ConsumeRightParenthesis();

                return new DMASTProcStatementDel(loc, value);
            } else {
                return null;
            }
        }

        /// <returns>Either a <see cref="DMASTProcStatementSet"/> or a DMASTAggregate that acts as a container for them. May be null.</returns>
        public DMASTProcStatement? Set() {
            var loc = Current().Location;

            if (Check(TokenType.DM_Set)) {
                Whitespace();

                DMASTProcStatementSet[] sets = ProcSetEnd(true);
                if (sets.Length == 0) {
                    Error("Expected set declaration");
                    return null;
                }

                if (sets.Length > 1)
                    return new DMASTAggregate<DMASTProcStatementSet>(loc, sets);
                return sets[0];
            }

            return null;
        }

        public DMASTProcStatementSpawn? Spawn() {
            var loc = Current().Location;

            if (Check(TokenType.DM_Spawn)) {
                Whitespace();
                bool hasArg = Check(TokenType.DM_LeftParenthesis);
                DMASTExpression? delay = null;

                if (hasArg) {
                    Whitespace();

                    if (!Check(TokenType.DM_RightParenthesis)) {
                        delay = Expression();
                        if (delay == null) Error("Expected an expression");

                        ConsumeRightParenthesis();
                    }

                    Whitespace();
                }

                Newline();

                DMASTProcBlockInner? body = ProcBlock();
                if (body == null) {
                    DMASTProcStatement? statement = ProcStatement();

                    if (statement == null) Error(WarningCode.BadExpression, "Expected body or statement");
                    body = new DMASTProcBlockInner(loc, statement);
                }

                return new DMASTProcStatementSpawn(loc, delay ?? new DMASTConstantInteger(loc, 0), body);
            } else {
                return null;
            }
        }

        public DMASTProcStatementIf? If() {
            var loc = Current().Location;

            if (Check(TokenType.DM_If)) {
                Whitespace();
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                BracketWhitespace();
                DMASTExpression? condition = Expression();
                if (condition == null) {
                    Error("Expected a condition");
                }
                if (condition is DMASTAssign) {
                    DMCompiler.Emit(WarningCode.AssignmentInConditional, condition.Location, "Assignment in conditional");
                }
                BracketWhitespace();
                ConsumeRightParenthesis();
                Whitespace();
                Check(TokenType.DM_Colon);
                Whitespace();

                DMASTProcStatement? procStatement = ProcStatement();
                DMASTProcBlockInner? body;
                DMASTProcBlockInner? elseBody = null;

                if (procStatement != null) {
                    body = new DMASTProcBlockInner(loc, procStatement);
                } else {
                    body = ProcBlock();
                }

                body ??= new DMASTProcBlockInner(loc);

                Token afterIfBody = Current();
                bool newLineAfterIf = Delimiter();
                if (newLineAfterIf) Whitespace();
                if (Check(TokenType.DM_Else)) {
                    Whitespace();
                    Check(TokenType.DM_Colon);
                    Whitespace();
                    procStatement = ProcStatement();

                    if (procStatement != null) {
                        elseBody = new DMASTProcBlockInner(loc, procStatement);
                    } else {
                        elseBody = ProcBlock();
                    }

                    elseBody ??= new DMASTProcBlockInner(loc);
                } else if (newLineAfterIf) {
                    ReuseToken(afterIfBody);
                }

                return new DMASTProcStatementIf(loc, condition, body, elseBody);
            } else {
                return null;
            }
        }

        public DMASTProcStatement? For() {
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
                DMValueType? dmTypes = AsTypes(out _);
                Whitespace();
                _allowVarDeclExpression = false;
                if (expr1 == null) {
                    if (ForSeparatorTypes.Contains(Current().Type)) {
                        expr1 = new DMASTConstantNull(loc);
                    } else {
                        Error("Expected 1st expression in for");
                    }
                }

                if (Check(TokenType.DM_To)) {
                    if (expr1 is DMASTAssign assign) {
                        ExpressionTo(out var endRange, out var step);
                        Consume(TokenType.DM_RightParenthesis, "Expected ')' in for after to expression");
                        return new DMASTProcStatementFor(loc, new DMASTExpressionInRange(loc, assign.LHS, assign.RHS, endRange, step), null, null, dmTypes, GetForBody(loc));
                    } else {
                        Error("Expected = before to in for");
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
                    if (ForSeparatorTypes.Contains(Current().Type)) {
                        expr2 = new DMASTConstantInteger(loc, 1);
                    } else {
                        Error("Expected 2nd expression in for");
                    }
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
                    if (Current().Type == TokenType.DM_RightParenthesis) {
                        expr3 = new DMASTConstantNull(loc);
                    } else {
                        Error("Expected 3nd expression in for");
                    }
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

        public DMASTProcStatement? While() {
            var loc = Current().Location;

            if (Check(TokenType.DM_While)) {
                Whitespace();
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                Whitespace();
                DMASTExpression? conditional = Expression();
                if (conditional == null) Error("Expected conditional");
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

        public DMASTProcStatementDoWhile? DoWhile() {
            var loc = Current().Location;

            if (Check(TokenType.DM_Do)) {
                Whitespace();
                DMASTProcBlockInner? body = ProcBlock();

                if (body == null) {
                    DMASTProcStatement? statement = ProcStatement();
                    if (statement is null) {// This is consistently fatal in BYOND
                        Error("Expected statement - do-while requires a non-empty block");
                        //For the sake of argument, add a statement (avoids repetitive warning emissions down the line :^) )
                        statement = new DMASTProcStatementContinue(loc);
                    }
                    body = new DMASTProcBlockInner(loc, new DMASTProcStatement[] { statement }, null);
                }

                Newline();
                Whitespace();
                Consume(TokenType.DM_While, "Expected 'while'");
                Whitespace();
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                Whitespace();
                DMASTExpression? conditional = Expression();
                if (conditional == null) Error("Expected conditional");
                ConsumeRightParenthesis();
                Whitespace();

                return new DMASTProcStatementDoWhile(loc, conditional, body);
            }

            return null;
        }

        public DMASTProcStatementSwitch? Switch() {
            var loc = Current().Location;

            if (Check(TokenType.DM_Switch)) {
                Whitespace();
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                Whitespace();
                DMASTExpression? value = Expression();
                ConsumeRightParenthesis();
                Whitespace();

                DMASTProcStatementSwitch.SwitchCase[]? switchCases = SwitchCases();

                if (switchCases == null) Error(WarningCode.BadExpression, "Expected switch cases");
                return new DMASTProcStatementSwitch(loc, value, switchCases);
            }

            return null;
        }

        public DMASTProcStatementSwitch.SwitchCase[]? SwitchCases() {
            Token beforeSwitchBlock = Current();
            bool hasNewline = Newline();

            DMASTProcStatementSwitch.SwitchCase[]? switchCases = BracedSwitchInner() ?? IndentedSwitchInner();

            if (switchCases == null && hasNewline) {
                ReuseToken(beforeSwitchBlock);
            }

            return switchCases;
        }

        public DMASTProcStatementSwitch.SwitchCase[]? BracedSwitchInner() {
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

        public DMASTProcStatementSwitch.SwitchCase[]? IndentedSwitchInner() {
            if (Check(TokenType.DM_Indent)) {
                DMASTProcStatementSwitch.SwitchCase[] switchInner = SwitchInner();
                Consume(TokenType.DM_Dedent, "Expected \"if\" or \"else\"");

                return switchInner;
            }

            return null;
        }

        public DMASTProcStatementSwitch.SwitchCase[] SwitchInner() {
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

        public DMASTProcStatementSwitch.SwitchCase? SwitchCase() {
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

                    if (statement != null) {
                        body = new DMASTProcBlockInner(statement.Location, statement);
                    } else {
                        body = new DMASTProcBlockInner(Current().Location);
                    }
                }

                return new DMASTProcStatementSwitch.SwitchCaseValues(expressions.ToArray(), body);
            } else if (Check(TokenType.DM_Else)) {
                Whitespace();
                var loc = Current().Location;
                if (Current().Type == TokenType.DM_If) {
                    //From now on, all ifs/elseifs/elses are actually part of this if's chain, not the switch's.
                    //Ambiguous, but that is parity behaviour. Ergo, the following emission.
                    DMCompiler.Emit(WarningCode.SuspiciousSwitchCase, loc,
                        "Expected \"if\" or \"else\" - \"else if\" is ambiguous as a switch case and may cause unintended flow");
                }

                DMASTProcBlockInner? body = ProcBlock();

                if (body == null) {
                    DMASTProcStatement? statement = ProcStatement();

                    if (statement != null) {
                        body = new DMASTProcBlockInner(loc, statement);
                    } else {
                        body = new DMASTProcBlockInner(loc);
                    }
                }

                return new DMASTProcStatementSwitch.SwitchCaseDefault(body);
            }

            return null;
        }

        public DMASTProcStatementTryCatch? TryCatch() {
            var loc = Current().Location;

            if (Check(TokenType.DM_Try)) {
                Whitespace();

                DMASTProcBlockInner? tryBody = ProcBlock();
                if (tryBody == null) {
                    DMASTProcStatement? statement = ProcStatement();

                    if (statement == null) Error(WarningCode.BadExpression, "Expected body or statement");
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

        public DMASTProcStatementThrow? Throw() {
            var loc = Current().Location;

            if (Check(TokenType.DM_Throw)) {
                Whitespace();
                DMASTExpression? value = Expression();

                return new DMASTProcStatementThrow(loc, value);
            } else {
                return null;
            }
        }

        public DMASTProcStatementLabel Label(DMASTIdentifier expression) {
            Whitespace();
            Newline();

            DMASTProcBlockInner? body = ProcBlock();

            return new DMASTProcStatementLabel(expression.Location, expression.Identifier, body);
        }

        public DMASTCallParameter[]? ProcCall() {
            if (Check(TokenType.DM_LeftParenthesis)) {
                BracketWhitespace();

                DMASTCallParameter[] callParameters = CallParameters() ?? Array.Empty<DMASTCallParameter>();
                BracketWhitespace();
                ConsumeRightParenthesis();

                return callParameters;
            }

            return null;
        }

        public DMASTPick.PickValue[]? PickArguments() {
            if (Check(TokenType.DM_LeftParenthesis)) {
                BracketWhitespace();

                DMASTPick.PickValue? arg = PickArgument();
                if (arg == null) Error("Expected a pick argument");
                List<DMASTPick.PickValue> args = new() { arg.Value };

                while (Check(TokenType.DM_Comma)) {
                    BracketWhitespace();
                    arg = PickArgument();

                    if (arg != null) {
                        args.Add(arg.Value);
                    } else {
                        //A comma at the end is allowed, but the call must immediately be closed
                        if (Current().Type != TokenType.DM_RightParenthesis) {
                            Error("Expected a pick argument");
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

        public DMASTPick.PickValue? PickArgument() {
            DMASTExpression? expression = Expression();

            if (Check(TokenType.DM_Semicolon)) {
                Whitespace();
                DMASTExpression? value = Expression();
                if (value == null) Error("Expected an expression");

                return new DMASTPick.PickValue(expression, value);
            } else if (expression != null) {
                return new DMASTPick.PickValue(null, expression);
            }

            return null;
        }

        public DMASTCallParameter[]? CallParameters() {
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

        public DMASTCallParameter? CallParameter() {
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

        public List<DMASTDefinitionParameter> DefinitionParameters(out bool wasIndeterminate) {
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
                    if (Error(WarningCode.SoftReservedKeyword, "'null' is not a valid variable name")) { // If it's an error, skip over this var instantiation.
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

        public DMASTDefinitionParameter? DefinitionParameter(out bool wasIndeterminate) {
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

                var type = AsTypes(out _);
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

        public DMASTExpression? Expression() {
            return ExpressionIn();
        }

        public void ExpressionTo(out DMASTExpression? endRange, out DMASTExpression? step) {
            Whitespace();
            endRange = ExpressionAssign();
            Whitespace();

            if (endRange is null)
                Error("Missing end range");

            if (Check(TokenType.DM_Step)) {
                Whitespace();
                step = ExpressionAssign();
                Whitespace();
                if (step is null)
                    Error("Missing step value");
            } else {
                step = null;
            }
        }

        public DMASTExpression? ExpressionIn() {
            DMASTExpression? value = ExpressionAssign();

            if (value != null && Check(TokenType.DM_In)) {
                var loc = Current().Location;
                Whitespace();
                DMASTExpression? list = ExpressionAssign();
                Whitespace();
                if (Check(TokenType.DM_To)) {
                    ExpressionTo(out var endRange, out var step);
                    return new DMASTExpressionInRange(loc, value, list, endRange, step);
                }

                return new DMASTExpressionIn(loc, value, list);
            }

            return value;
        }

        public DMASTExpression? ExpressionAssign() {
            DMASTExpression? expression = ExpressionTernary();

            if (expression != null) {
                Token token = Current();
                if (Check(AssignTypes)) {
                    Whitespace();
                    DMASTExpression? value = ExpressionAssign();

                    if (value != null) {
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
                    } else {
                        Error("Expected a value");
                    }
                }
            }

            return expression;
        }

        public DMASTExpression? ExpressionTernary(bool isTernaryB = false) {
            DMASTExpression a = ExpressionOr(isTernaryB);

            if (a != null && Check(TokenType.DM_Question)) {
                Whitespace();
                DMASTExpression? b = ExpressionTernary(isTernaryB: true);
                if (b is DMASTVoid) b = new DMASTConstantNull(b.Location);
                if (b == null) Error("Expected an expression");

                if (!Check(TokenType.DM_Colon)) {
                    Error("Expected ':'");
                }

                Whitespace();
                DMASTExpression? c = ExpressionTernary(isTernaryB);
                if (c is DMASTVoid) c = new DMASTConstantNull(c.Location);

                return new DMASTTernary(a.Location, a, b, c);
            }

            return a;
        }

        public DMASTExpression? ExpressionOr(bool isTernaryB = false) {
            DMASTExpression? a = ExpressionAnd(isTernaryB);
            if (a != null) {
                var loc = Current().Location;

                while (Check(TokenType.DM_BarBar)) {
                    Whitespace();
                    DMASTExpression? b = ExpressionAnd(isTernaryB);
                    if (b == null) Error("Expected a second value");
                    a = new DMASTOr(loc, a, b);
                }
            }

            return a;
        }

        public DMASTExpression? ExpressionAnd(bool isTernaryB = false) {
            DMASTExpression? a = ExpressionBinaryOr(isTernaryB);

            if (a != null) {
                var loc = Current().Location;

                while (Check(TokenType.DM_AndAnd)) {
                    Whitespace();
                    DMASTExpression? b = ExpressionBinaryOr(isTernaryB);
                    if (b == null) Error("Expected a second value");
                    a = new DMASTAnd(loc, a, b);
                }
            }

            return a;
        }

        public DMASTExpression? ExpressionBinaryOr(bool isTernaryB = false) {
            DMASTExpression a = ExpressionBinaryXor(isTernaryB);
            if (a != null) {
                var loc = Current().Location;

                while (Check(TokenType.DM_Bar)) {
                    Whitespace();
                    DMASTExpression? b = ExpressionBinaryXor(isTernaryB);
                    if (b == null) Error("Expected an expression");
                    a = new DMASTBinaryOr(loc, a, b);
                }
            }

            return a;
        }

        public DMASTExpression? ExpressionBinaryXor(bool isTernaryB = false) {
            DMASTExpression? a = ExpressionBinaryAnd(isTernaryB);
            if (a != null) {
                var loc = Current().Location;

                while (Check(TokenType.DM_Xor)) {
                    Whitespace();
                    DMASTExpression? b = ExpressionBinaryAnd(isTernaryB);
                    if (b == null) Error("Expected an expression");
                    a = new DMASTBinaryXor(loc, a, b);
                }
            }

            return a;
        }

        public DMASTExpression? ExpressionBinaryAnd(bool isTernaryB = false) {
            DMASTExpression? a = ExpressionComparison(isTernaryB);
            if (a != null) {
                var loc = Current().Location;

                while (Check(TokenType.DM_And)) {
                    Whitespace();
                    DMASTExpression? b = ExpressionComparison(isTernaryB);

                    if (b == null) Error("Expected an expression");
                    a = new DMASTBinaryAnd(loc, a, b);
                }
            }

            return a;
        }

        public DMASTExpression? ExpressionComparison(bool isTernaryB = false) {
            DMASTExpression? a = ExpressionBitShift(isTernaryB);

            if (a != null) {
                Token token = Current();

                while (Check(ComparisonTypes)) {
                    Whitespace();
                    DMASTExpression? b = ExpressionBitShift(isTernaryB);
                    if (b == null) Error("Expected an expression to compare to");

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

        public DMASTExpression? ExpressionBitShift(bool isTernaryB = false) {
            DMASTExpression? a = ExpressionComparisonLtGt(isTernaryB);

            if (a != null) {
                Token token = Current();

                while (Check(ShiftTypes)) {
                    Whitespace();
                    DMASTExpression? b = ExpressionComparisonLtGt(isTernaryB);
                    if (b == null) Error("Expected an expression");

                    switch (token.Type) {
                        case TokenType.DM_LeftShift: a = new DMASTLeftShift(token.Location, a, b); break;
                        case TokenType.DM_RightShift: a = new DMASTRightShift(token.Location, a, b); break;
                    }

                    token = Current();
                }
            }

            return a;
        }

        public DMASTExpression? ExpressionComparisonLtGt(bool isTernaryB = false) {
            DMASTExpression? a = ExpressionAdditionSubtraction(isTernaryB);

            if (a != null) {
                Token token = Current();

                while (Check(LtGtComparisonTypes)) {
                    Whitespace();
                    DMASTExpression? b = ExpressionAdditionSubtraction(isTernaryB);
                    if (b == null) Error("Expected an expression");

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

        public DMASTExpression? ExpressionAdditionSubtraction(bool isTernaryB = false) {
            DMASTExpression? a = ExpressionMultiplicationDivisionModulus(isTernaryB);

            if (a != null) {
                Token token = Current();

                while (Check(PlusMinusTypes)) {
                    Whitespace();
                    DMASTExpression? b = ExpressionMultiplicationDivisionModulus(isTernaryB);
                    if (b == null) Error("Expected an expression");

                    switch (token.Type) {
                        case TokenType.DM_Plus: a = new DMASTAdd(token.Location, a, b); break;
                        case TokenType.DM_Minus: a = new DMASTSubtract(token.Location, a, b); break;
                    }

                    token = Current();
                }
            }

            return a;
        }

        public DMASTExpression? ExpressionMultiplicationDivisionModulus(bool isTernaryB = false) {
            DMASTExpression? a = ExpressionPower(isTernaryB);

            if (a != null) {
                Token token = Current();

                while (Check(MulDivModTypes)) {
                    Whitespace();
                    DMASTExpression? b = ExpressionPower(isTernaryB);
                    if (b == null) Error("Expected an expression");

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

        public DMASTExpression? ExpressionPower(bool isTernaryB = false) {
            DMASTExpression? a = ExpressionUnary(isTernaryB);

            if (a != null) {
                var loc = Current().Location;

                while (Check(TokenType.DM_StarStar)) {
                    Whitespace();
                    DMASTExpression? b = ExpressionPower(isTernaryB);
                    if (b == null) Error("Expected an expression");
                    a = new DMASTPower(loc, a, b);
                }
            }

            return a;
        }

        public DMASTExpression? ExpressionUnary(bool isTernaryB = false) {
            var loc = Current().Location;

            if (Check(TokenType.DM_Exclamation)) {
                Whitespace();
                DMASTExpression? expression = ExpressionUnary(isTernaryB);
                if (expression == null) Error("Expected an expression");

                return new DMASTNot(loc, expression);
            } else if (Check(TokenType.DM_Tilde)) {
                Whitespace();
                DMASTExpression? expression = ExpressionUnary(isTernaryB);
                if (expression == null) Error("Expected an expression");

                return new DMASTBinaryNot(loc, expression);
            } else if (Check(TokenType.DM_PlusPlus)) {
                Whitespace();
                DMASTExpression? expression = ExpressionSign(isTernaryB);
                if (expression == null) Error("Expected an expression");

                return new DMASTPreIncrement(loc, expression);
            } else if (Check(TokenType.DM_MinusMinus)) {
                Whitespace();
                DMASTExpression? expression = ExpressionSign(isTernaryB);
                if (expression == null) Error("Expected an expression");

                return new DMASTPreDecrement(loc, expression);
            } else {
                DMASTExpression expression = ExpressionSign(isTernaryB);

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

        public DMASTExpression? ExpressionSign(bool isTernaryB = false) {
            Token token = Current();

            if (Check(PlusMinusTypes)) {
                Whitespace();
                DMASTExpression? expression = ExpressionSign();

                if (expression == null) Error("Expected an expression");
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

        public DMASTExpression? ExpressionNew(bool isTernaryB = false) {
            var loc = Current().Location;

            if (Check(TokenType.DM_New)) {
                Whitespace();
                DMASTExpression? type = ExpressionPrimary(allowParentheses: false);
                type = ParseDereference(type, allowCalls: false);
                DMASTCallParameter[]? parameters = ProcCall();

                DMASTExpression? newExpression = type switch {
                    DMASTConstantPath path => new DMASTNewPath(loc, path, parameters),
                    DMASTExpression expr => new DMASTNewExpr(loc, expr, parameters),
                    null => new DMASTNewInferred(loc, parameters),
                };

                newExpression = ParseDereference(newExpression);
                return newExpression;
            }

            return ParseDereference(ExpressionPrimary(), true, isTernaryB);
        }

        public DMASTExpression? ExpressionPrimary(bool allowParentheses = true) {
            var token = Current();
            if (allowParentheses && Check(TokenType.DM_LeftParenthesis)) {
                BracketWhitespace();
                DMASTExpression? inner = Expression();
                BracketWhitespace();
                ConsumeRightParenthesis();

                if (inner is null) {
                    inner = new DMASTVoid(token.Location);
                } else {
                    inner = new DMASTExpressionWrapped(inner.Location, inner);
                }

                return inner;
            }

            var loc = token.Location;
            if (token.Type == TokenType.DM_Var && _allowVarDeclExpression) {
                return new DMASTVarDeclExpression( loc, Path() );
            }

            DMASTExpression? primary = Constant();
            if (primary == null && Path(true) is { } path) {
                primary = new DMASTConstantPath(loc, path);

                while (Check(TokenType.DM_Period)) {
                    DMASTPath? search = Path();
                    if (search == null) Error("Expected a path for an upward search");

                    primary = new DMASTUpwardPathSearch(loc, (DMASTExpressionConstant)primary, search);
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
            }

            primary ??= Identifier();
            primary ??= (DMASTExpression?)Callable();

            if (Check(TokenType.DM_DoubleColon)) {
                primary = ParseScopeIdentifier(primary);
            }

            if (primary != null && allowParentheses) {
                primary = ParseProcCall(primary);
                return primary;
            }

            if (primary == null && Check(TokenType.DM_Call)) {
                Whitespace();
                DMASTCallParameter[]? callParameters = ProcCall();
                if (callParameters == null || callParameters.Length < 1 || callParameters.Length > 2) Error("Call must have 2 parameters");
                Whitespace();
                DMASTCallParameter[]? procParameters = ProcCall();
                if (procParameters == null) Error("Expected proc parameters");

                primary = new DMASTCall(loc, callParameters, procParameters);
            }

            return primary;
        }

        public DMASTExpression? Constant() {
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

        protected bool Newline() {
            bool hasNewline = Check(TokenType.Newline);

            while (Check(TokenType.Newline)) {
            }
            return hasNewline;
        }

        protected bool Whitespace(bool includeIndentation = false) {
            if (includeIndentation) {
                bool hadWhitespace = false;

                while (Check(WhitespaceTypes)) hadWhitespace = true;
                return hadWhitespace;
            } else {
                return Check(TokenType.DM_Whitespace);
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

                        if (!Check(WhitespacedDereferenceTypes)) {
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
            if (expression is not (IDMASTCallable or DMASTIdentifier)) return expression;

            Whitespace();

            DMASTIdentifier? identifier = expression as DMASTIdentifier;

            if (identifier?.Identifier == "pick") {
                DMASTPick.PickValue[]? pickValues = PickArguments();

                if (pickValues != null) {
                    return new DMASTPick(identifier.Location, pickValues);
                }
            }

            DMASTCallParameter[]? callParameters = ProcCall();
            if (callParameters != null) {
                if (expression is IDMASTCallable callable) {
                    return new DMASTProcCall(expression.Location, callable, callParameters);
                }

                switch (identifier.Identifier) {
                    case "list": return new DMASTList(identifier.Location, callParameters);
                    case "newlist": return new DMASTNewList(identifier.Location, callParameters);
                    case "addtext": return new DMASTAddText(identifier.Location, callParameters);
                    case "prob":
                        if (callParameters.Length != 1)
                            Error("prob() takes 1 argument");
                        if (callParameters[0].Key != null)
                            Error("prob() does not take a named argument");

                        return new DMASTProb(identifier.Location, callParameters[0].Value);
                    case "input": {
                        Whitespace();
                        DMValueType? types = AsTypes(out _);
                        Whitespace();
                        DMASTExpression? list = null;

                        if (Check(TokenType.DM_In)) {
                            Whitespace();
                            list = Expression();
                        }

                        return new DMASTInput(identifier.Location, callParameters, types, list);
                    }
                    case "initial": {
                        if (callParameters.Length != 1) Error("initial() requires 1 argument");

                        return new DMASTInitial(identifier.Location, callParameters[0].Value);
                    }
                    case "nameof": {
                        if (callParameters.Length != 1) Error("nameof() requires 1 argument");

                        return new DMASTNameof(identifier.Location, callParameters[0].Value);
                    }
                    case "issaved": {
                        if (callParameters.Length != 1) Error("issaved() requires 1 argument");

                        return new DMASTIsSaved(identifier.Location, callParameters[0].Value);
                    }
                    case "sin": {
                        if (callParameters.Length != 1) Error("sin() requires 1 argument");

                        return new DMASTSin(identifier.Location, callParameters[0].Value);
                    }
                    case "cos": {
                        if (callParameters.Length != 1) Error("cos() requires 1 argument");

                        return new DMASTCos(identifier.Location, callParameters[0].Value);
                    }
                    case "tan": {
                        if (callParameters.Length != 1) Error("tan() requires 1 argument");

                        return new DMASTTan(identifier.Location, callParameters[0].Value);
                    }
                    case "arcsin": {
                        if (callParameters.Length != 1) Error("arcsin() requires 1 argument");

                        return new DMASTArcsin(identifier.Location, callParameters[0].Value);
                    }
                    case "arccos": {
                        if (callParameters.Length != 1) Error("arccos() requires 1 argument");

                        return new DMASTArccos(identifier.Location, callParameters[0].Value);
                    }
                    case "arctan": {
                        if (callParameters.Length != 1 && callParameters.Length != 2)
                            Error("arctan() requires 1 or 2 arguments");
                        if (callParameters.Length == 1)
                            return new DMASTArctan(identifier.Location, callParameters[0].Value);
                        return new DMASTArctan2(identifier.Location, callParameters[0].Value, callParameters[1].Value);
                    }
                    case "sqrt": {
                        if (callParameters.Length != 1) Error("sqrt() requires 1 argument");

                        return new DMASTSqrt(identifier.Location, callParameters[0].Value);
                    }
                    case "log": {
                        if (callParameters.Length != 1 && callParameters.Length != 2)
                            Error("log() requires 1 or 2 arguments");
                        if (callParameters.Length == 1)
                            return new DMASTLog(identifier.Location, callParameters[0].Value, null);
                        return new DMASTLog(identifier.Location, callParameters[1].Value, callParameters[0].Value);
                    }
                    case "abs": {
                        if (callParameters.Length != 1) Error("abs() requires 1 argument");

                        return new DMASTAbs(identifier.Location, callParameters[0].Value);
                    }
                    case "istype": {
                        if (callParameters.Length == 1) {
                            return new DMASTImplicitIsType(identifier.Location, callParameters[0].Value);
                        } else if (callParameters.Length == 2) {
                            return new DMASTIsType(identifier.Location, callParameters[0].Value, callParameters[1].Value);
                        } else {
                            Error("istype() requires 1 or 2 arguments");
                            break;
                        }
                    }
                    case "isnull": {
                        if (callParameters.Length != 1) Error("isnull() requires exactly 1 argument");

                        return new DMASTIsNull(identifier.Location, callParameters[0].Value);
                    }
                    case "get_step": {
                        if (callParameters.Length != 2) Error("get_step() requires exactly 2 arguments");

                        return new DMASTGetStep(identifier.Location, callParameters[0].Value, callParameters[1].Value);
                    }
                    case "get_dir": {
                        if (callParameters.Length != 2) Error("get_dir() requires exactly 2 arguments");

                        return new DMASTGetDir(identifier.Location, callParameters[0].Value, callParameters[1].Value);
                    }
                    case "length": {
                        if (callParameters.Length != 1) Error("length() requires exactly 1 argument");

                        return new DMASTLength(identifier.Location, callParameters[0].Value);
                    }
                    case "text": {
                        if (callParameters.Length == 0) Error("text() requires at least 1 argument");

                        if (callParameters[0].Value is DMASTConstantString constantString) {
                            if (callParameters.Length > 1) Error("text() expected 1 argument");

                            return constantString;
                        } else if (callParameters[0].Value is DMASTStringFormat formatText) {
                            if (formatText == null) Error("text()'s first argument must be a string format");

                            List<int> emptyValueIndices = new();
                            for (int i = 0; i < formatText.InterpolatedValues.Length; i++) {
                                if (formatText.InterpolatedValues[i] == null) emptyValueIndices.Add(i);
                            }

                            if (callParameters.Length != emptyValueIndices.Count + 1) Error("text() was given an invalid amount of arguments for the string");
                            for (int i = 0; i < emptyValueIndices.Count; i++) {
                                int emptyValueIndex = emptyValueIndices[i];

                                formatText.InterpolatedValues[emptyValueIndex] = callParameters[i + 1].Value;
                            }

                            return formatText;
                        } else {
                            Error("text() expected a string as the first argument");
                            break;
                        }
                    }
                    case "locate": {
                        if (callParameters.Length > 3) Error("locate() was given too many arguments");

                        if (callParameters.Length == 3) { //locate(X, Y, Z)
                            return new DMASTLocateCoordinates(identifier.Location, callParameters[0].Value, callParameters[1].Value, callParameters[2].Value);
                        } else {
                            Whitespace();

                            DMASTExpression? container = null;
                            if (Check(TokenType.DM_In)) {
                                Whitespace();

                                container = Expression();
                                if (container == null) Error("Expected a container for locate()");
                            }

                            DMASTExpression? type = null;
                            if (callParameters.Length == 2) {
                                type = callParameters[0].Value;
                                container = callParameters[1].Value;
                            } else if (callParameters.Length == 1) {
                                type = callParameters[0].Value;
                            }

                            return new DMASTLocate(identifier.Location, type, container);
                        }
                    }
                    case "gradient": {
                        return new DMASTGradient(identifier.Location, callParameters);
                    }
                    case "rgb": {
                        if (callParameters.Length is < 3 or > 5)
                            Error("Expected 3 to 5 arguments for rgb()");

                        return new DMASTRgb(identifier.Location, callParameters);
                    }
                    default: return new DMASTProcCall(identifier.Location, new DMASTCallableProcIdentifier(identifier.Location, identifier.Identifier), callParameters);
                }
            }

            return expression;
        }

        private DMValueType? AsTypes(out DMASTPath? path, bool allowPath = false) {
            path = null;
            if (Check(TokenType.DM_As)) {
                DMValueType type = DMValueType.Anything;

                Whitespace();
                bool parenthetical = Check(TokenType.DM_LeftParenthesis);
                bool closed = false;

                do {
                    Whitespace();
                    Token typeToken = Current();

                    if (parenthetical) {
                        closed = Check(TokenType.DM_RightParenthesis);
                        if (closed) break;
                    }


                    if (!Check(new TokenType[] { TokenType.DM_Identifier, TokenType.DM_Null })) {
                        // Proc return types
                        path = Path();
                        if (allowPath) {
                            if (path is null) {
                                DMCompiler.Emit(WarningCode.BadToken, typeToken.Location, "Expected value type or path");
                            }
                            type |= DMValueType.Path;
                        } else {
                            DMCompiler.Emit(WarningCode.BadToken, typeToken.Location, "Expected value type");
                        }
                    } else {
                        switch (typeToken.Text) {
                            case "anything": type |= DMValueType.Anything; break;
                            case "null": type |= DMValueType.Null; break;
                            case "text": type |= DMValueType.Text; break;
                            case "obj": type |= DMValueType.Obj; break;
                            case "mob": type |= DMValueType.Mob; break;
                            case "turf": type |= DMValueType.Turf; break;
                            case "num": type |= DMValueType.Num; break;
                            case "message": type |= DMValueType.Message; break;
                            case "area": type |= DMValueType.Area; break;
                            case "color": type |= DMValueType.Color; break;
                            case "file": type |= DMValueType.File; break;
                            case "command_text": type |= DMValueType.CommandText; break;
                            case "sound": type |= DMValueType.Sound; break;
                            case "icon": type |= DMValueType.Icon; break;
                            case "opendream_unimplemented": type |= DMValueType.Unimplemented; break;
                            case "opendream_compiletimereadonly": type |= DMValueType.CompiletimeReadonly; break;
                            default: Error("Invalid value type '" + typeToken.Text + "'"); break;
                        }
                    }
                    Whitespace();
                } while (Check(TokenType.DM_Bar));

                if (parenthetical && !closed) {
                    Whitespace();
                    ConsumeRightParenthesis();
                }

                return type;
            }

            return null;
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
