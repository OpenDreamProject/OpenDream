using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DMCompiler.Compiler.DMPreprocessor;
using DMCompiler.DM.Expressions;
using OpenDreamShared.Compiler;
using OpenDreamShared.Dream;
using DereferenceType = DMCompiler.Compiler.DM.DMASTDereference.DereferenceType;
using OpenDreamShared.Dream.Procs;
using String = System.String;

namespace DMCompiler.Compiler.DM {
    public partial class DMParser : Parser<Token> {
        public const char StringFormatCharacter = (char)0xFF;

        private DreamPath _currentPath = DreamPath.Root;

        private bool _unimplementedWarnings;

        private bool _allowVarDeclExpression = false;

        public DMParser(DMLexer lexer, bool unimplementedWarnings) : base(lexer) {
            _unimplementedWarnings = unimplementedWarnings;
        }

        private static readonly TokenType[] AssignTypes =
        {
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
            TokenType.DM_ModulusEquals
        };

        /// <remarks>This (and other similar TokenType[] sets here) is public because <see cref="DMPreprocessorParser"/> needs it.</remarks>
        public static readonly TokenType[] ComparisonTypes =
        {
            TokenType.DM_EqualsEquals,
            TokenType.DM_ExclamationEquals,
            TokenType.DM_TildeEquals,
            TokenType.DM_TildeExclamation
        };

        public static readonly TokenType[] LtGtComparisonTypes =
        {
            TokenType.DM_LessThan,
            TokenType.DM_LessThanEquals,
            TokenType.DM_GreaterThan,
            TokenType.DM_GreaterThanEquals
        };

        private static readonly TokenType[] ShiftTypes =
        {
            TokenType.DM_LeftShift,
            TokenType.DM_RightShift
        };

        public static readonly TokenType[] PlusMinusTypes =
        {
            TokenType.DM_Plus,
            TokenType.DM_Minus,
        };

        public static readonly TokenType[] MulDivModTypes =
        {
            TokenType.DM_Star,
            TokenType.DM_Slash,
            TokenType.DM_Modulus
        };

        private static readonly TokenType[] DereferenceTypes =
        {
            TokenType.DM_Period,
            TokenType.DM_Colon,
            TokenType.DM_QuestionPeriod,
            TokenType.DM_QuestionColon
        };

        private static readonly TokenType[] WhitespaceTypes =
        {
            TokenType.DM_Whitespace,
            TokenType.DM_Indent,
            TokenType.DM_Dedent
        };

        private static readonly TokenType[] IdentifierTypes = {TokenType.DM_Identifier, TokenType.DM_Step};

        private static readonly TokenType[]  ValidPathElementTokens = {
            TokenType.DM_Identifier,
            TokenType.DM_Var,
            TokenType.DM_Proc,
            TokenType.DM_Step,
            TokenType.DM_Throw,
            TokenType.DM_Null,
            TokenType.DM_Switch,
            TokenType.DM_Spawn
        };

        private static readonly TokenType[] ForSeparatorTypes =
        {
            TokenType.DM_Semicolon,
            TokenType.DM_Comma
        };

        public DMASTFile File() {
            var loc = Current().Location;
            List<DMASTStatement> statements = new();

            while (Current().Type != TokenType.EndOfFile) {
                try {
                    List<DMASTStatement> blockInner = BlockInner();

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

        public List<DMASTStatement> BlockInner() {
            List<DMASTStatement> statements = new();

            do {
                Whitespace();

                try {
                    DMASTStatement statement = Statement();

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

        public DMASTStatement Statement(bool requireDelimiter = true)
        {
            var loc = Current().Location;
            DMASTPath path = Path();

            if (path != null) {
                DreamPath oldPath = _currentPath;
                Whitespace();
                _currentPath = _currentPath.Combine(path.Path);
                if (_currentPath.LastElement == "proc") _currentPath = _currentPath.RemoveElement(-1);

                try {
                    DMASTStatement statement = null;

                    //Proc definition
                    if (Check(TokenType.DM_LeftParenthesis)) {
                        DMCompiler.VerbosePrint($"Parsing proc {_currentPath}()");
                        BracketWhitespace();
                        var parameters = DefinitionParameters();

                        if (Current().Type != TokenType.DM_RightParenthesis && Current().Type != TokenType.DM_Comma &&
                            !Check(TokenType.DM_IndeterminateArgs))
                        {
                            if (parameters.Count > 0) // Separate error handling mentions the missing right-paren
                            {
                                Error($"error: {parameters.Last().Name}: missing comma ',' or right-paren ')'", false);
                            }
                            parameters.AddRange(DefinitionParameters());
                        }
                        if (!Check(TokenType.DM_IndeterminateArgs) && Current().Type != TokenType.DM_RightParenthesis && Current().Type != TokenType.EndOfFile) {
                            // BYOND doesn't specify the arg
                            Error($"error: bag argument definition '{Current().PrintableText}'", false);
                            Advance();
                            BracketWhitespace();
                            Check(TokenType.DM_Comma);
                            BracketWhitespace();
                            parameters.AddRange(DefinitionParameters());
                        }

                        BracketWhitespace();
                        ConsumeRightParenthesis();
                        Whitespace();

                        DMASTProcBlockInner procBlock = ProcBlock();
                        if (procBlock == null) {
                            DMASTProcStatement procStatement = ProcStatement();

                            if (procStatement != null) {
                                procBlock = new DMASTProcBlockInner(loc, new DMASTProcStatement[] { procStatement });
                            }
                        }

                        statement = new DMASTProcDefinition(loc, _currentPath, parameters.ToArray(), procBlock);
                    }

                    //Object definition
                    if (statement == null) {
                        DMASTBlockInner block = Block();

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

                            DMASTExpression value = null;
                            PathArray(ref varPath, out value);

                            if (Check(TokenType.DM_Equals)) {
                                if (value != null) Warning("List doubly initialized");

                                Whitespace();
                                value = Expression();
                                if (value == null) Error("Expected an expression");
                            }

                            if (value == null) value = new DMASTConstantNull(loc);

                            var valType = AsTypes() ?? DMValueType.Anything;
                            var varDef = new DMASTObjectVarDefinition(loc, varPath, value, valType);

                            varDefinitions.Add(varDef);
                            if (Check(TokenType.DM_Comma)) {
                                Whitespace();
                                DMASTPath newVarPath = Path();
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
                        DMASTExpression value = Expression();
                        if (value == null) Error("Expected an expression");

                        statement = new DMASTObjectVarOverride(loc, _currentPath, value);
                    }

                    //Empty object definition
                    if (statement == null) {
                        DMCompiler.VerbosePrint($"Parsed object {_currentPath}");
                        statement = new DMASTObjectDefinition(loc, _currentPath, null);
                    }

                    if (requireDelimiter && !PeekDelimiter() && Current().Type != TokenType.DM_Dedent) {
                        Error("Expected end of object statement");
                    }

                    return statement;
                } finally {
                    _currentPath = oldPath;
                }
            }

            return null;
        }

        public DMASTPath Path(bool expression = false) {
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

            string pathElement = PathElement();
            if (pathElement != null) {
                List<string> pathElements = new() { pathElement };

                while (pathElement != null && Check(TokenType.DM_Slash)) {
                    pathElement = PathElement();

                    if (pathElement != null) {
                        pathElements.Add(pathElement);
                    }
                }

                return new DMASTPath(firstToken.Location, new DreamPath(pathType, pathElements.ToArray()));
            } else if (hasPathTypeToken) {
                if (expression) ReuseToken(firstToken);

                return null;
            }

            return null;
        }

        public string PathElement() {
            Token elementToken = Current();
            if (Check(ValidPathElementTokens)) {
                return elementToken.Text;
            } else {
                return null;
            }
        }

        public bool PathArray(ref DreamPath path, out DMASTExpression implied_value) {
            implied_value = null;
            if (Current().Type == TokenType.DM_LeftBracket)
            {
                var loc = Current().Location;
                // Trying to use path.IsDescendantOf(DreamPath.List) here doesn't work
                if (!path.Elements[..^1].Contains("list")) {
                    var elements = path.Elements.ToList();
                    elements.Insert(elements.IndexOf("var") + 1, "list");
                    path = new DreamPath("/" + String.Join("/", elements));
                }

                List<DMASTCallParameter> sizes = new(2); // Most common is 1D or 2D lists

                while (Check(TokenType.DM_LeftBracket))
                {
                    Whitespace();

                    var size = Expression();
                    if (size is not null)
                    {
                        sizes.Add(new DMASTCallParameter(size.Location, size));
                    }

                    ConsumeRightBracket();
                    Whitespace();
                }

                if (sizes.Count > 0) {
                    implied_value = new DMASTNewPath(loc, new DMASTPath(loc, DreamPath.List), sizes.ToArray());
                }

                return true;
            }
            return false;
        }

        public DMASTCallable Callable() {
            var loc = Current().Location;
            if (Check(TokenType.DM_SuperProc)) return new DMASTCallableSuper(loc);
            if (Check(TokenType.DM_Period)) return new DMASTCallableSelf(loc);

            return null;
        }

        public DMASTIdentifier Identifier() {
            Token token = Current();
            return Check(IdentifierTypes) ? new DMASTIdentifier(token.Location, token.Text) : null;
        }

        public DMASTBlockInner Block() {
            Token beforeBlockToken = Current();
            bool hasNewline = Newline();

            DMASTBlockInner block = BracedBlock();
            if (block == null) block = IndentedBlock();

            if (block == null && hasNewline) {
                ReuseToken(beforeBlockToken);
            }

            return block;
        }

        public DMASTBlockInner BracedBlock() {
            var loc = Current().Location;
            if (Check(TokenType.DM_LeftCurlyBracket)) {
                Whitespace();
                Newline();
                bool isIndented = Check(TokenType.DM_Indent);
                List<DMASTStatement> blockInner = BlockInner();
                if (isIndented) Check(TokenType.DM_Dedent);
                Newline();
                Consume(TokenType.DM_RightCurlyBracket, "Expected '}'");

                return new DMASTBlockInner(loc, blockInner.ToArray());
            }

            return null;
        }

        public DMASTBlockInner IndentedBlock() {
            var loc = Current().Location;
            if (Check(TokenType.DM_Indent)) {
                List<DMASTStatement> blockInner = BlockInner();

                if (blockInner != null) {
                    Newline();
                    Consume(TokenType.DM_Dedent, "Expected dedent");

                    return new DMASTBlockInner(loc, blockInner.ToArray());
                }
            }

            return null;
        }

        public DMASTProcBlockInner ProcBlock() {
            Token beforeBlockToken = Current();
            bool hasNewline = Newline();

            DMASTProcBlockInner procBlock = BracedProcBlock();
            if (procBlock == null) procBlock = IndentedProcBlock();

            if (procBlock == null && hasNewline) {
                ReuseToken(beforeBlockToken);
            }

            return procBlock;
        }

        public DMASTProcBlockInner BracedProcBlock() {
            var loc = Current().Location;
            if (Check(TokenType.DM_LeftCurlyBracket)) {
                DMASTProcBlockInner block;

                Whitespace();
                Newline();
                if (Current().Type == TokenType.DM_Indent) {
                    block = IndentedProcBlock();
                    Newline();
                    Consume(TokenType.DM_RightCurlyBracket, "Expected '}'");
                } else {
                    List<DMASTProcStatement> statements = new();

                    do {
                        List<DMASTProcStatement> blockInner = ProcBlockInner();
                        if (blockInner != null) statements.AddRange(blockInner);

                        if (!Check(TokenType.DM_RightCurlyBracket)) {
                            Error("Expected end of proc statement", throwException: false);
                            LocateNextStatement();
                            Delimiter();
                        } else {
                            break;
                        }
                    } while (true);

                    block = new DMASTProcBlockInner(loc, statements.ToArray());
                }

                return block;
            }

            return null;
        }

        public DMASTProcBlockInner IndentedProcBlock() {
            var loc = Current().Location;
            if (Check(TokenType.DM_Indent)) {
                List<DMASTProcStatement> statements = new();

                do {
                    List<DMASTProcStatement> blockInner = ProcBlockInner();
                    if (blockInner != null) statements.AddRange(blockInner);

                    if (!Check(TokenType.DM_Dedent)) {
                        Error("Expected end of proc statement", throwException: false);
                        LocateNextStatement();
                        Delimiter();
                    } else {
                        break;
                    }
                } while (true);

                return new DMASTProcBlockInner(loc, statements.ToArray());
            }

            return null;
        }

        public List<DMASTProcStatement> ProcBlockInner() {
            List<DMASTProcStatement> procStatements = new();

            DMASTProcStatement statement = null;
            do {
                Whitespace();

                try {
                    statement = ProcStatement();
                    if (statement != null) {
                        Whitespace();
                        procStatements.Add(statement);
                    }
                } catch (CompileErrorException) {
                    LocateNextStatement();

                    //LocateNextStatement() may have landed us on another indented/braced block
                    DMASTProcBlockInner blockInner = ProcBlock();
                    if (blockInner != null) procStatements.AddRange(blockInner.Statements);
                }
            } while (Delimiter() || statement is DMASTProcStatementLabel);
            Whitespace();

            if (procStatements.Count == 0) return null;
            return procStatements;
        }

        public DMASTProcStatement ProcStatement()
        {
            var loc = Current().Location;
            var leadingColon = Check(TokenType.DM_Colon);

            DMASTExpression expression = null;
            if (Current().Type != TokenType.DM_Var) {
                expression = Expression();
            }

            if (leadingColon && expression is not DMASTIdentifier)
            {
                Error("Expected a label identifier");
            }

            if (expression != null)
            {
                switch (expression)
                {
                    case DMASTIdentifier identifier:
                        Check(TokenType.DM_Colon);
                        return Label(identifier);
                    case DMASTLeftShift leftShift:
                    {
                        DMASTProcCall procCall = leftShift.B as DMASTProcCall;

                        if (procCall != null && procCall.Callable is DMASTCallableProcIdentifier identifier) {
                            if (identifier.Identifier == "browse") {
                                if (procCall.Parameters.Length != 1 && procCall.Parameters.Length != 2) Error("browse() requires 1 or 2 parameters");

                                DMASTExpression body = procCall.Parameters[0].Value;
                                DMASTExpression options = (procCall.Parameters.Length == 2) ? procCall.Parameters[1].Value : new DMASTConstantNull(loc);
                                return new DMASTProcStatementBrowse(loc, leftShift.A, body, options);
                            } else if (identifier.Identifier == "browse_rsc") {
                                if (procCall.Parameters.Length != 1 && procCall.Parameters.Length != 2) Error("browse_rsc() requires 1 or 2 parameters");

                                DMASTExpression file = procCall.Parameters[0].Value;
                                DMASTExpression filepath = (procCall.Parameters.Length == 2) ? procCall.Parameters[1].Value : new DMASTConstantNull(loc);
                                return new DMASTProcStatementBrowseResource(loc, leftShift.A, file, filepath);
                            } else if (identifier.Identifier == "output") {
                                if (procCall.Parameters.Length != 2) Error("output() requires 2 parameters");

                                DMASTExpression msg = procCall.Parameters[0].Value;
                                DMASTExpression control = procCall.Parameters[1].Value;
                                return new DMASTProcStatementOutputControl(loc, leftShift.A, msg, control);
                            }
                        }

                        break;
                    }
                }

                return new DMASTProcStatementExpression(loc, expression);
            } else {
                // These are sorted by frequency, except If() is moved to the end because it's really slow (relatively)
                DMASTProcStatement procStatement = Return();
                if (procStatement == null) procStatement = ProcVarDeclaration();
                if (procStatement == null) procStatement = For();
                if (procStatement == null) procStatement = Set();
                if (procStatement == null) procStatement = Switch();
                if (procStatement == null) procStatement = Continue();
                if (procStatement == null) procStatement = Break();
                if (procStatement == null) procStatement = Spawn();
                if (procStatement == null) procStatement = While();
                if (procStatement == null) procStatement = DoWhile();
                if (procStatement == null) procStatement = Throw();
                if (procStatement == null) procStatement = Del();
                if (procStatement == null) procStatement = TryCatch();
                if (procStatement == null) procStatement = Goto();
                if (procStatement == null) procStatement = If();

                if (procStatement != null) {
                    Whitespace();
                }


                return procStatement;
            }
        }

        public DMASTProcStatement ProcVarDeclaration(bool allowMultiple = true) {
            Token firstToken = Current();
            bool wasSlash = Check(TokenType.DM_Slash);

            if (Check(TokenType.DM_Var)) {
                if (wasSlash) Error("Unsupported root variable declaration");

                Whitespace();
                DMASTProcStatementVarDeclaration[] vars = ProcVarEnd(allowMultiple);
                if (vars == null) Error("Expected a var declaration");

                if (vars.Length > 1) {
                    return new DMASTProcStatementMultipleVarDeclarations(firstToken.Location, vars);
                } else {
                    return vars[0];
                }
            } else if (wasSlash) {
                ReuseToken(firstToken);
            }

            return null;
        }

        public DMASTProcStatementVarDeclaration[] ProcVarBlock(DMASTPath varPath) {
            Token newlineToken = Current();
            bool hasNewline = Newline();

            if (Check(TokenType.DM_Indent)) {
                List<DMASTProcStatementVarDeclaration> varDeclarations = new();

                while (!Check(TokenType.DM_Dedent)) {
                    DMASTProcStatementVarDeclaration[] varDecl = ProcVarEnd(true, path: varPath);
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
                    DMASTProcStatementVarDeclaration[] varDecl = ProcVarEnd(true, path: varPath);
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

        public DMASTProcStatementVarDeclaration[] ProcVarEnd(bool allowMultiple, DMASTPath path = null) {
            var loc = Current().Location;
            DMASTPath varPath = Path();

            if (allowMultiple) {
                DMASTProcStatementVarDeclaration[] block = ProcVarBlock(varPath);
                if (block != null) return block;
            }

            if (varPath == null) return null;
            if (path != null) varPath = new DMASTPath(loc, path.Path.Combine(varPath.Path));

            List<DMASTProcStatementVarDeclaration> varDeclarations = new();
            while (true) {
                DMASTExpression value = null;
                Whitespace();

                PathArray(ref varPath.Path, out value);

                if (Check(TokenType.DM_Equals)) {
                    Whitespace();
                    value = Expression();

                    if (value == null) Error("Expected an expression");
                }

                AsTypes();

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

        public DMASTProcStatementReturn Return() {
            if (Check(TokenType.DM_Return)) {
                var loc = Current().Location;
                Whitespace();
                DMASTExpression value = Expression();

                return new DMASTProcStatementReturn(loc, value);
            } else {
                return null;
            }
        }

        public DMASTProcStatementBreak Break() {
            if (Check(TokenType.DM_Break))
            {
                var loc = Current().Location;
                Whitespace();
                DMASTExpression label = Expression();

                return new DMASTProcStatementBreak(loc, label as DMASTIdentifier);
            } else {
                return null;
            }
        }

        public DMASTProcStatementContinue Continue() {
            if (Check(TokenType.DM_Continue)) {
                var loc = Current().Location;
                Whitespace();
                DMASTExpression label = Expression();

                return new DMASTProcStatementContinue(loc, label as DMASTIdentifier);
            } else {
                return null;
            }
        }

        public DMASTProcStatementGoto Goto() {
            if (Check(TokenType.DM_Goto)) {
                var loc = Current().Location;
                Whitespace();
                DMASTIdentifier label = Identifier();

                return new DMASTProcStatementGoto(loc, label);
            } else {
                return null;
            }
        }

        public DMASTProcStatementDel Del() {
            if (Check(TokenType.DM_Del)) {
                var loc = Current().Location;
                Whitespace();
                bool hasParenthesis = Check(TokenType.DM_LeftParenthesis);
                Whitespace();
                DMASTExpression value = Expression();
                if (value == null) Error("Expected value to delete");
                if (hasParenthesis) ConsumeRightParenthesis();

                return new DMASTProcStatementDel(loc, value);
            } else {
                return null;
            }
        }

        public DMASTProcStatementSet Set() {
            if (Check(TokenType.DM_Set)) {
                Whitespace();
                Token attributeToken = Current();

                if (Check(TokenType.DM_Identifier)) {
                    Whitespace();
                    Consume(new TokenType[] { TokenType.DM_Equals, TokenType.DM_In }, "Expected '=' or 'in'");
                    Whitespace();
                    DMASTExpression value = Expression();
                    if (value == null) Error("Expected an expression");

                    return new DMASTProcStatementSet(attributeToken.Location, attributeToken.Text, value);
                } else {
                    Error("Expected property name");
                }
            }

            return null;
        }

        public DMASTProcStatementSpawn Spawn() {
            if (Check(TokenType.DM_Spawn)) {
                var loc = Current().Location;
                Whitespace();
                bool hasArg = Check(TokenType.DM_LeftParenthesis);
                DMASTExpression delay = null;

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

                DMASTProcBlockInner body = ProcBlock();
                if (body == null) {
                    DMASTProcStatement statement = ProcStatement();

                    if (statement == null) Error("Expected body or statement");
                    body = new DMASTProcBlockInner(loc, new DMASTProcStatement[] { statement });
                }

                return new DMASTProcStatementSpawn(loc, delay ?? new DMASTConstantInteger(loc, 0), body);
            } else {
                return null;
            }
        }

        public DMASTProcStatementIf If() {
            if (Check(TokenType.DM_If)) {
                var loc = Current().Location;
                Whitespace();
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                BracketWhitespace();
                DMASTExpression condition = Expression();
                if (condition == null) Error("Expected a condition");
                BracketWhitespace();
                ConsumeRightParenthesis();
                Whitespace();
                Check(TokenType.DM_Colon);
                Whitespace();

                DMASTProcStatement procStatement = ProcStatement();
                DMASTProcBlockInner body;
                DMASTProcBlockInner elseBody = null;

                if (procStatement != null) {
                    body = new DMASTProcBlockInner(loc, new DMASTProcStatement[] { procStatement });
                } else {
                    body = ProcBlock();
                }

                if (body == null) body = new DMASTProcBlockInner(loc, new DMASTProcStatement[0]);
                Token afterIfBody = Current();
                bool newLineAfterIf = Delimiter();
                if (newLineAfterIf) Whitespace();
                if (Check(TokenType.DM_Else)) {
                    Whitespace();
                    Check(TokenType.DM_Colon);
                    Whitespace();
                    procStatement = ProcStatement();

                    if (procStatement != null) {
                        elseBody = new DMASTProcBlockInner(loc, new DMASTProcStatement[] { procStatement });
                    } else {
                        elseBody = ProcBlock();
                    }

                    if (elseBody == null) elseBody = new DMASTProcBlockInner(loc, new DMASTProcStatement[0]);
                } else if (newLineAfterIf) {
                    ReuseToken(afterIfBody);
                }

                return new DMASTProcStatementIf(loc, condition, body, elseBody);
            } else {
                return null;
            }
        }

        public DMASTProcStatement For() {
            if (Check(TokenType.DM_For)) {
                Whitespace();

                var loc = Current().Location;
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                Whitespace();

                if (Check(TokenType.DM_RightParenthesis)) {
                    return new DMASTProcStatementInfLoop(loc, GetForBody());
                }

                _allowVarDeclExpression = true;
                DMASTExpression expr1 = Expression();
                Whitespace();
                AsTypes(); //TODO: Correctly handle
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
                        DMASTExpression endRange = null, step = null;
                        ExpressionTo(ref endRange, ref step);
                        Consume(TokenType.DM_RightParenthesis, "Expected ')' in for after to expression");
                        Whitespace();
                        Newline();
                        return new DMASTProcStatementFor(loc, new DMASTExpressionInRange(loc, assign.Expression, assign.Value, endRange, step), null, null, GetForBody());
                    } else {
                        Error("Expected = before to in for");
                    }
                }
                Whitespace();
                AsTypes(); //TODO: Correctly handle
                Whitespace();
                if (Check(TokenType.DM_In)) {
                    Whitespace();
                    DMASTExpression listExpr = Expression();
                    Whitespace();
                    Consume(TokenType.DM_RightParenthesis, "Expected ')' in for after expression 2");
                    Whitespace();
                    Newline();
                    return new DMASTProcStatementFor(loc, new DMASTExpressionIn(loc, expr1, listExpr), null, null, GetForBody());
                }
                else if (!Check(ForSeparatorTypes)) {
                    Whitespace();
                    Consume(TokenType.DM_RightParenthesis, "Expected ')' in for after expression 1");
                    Whitespace();
                    Newline();
                    return new DMASTProcStatementFor(loc, expr1, null, null, GetForBody());
                }
                if (Check(TokenType.DM_RightParenthesis)) {
                    Whitespace();
                    Newline();
                    return new DMASTProcStatementFor(loc, expr1, null, null, GetForBody());
                }
                Whitespace();
                DMASTExpression expr2 = Expression();
                Whitespace();
                if (expr2 == null) {
                    if (ForSeparatorTypes.Contains(Current().Type)) {
                        expr2 = new DMASTConstantInteger(loc, 1);
                    } else {
                        Error("Expected 2nd expression in for");
                    }
                }
                if (!Check(ForSeparatorTypes)) {
                    Consume(TokenType.DM_RightParenthesis, "Expected ')' in for after expression 2");
                    Whitespace();
                    Newline();
                    return new DMASTProcStatementFor(loc, expr1, expr2, null, GetForBody());
                }
                if (Check(TokenType.DM_RightParenthesis)) {
                    Whitespace();
                    Newline();
                    return new DMASTProcStatementFor(loc, expr1, expr2, null, GetForBody());
                }
                Whitespace();
                DMASTExpression expr3 = Expression();
                Whitespace();
                if (expr3 == null) {
                    if (Current().Type == TokenType.DM_RightParenthesis) {
                        expr3 = new DMASTConstantNull(loc);
                    } else {
                        Error("Expected 3nd expression in for");
                    }
                }
                Consume(TokenType.DM_RightParenthesis, "Expected ')' in for after expression 3");
                Whitespace();
                Newline();
                return new DMASTProcStatementFor(loc, expr1, expr2, expr3, GetForBody());
            }

            return null;

            DMASTProcBlockInner GetForBody() {
                DMASTProcBlockInner body = ProcBlock();
                if (body == null) {
                    var loc = Current().Location;

                    DMASTProcStatement statement;
                    if (Check(TokenType.DM_Semicolon)) {
                        statement = new DMASTProcStatementExpression(loc, new DMASTConstantNull(loc));
                    } else {
                        statement = ProcStatement();
                        if (statement == null) Error("Expected body or statement");
                    }
                    body = new DMASTProcBlockInner(loc, new DMASTProcStatement[] { statement });
                }

                return body;
            }
        }

        public DMASTProcStatement While() {
            if (Check(TokenType.DM_While)) {
                var loc = Current().Location;
                Whitespace();
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                Whitespace();
                DMASTExpression conditional = Expression();
                if (conditional == null) Error("Expected conditional");
                ConsumeRightParenthesis();
                Check(TokenType.DM_Semicolon);
                Whitespace();
                DMASTProcBlockInner body = ProcBlock();

                if (body == null) {
                    DMASTProcStatement statement = ProcStatement();

                    //Loops without a body are valid DM
                    if (statement == null) statement = new DMASTProcStatementContinue(loc);

                    body = new DMASTProcBlockInner(loc, new DMASTProcStatement[] { statement });
                }
                if(conditional is DMASTConstantInteger){
                    if(((DMASTConstantInteger)conditional).Value != 0){
                        return new DMASTProcStatementInfLoop(loc,body);
                    }
                }
                return new DMASTProcStatementWhile(loc, conditional, body);

            }

            return null;
        }

        public DMASTProcStatementDoWhile DoWhile() {
            if (Check(TokenType.DM_Do)) {
                var loc = Current().Location;
                Whitespace();
                DMASTProcBlockInner body = ProcBlock();

                if (body == null) {
                    DMASTProcStatement statement = ProcStatement();
                    if (statement == null) Error("Expected statement");

                    body = new DMASTProcBlockInner(loc, new DMASTProcStatement[] { statement });
                }

                Newline();
                Whitespace();
                Consume(TokenType.DM_While, "Expected 'while'");
                Whitespace();
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                Whitespace();
                DMASTExpression conditional = Expression();
                if (conditional == null) Error("Expected conditional");
                ConsumeRightParenthesis();
                Whitespace();

                return new DMASTProcStatementDoWhile(loc, conditional, body);
            }

            return null;
        }

        public DMASTProcStatementSwitch Switch() {
            if (Check(TokenType.DM_Switch)) {
                var loc = Current().Location;
                Whitespace();
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                Whitespace();
                DMASTExpression value = Expression();
                ConsumeRightParenthesis();
                Whitespace();

                DMASTProcStatementSwitch.SwitchCase[] switchCases = SwitchCases();

                if (switchCases == null) Error("Expected switch cases");
                return new DMASTProcStatementSwitch(loc, value, switchCases);
            }

            return null;
        }

        public DMASTProcStatementSwitch.SwitchCase[] SwitchCases() {
            Token beforeSwitchBlock = Current();
            bool hasNewline = Newline();

            DMASTProcStatementSwitch.SwitchCase[] switchCases = BracedSwitchInner();

            if (switchCases == null) switchCases = IndentedSwitchInner();

            if (switchCases == null && hasNewline) {
                ReuseToken(beforeSwitchBlock);
            }

            return switchCases;
        }

        public DMASTProcStatementSwitch.SwitchCase[] BracedSwitchInner() {
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

        public DMASTProcStatementSwitch.SwitchCase[] IndentedSwitchInner() {
            if (Check(TokenType.DM_Indent)) {
                DMASTProcStatementSwitch.SwitchCase[] switchInner = SwitchInner();
                Consume(TokenType.DM_Dedent, "Expected \"if\" or \"else\"");

                return switchInner;
            }

            return null;
        }

        public DMASTProcStatementSwitch.SwitchCase[] SwitchInner() {
            List<DMASTProcStatementSwitch.SwitchCase> switchCases = new();
            DMASTProcStatementSwitch.SwitchCase switchCase = SwitchCase();

            if (switchCase != null) {
                do {
                    switchCases.Add(switchCase);
                    Newline();
                    Whitespace();
                    switchCase = SwitchCase();
                } while (switchCase != null);
            }

            return switchCases.ToArray();
        }

        public DMASTProcStatementSwitch.SwitchCase SwitchCase() {
            if (Check(TokenType.DM_If)) {
                List<DMASTExpression> expressions = new();

                Whitespace();
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");

                do {
                    BracketWhitespace();

                    DMASTExpression expression = Expression();
                    if (expression == null) {
                        if (expressions.Count == 0) {
                            Error("Expected an expression");
                        } else //Eat a trailing comma if there's at least 1 expression
                          {
                            break;
                        }
                    }

                    if (Check(TokenType.DM_To)) {
                        var loc = Current().Location;
                        Whitespace();
                        DMASTExpression rangeEnd = Expression();
                        if (rangeEnd == null) Error("Expected an upper limit");

                        expressions.Add(new DMASTSwitchCaseRange(loc, expression, rangeEnd));
                    } else {
                        expressions.Add(expression);
                    }

                    Delimiter();
                } while (Check(TokenType.DM_Comma));
                Whitespace();
                ConsumeRightParenthesis();
                Whitespace();
                DMASTProcBlockInner body = ProcBlock();

                if (body == null) {
                    DMASTProcStatement statement = ProcStatement();
                    var loc = Current().Location;

                    if (statement != null) {
                        body = new DMASTProcBlockInner(loc, new DMASTProcStatement[] { statement });
                    } else {
                        body = new DMASTProcBlockInner(loc, new DMASTProcStatement[0]);
                    }
                }

                return new DMASTProcStatementSwitch.SwitchCaseValues(expressions.ToArray(), body);
            } else if (Check(TokenType.DM_Else)) {
                var loc = Current().Location;
                Whitespace();
                if (Current().Type == TokenType.DM_If)
                {
                    Error("Expected \"if\" or \"else\", \"else if\" is not permitted as a switch case");
                }
                DMASTProcBlockInner body = ProcBlock();

                if (body == null) {
                    DMASTProcStatement statement = ProcStatement();

                    if (statement != null) {
                        body = new DMASTProcBlockInner(loc, new DMASTProcStatement[] { statement });
                    } else {
                        body = new DMASTProcBlockInner(loc, new DMASTProcStatement[0]);
                    }
                }

                return new DMASTProcStatementSwitch.SwitchCaseDefault(body);
            }

            return null;
        }

        public DMASTProcStatementTryCatch TryCatch() {
            if (Check(TokenType.DM_Try)) {
                var loc = Current().Location;
                Whitespace();

                DMASTProcBlockInner tryBody = ProcBlock();
                if (tryBody == null) {
                    DMASTProcStatement statement = ProcStatement();

                    if (statement == null) Error("Expected body or statement");
                    tryBody = new DMASTProcBlockInner(loc, new DMASTProcStatement[] { statement });
                }

                if (_unimplementedWarnings)
                {
                    Warning("Exceptions in 'try/catch' blocks are currently not caught");
                }

                Newline();
                Consume(TokenType.DM_Catch, "Expected catch");
                Whitespace();

                // catch(var/exception/E)
                DMASTProcStatement parameter = null;
                if (Check(TokenType.DM_LeftParenthesis))
                {
                    BracketWhitespace();
                    parameter = ProcVarDeclaration(allowMultiple: false);
                    BracketWhitespace();
                    ConsumeRightParenthesis();
                    Whitespace();
                }

                DMASTProcBlockInner catchBody = ProcBlock();
                if (catchBody == null) {
                    DMASTProcStatement statement = ProcStatement();

                    if (statement != null) catchBody = new DMASTProcBlockInner(loc, new DMASTProcStatement[] { statement });
                }

                return new DMASTProcStatementTryCatch(loc, tryBody, catchBody, parameter);
            }

            return null;
        }

        public DMASTProcStatementThrow Throw()
        {
            if (Check(TokenType.DM_Throw)) {
                var loc = Current().Location;
                if (_unimplementedWarnings)
                {
                    Warning("'throw' is not properly implemented and will just cause an uncaught runtime");
                }
                Whitespace();
                DMASTExpression value = Expression();

                return new DMASTProcStatementThrow(loc, value);
            } else {
                return null;
            }
        }

        public DMASTProcStatementLabel Label(DMASTIdentifier expression)
        {
            Whitespace();
            Newline();

            DMASTProcBlockInner body = ProcBlock();
            if (body == null) {
                var loc = Current().Location;
                DMASTProcStatement statement = ProcStatement();

                if (statement != null) body = new DMASTProcBlockInner(loc, new DMASTProcStatement[] { statement });
            }
            return new DMASTProcStatementLabel(expression.Location, expression.Identifier, body);
        }

        public DMASTCallParameter[] ProcCall() {
            if (Check(TokenType.DM_LeftParenthesis)) {
                BracketWhitespace();

                DMASTCallParameter[] callParameters = CallParameters();
                if (callParameters == null) callParameters = new DMASTCallParameter[0];
                BracketWhitespace();
                ConsumeRightParenthesis();

                return callParameters;
            }

            return null;
        }

        public DMASTPick.PickValue[] PickArguments() {
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
            DMASTExpression expression = Expression();

            if (Check(TokenType.DM_Semicolon)) {
                Whitespace();
                DMASTExpression value = Expression();
                if (value == null) Error("Expected an expression");

                return new DMASTPick.PickValue(expression, value);
            } else if (expression != null) {
                return new DMASTPick.PickValue(null, expression);
            }

            return null;
        }

        public DMASTCallParameter[] CallParameters() {
            List<DMASTCallParameter> parameters = new();
            DMASTCallParameter parameter = CallParameter();
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

        public DMASTCallParameter CallParameter() {
            DMASTExpression expression = Expression();
            if (expression == null)
                return null;

            if (expression is DMASTAssign assign) {
                DMASTExpression key = assign.Expression;
                if (key is DMASTIdentifier identifier) {
                    key = new DMASTConstantString(key.Location, identifier.Identifier);
                } else if (key is DMASTConstantNull) {
                    key = new DMASTConstantString(key.Location, "null");
                }

                return new DMASTCallParameter(assign.Location, assign.Value, key);
            } else {
                return new DMASTCallParameter(expression.Location, expression);
            }
        }

        public List<DMASTDefinitionParameter> DefinitionParameters() {
            List<DMASTDefinitionParameter> parameters = new();
            DMASTDefinitionParameter parameter = DefinitionParameter();

            if (parameter != null) parameters.Add(parameter);

            BracketWhitespace();

            while (Check(TokenType.DM_Comma)) {
                BracketWhitespace();
                parameter = DefinitionParameter();

                if (parameter != null)
                {
                    parameters.Add(parameter);
                    BracketWhitespace();

                }
                if (Check(TokenType.DM_Null)){
                    // Breaking change - BYOND creates a var named null that overrides the keyword. No error.
                    Error($"error: 'null' is not a valid variable name", false);
                    Advance();
                    BracketWhitespace();
                    Check(TokenType.DM_Comma);
                    BracketWhitespace();
                    parameters.AddRange(DefinitionParameters());
                }
            }

            return parameters;
        }

        public DMASTDefinitionParameter DefinitionParameter() {
            DMASTPath path = Path();

            if (path != null) {
                var loc = Current().Location;
                Whitespace();

                DMASTExpression value = null;
                DMValueType type;
                DMASTExpression possibleValues = null;

                PathArray(ref path.Path, out value);

                if (Check(TokenType.DM_Equals)) {
                    Whitespace();
                    value = Expression();
                }

                type = AsTypes() ?? DMValueType.Anything;
                Whitespace();

                if (Check(TokenType.DM_In)) {
                    Whitespace();
                    possibleValues = Expression();
                }

                return new DMASTDefinitionParameter(loc, path, value, type, possibleValues);
            }

            Check(TokenType.DM_IndeterminateArgs);

            return null;
        }

        public DMASTExpression Expression() {
            return ExpressionIn();
        }

        public void ExpressionTo(ref DMASTExpression endRange, ref DMASTExpression step) {
            Whitespace();
            endRange = ExpressionAssign();
            Whitespace();
            if (endRange is null) {
                Error("Missing end range");
            }
            if (Check(TokenType.DM_Step)) {
                Whitespace();
                step = ExpressionAssign();
                Whitespace();
                if (step is null) { Error("Missing step value"); }
            }
        }

        public DMASTExpression ExpressionIn() {
            DMASTExpression value = ExpressionAssign();

            if (value != null && Check(TokenType.DM_In)) {
                var loc = Current().Location;
                Whitespace();
                DMASTExpression list = ExpressionAssign();
                Whitespace();
                if (Check(TokenType.DM_To)) {
                    DMASTExpression endRange = null, step = null;
                    ExpressionTo(ref endRange, ref step);
                    return new DMASTExpressionInRange(loc, value, list, endRange, step);
                }

                return new DMASTExpressionIn(loc, value, list);
            }

            return value;
        }
        public DMASTExpression ExpressionAssign() {
            DMASTExpression expression = ExpressionTernary();

            if (expression != null) {
                Token token = Current();
                if (Check(AssignTypes)) {
                    Whitespace();
                    DMASTExpression value = ExpressionAssign();

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
                        }
                    } else {
                        Error("Expected a value");
                    }
                }
            }

            return expression;
        }



        public DMASTExpression ExpressionTernary() {
            DMASTExpression a = ExpressionOr();

            if (a != null && Check(TokenType.DM_Question)) {
                Whitespace();
                DMASTExpression b = ExpressionTernary();
                if (b == null) Error("Expected an expression");

                /* DM has some really strange behavior when it comes to proc calls and dereferences inside ternaries
                 * Consider the following expression:
                 *      a ? foo():pixel_x
                 * This is ambiguous, it could be either a ternary or a dereference (and an error)
                 *
                 * What DM does here is parse `foo():pixel_x` as a dereference, and attempts to split it into a correct ternary
                 * Everything past the last proc call followed by a dereference becomes "c"
                 * This last dereference must also be a search, otherwise it's a "Expected ':'" error
                 *
                 * None of this happens if there is a whitespace followed by a colon after the "b" expression:
                 *      a ? foo():pixel_x : 2
                 */

                DMASTExpression c;
                if (Check(TokenType.DM_Colon)) {
                    Whitespace();
                    c = ExpressionTernary();
                } else {
                    if (b is DMASTDereference deref) {
                        c = null;

                        DMASTExpression expr;
                        DereferenceType type = default;
                        bool conditional = default;
                        do {
                            if (c == null) {
                                c = new DMASTIdentifier(deref.Location, deref.Property);
                            } else {
                                c = new DMASTDereference(deref.Location, new DMASTIdentifier(deref.Location, deref.Property), ((DMASTIdentifier)c).Identifier, type, conditional);
                            }

                            expr = deref.Expression;
                            type = deref.Type;
                            conditional = deref.Conditional;
                            deref = expr as DMASTDereference;
                        } while (deref != null);

                        if (deref == null && type == DereferenceType.Search) {
                            b = expr;
                        } else {
                            Error("Expected ':'");
                        }
                    } else {
                        Error("Expected ':'");
                        c = null;
                    }
                }

                return new DMASTTernary(a.Location, a, b, c);
            }

            return a;
        }

        public DMASTExpression ExpressionOr() {
            DMASTExpression a = ExpressionAnd();
            if (a != null) {
                var loc = Current().Location;
                while (Check(TokenType.DM_BarBar)) {
                    Whitespace();
                    DMASTExpression b = ExpressionAnd();
                    if (b == null) Error("Expected a second value");
                    a = new DMASTOr(loc, a, b);
                }
            }
            return a;
        }

        public DMASTExpression ExpressionAnd() {
            DMASTExpression a = ExpressionBinaryOr();
            if (a != null)
            {
                var loc = Current().Location;
                while (Check(TokenType.DM_AndAnd)) {
                    Whitespace();
                    DMASTExpression b = ExpressionBinaryOr();
                    if (b == null) Error("Expected a second value");
                    a = new DMASTAnd(loc, a, b);
                }
            }
            return a;
        }

        public DMASTExpression ExpressionBinaryOr() {
            DMASTExpression a = ExpressionBinaryXor();
            if (a != null) {
                var loc = Current().Location;
                while (Check(TokenType.DM_Bar)) {
                    Whitespace();
                    DMASTExpression b = ExpressionBinaryXor();
                    if (b == null) Error("Expected an expression");
                    a = new DMASTBinaryOr(loc, a, b);
                }
            }
            return a;
        }

        public DMASTExpression ExpressionBinaryXor() {
            DMASTExpression a = ExpressionBinaryAnd();
            if (a != null) {
                var loc = Current().Location;
                while (Check(TokenType.DM_Xor)) {
                    Whitespace();
                    DMASTExpression b = ExpressionBinaryAnd();
                    if (b == null) Error("Expected an expression");
                    a = new DMASTBinaryXor(loc, a, b);
                }
            }
            return a;
        }

        public DMASTExpression ExpressionBinaryAnd() {
            DMASTExpression a = ExpressionComparison();
            if (a != null) {
                var loc = Current().Location;
                while (Check(TokenType.DM_And)) {
                    Whitespace();
                    DMASTExpression b = ExpressionComparison();

                    if (b == null) Error("Expected an expression");
                    a = new DMASTBinaryAnd(loc, a, b);
                }
            }
            return a;
        }

        public DMASTExpression ExpressionComparison() {
            DMASTExpression a = ExpressionBitShift();

            if (a != null) {
                Token token = Current();
                while (Check(ComparisonTypes)) {
                    Whitespace();
                    DMASTExpression b = ExpressionBitShift();
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

        public DMASTExpression ExpressionBitShift() {
            DMASTExpression a = ExpressionComparisonLtGt();

            if (a != null) {
                Token token = Current();
                while (Check(ShiftTypes)) {
                    Whitespace();
                    DMASTExpression b = ExpressionComparisonLtGt();
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

        public DMASTExpression ExpressionComparisonLtGt() {
            DMASTExpression a = ExpressionAdditionSubtraction();

            if (a != null) {
                Token token = Current();
                while (Check(LtGtComparisonTypes)) {
                    Whitespace();
                    DMASTExpression b = ExpressionAdditionSubtraction();
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

        public DMASTExpression ExpressionAdditionSubtraction() {
            DMASTExpression a = ExpressionMultiplicationDivisionModulus();

            if (a != null) {
                Token token = Current();
                while (Check(PlusMinusTypes)) {
                    Whitespace();
                    DMASTExpression b = ExpressionMultiplicationDivisionModulus();
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

        public DMASTExpression ExpressionMultiplicationDivisionModulus() {
            DMASTExpression a = ExpressionPower();

            if (a != null) {
                Token token = Current();
                while (Check(MulDivModTypes)) {
                    Whitespace();
                    DMASTExpression b = ExpressionPower();
                    if (b == null) Error("Expected an expression");

                    switch (token.Type) {
                        case TokenType.DM_Star: a = new DMASTMultiply(token.Location, a, b); break;
                        case TokenType.DM_Slash: a = new DMASTDivide(token.Location, a, b); break;
                        case TokenType.DM_Modulus: a = new DMASTModulus(token.Location, a, b); break;
                    }

                    token = Current();
                }
            }

            return a;
        }

        public DMASTExpression ExpressionPower() {
            DMASTExpression a = ExpressionUnary();

            if (a != null)
            {
                var loc = Current().Location;
                while (Check(TokenType.DM_StarStar)) {
                    Whitespace();
                    DMASTExpression b = ExpressionPower();
                    if (b == null) Error("Expected an expression");
                    a = new DMASTPower(loc, a, b);
                }
            }

            return a;
        }

        public DMASTExpression ExpressionUnary() {
            var loc = Current().Location;
            if (Check(TokenType.DM_Exclamation)) {
                Whitespace();
                DMASTExpression expression = ExpressionUnary();
                if (expression == null) Error("Expected an expression");

                return new DMASTNot(loc, expression);
            } else if (Check(TokenType.DM_Tilde)) {
                Whitespace();
                DMASTExpression expression = ExpressionUnary();
                if (expression == null) Error("Expected an expression");

                return new DMASTBinaryNot(loc, expression);
            } else if (Check(TokenType.DM_PlusPlus)) {
                Whitespace();
                DMASTExpression expression = ExpressionSign();
                if (expression == null) Error("Expected an expression");

                return new DMASTPreIncrement(loc, expression);
            } else if (Check(TokenType.DM_MinusMinus)) {
                Whitespace();
                DMASTExpression expression = ExpressionSign();
                if (expression == null) Error("Expected an expression");

                return new DMASTPreDecrement(loc, expression);
            } else {
                DMASTExpression expression = ExpressionSign();

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

        public DMASTExpression ExpressionSign() {
            Token token = Current();

            if (Check(PlusMinusTypes)) {
                Whitespace();
                DMASTExpression expression = ExpressionSign();

                if (expression == null) Error("Expected an expression");
                if (token.Type == TokenType.DM_Minus)
                {
                    switch (expression)
                    {
                        case DMASTConstantInteger integer:
                        {
                            int value = integer.Value;

                            return new DMASTConstantInteger(token.Location, -value);
                        }
                        case DMASTConstantFloat constantFloat:
                        {
                            float value = constantFloat.Value;

                            return new DMASTConstantFloat(token.Location, -value);
                        }
                        default:
                            return new DMASTNegate(token.Location, expression);
                    }
                } else {
                    return expression;
                }
            }

            return ExpressionNew();
        }

        public DMASTExpression ExpressionNew() {
            var loc = Current().Location;
            if (Check(TokenType.DM_New)) {
                Whitespace();
                DMASTExpression type = ExpressionPrimary(allowParentheses: false);
                type = ParseDereference(type, allowCalls: false);
                DMASTCallParameter[] parameters = ProcCall();

                //TODO: These don't need to be separate types
                DMASTExpression newExpression = type switch {
                    DMASTListIndex listIdx => new DMASTNewListIndex(loc, listIdx, parameters),
                    DMASTDereference deref => new DMASTNewDereference(loc, deref, parameters),
                    DMASTIdentifier identifier => new DMASTNewIdentifier(loc, identifier, parameters),
                    DMASTConstantPath path => new DMASTNewPath(loc, path.Value, parameters),
                    null => new DMASTNewInferred(loc, parameters),
                    _ => null
                };

                if (newExpression == null) Error("Invalid type");
                newExpression = ParseDereference(newExpression);
                return newExpression;
            }

            return ParseDereference(ExpressionPrimary());
        }

        public DMASTExpression ExpressionPrimary(bool allowParentheses = true) {
            if (allowParentheses && Check(TokenType.DM_LeftParenthesis))
            {
                BracketWhitespace();
                DMASTExpression inner = Expression();
                BracketWhitespace();
                ConsumeRightParenthesis();

                return inner;
            }

            var loc = Current().Location;
            if (Current().Type == TokenType.DM_Var && _allowVarDeclExpression) {
                return new DMASTVarDeclExpression( loc, Path() );
            }
            DMASTExpression primary = Constant();
            if (primary == null) {
                DMASTPath path = Path(true);

                if (path != null) {
                    primary = new DMASTConstantPath(loc, path);

                    while (Check(TokenType.DM_Period)) {
                        DMASTPath search = Path();
                        if (search == null) Error("Expected a path for an upward search");

                        primary = new DMASTUpwardPathSearch(loc, (DMASTExpressionConstant)primary, search);
                    }

                    Whitespace(); // whitespace between path and modified type

                    //TODO actual modified type support
                    if (Check(TokenType.DM_LeftCurlyBracket)) {
                        if (_unimplementedWarnings) Warning("Modified types are currently not supported and modified values will be ignored.");

                        while (Current().Type != TokenType.DM_RightCurlyBracket && !Check(TokenType.EndOfFile)) Advance();
                        Consume(TokenType.DM_RightCurlyBracket, "Expected '}'");
                        //The lexer tosses in a newline after '}', but we avoid Newline() because we only want to remove the extra newline, not all of them
                        Check(TokenType.Newline);
                    }
                }
            }

            if (primary == null) {
                primary = Identifier();
            }

            if (primary == null) {
                primary = (DMASTExpression)Callable();

                if (primary != null) {
                    primary = ParseProcCall(primary);
                }
            }
            if (primary == null && Check(TokenType.DM_Call)) {
                Whitespace();
                DMASTCallParameter[] callParameters = ProcCall();
                if (callParameters == null || callParameters.Length < 1 || callParameters.Length > 2) Error("Call must have 2 parameters");
                Whitespace();
                DMASTCallParameter[] procParameters = ProcCall();
                if (procParameters == null) Error("Expected proc parameters");

                primary = new DMASTCall(loc, callParameters, procParameters);
            }

            return primary;
        }

        public DMASTExpression Constant() {
            Token constantToken = Current();

            switch (constantToken.Type) {
                case TokenType.DM_Integer: Advance(); return new DMASTConstantInteger(constantToken.Location, (int)constantToken.Value);
                case TokenType.DM_Float: Advance(); return new DMASTConstantFloat(constantToken.Location, (float)constantToken.Value);
                case TokenType.DM_Resource: Advance(); return new DMASTConstantResource(constantToken.Location, (string)constantToken.Value);
                case TokenType.DM_Null: Advance(); return new DMASTConstantNull(constantToken.Location);
                case TokenType.DM_RawString: Advance(); return new DMASTConstantString(constantToken.Location, (string)constantToken.Value);
                case TokenType.DM_String: {
                    string tokenValue = (string)constantToken.Value;
                    StringBuilder stringBuilder = new StringBuilder(tokenValue.Length);
                    List<DMASTExpression>? interpolationValues = null;
                    Advance();

                    int bracketNesting = 0;
                    StringBuilder? insideBrackets = null;
                    StringFormatTypes currentInterpolationType = StringFormatTypes.Stringify;
                    for (int i = 0; i < tokenValue.Length; i++) {
                        char c = tokenValue[i];


                        if (bracketNesting > 0) {
                            insideBrackets?.Append(c); // should never be null
                        }

                        switch (c)
                        {
                            case '[':
                                bracketNesting++;
                                insideBrackets ??= new StringBuilder(tokenValue.Length - stringBuilder.Length);
                                interpolationValues ??= new List<DMASTExpression>(1);
                                break;
                            case ']' when bracketNesting > 0:
                            {
                                bracketNesting--;

                                if (bracketNesting == 0) { //End of expression
                                    insideBrackets.Remove(insideBrackets.Length - 1, 1); //Remove the ending bracket

                                    string insideBracketsText = insideBrackets?.ToString();
                                    if (!String.IsNullOrWhiteSpace(insideBracketsText)) {
                                        DMPreprocessorLexer preprocLexer = new DMPreprocessorLexer(null, constantToken.Location.SourceFile, insideBracketsText);
                                        List<Token> preprocTokens = new();
                                        Token preprocToken;
                                        do {
                                            preprocToken = preprocLexer.GetNextToken();
                                            preprocToken.Location = constantToken.Location;
                                            preprocTokens.Add(preprocToken);
                                        } while (preprocToken.Type != TokenType.EndOfFile);

                                        DMLexer expressionLexer = new DMLexer(constantToken.Location.SourceFile, preprocTokens);
                                        DMParser expressionParser = new DMParser(expressionLexer, _unimplementedWarnings);

                                        DMASTExpression expression = null;
                                        try {
                                            expressionParser.Whitespace(true);
                                            expression = expressionParser.Expression();
                                            if (expression == null) Error("Expected an expression");
                                            if (expressionParser.Current().Type != TokenType.EndOfFile) Error("Expected end of embedded statement");
                                        } catch (CompileErrorException) {
                                        }

                                        if (expressionParser.Errors.Count > 0) Errors.AddRange(expressionParser.Errors);
                                        if (expressionParser.Warnings.Count > 0) Warnings.AddRange(expressionParser.Warnings);
                                        interpolationValues.Add(expression);
                                    } else {
                                        interpolationValues.Add(null);
                                    }

                                    stringBuilder.Append(StringFormatCharacter);
                                    stringBuilder.Append((char)currentInterpolationType);

                                    currentInterpolationType = StringFormatTypes.Stringify;
                                    insideBrackets.Clear();
                                }

                                break;
                            }
                            case '\\' when bracketNesting == 0:
                            {
                                string escapeSequence = String.Empty;

                                if (i == tokenValue.Length) {
                                    Error("Invalid escape sequence");
                                }
                                c = tokenValue[++i];

                                if (char.IsLetter(c)) {
                                    while (i < tokenValue.Length && char.IsLetter(tokenValue[i])) {
                                        escapeSequence += tokenValue[i++];
                                    }
                                    i--;

                                    bool unimplemented = false;
                                    bool skipSpaces = false;
                                    //TODO: Many of these require [] before the macro instead of after. They should verify that there is one.
                                    switch (escapeSequence) {
                                        case "proper":
                                        case "improper":
                                            if (stringBuilder.Length != 0) {
                                                Error($"Escape sequence \"\\{escapeSequence}\" must come at the beginning of the string");
                                            }

                                            skipSpaces = true;
                                            stringBuilder.Append(StringFormatCharacter);
                                            stringBuilder.Append(escapeSequence == "proper" ? (char)StringFormatTypes.Proper : (char)StringFormatTypes.Improper);
                                            break;

                                        case "ref":
                                            currentInterpolationType = StringFormatTypes.Ref; break;

                                        case "The":
                                            skipSpaces = true;
                                            currentInterpolationType = StringFormatTypes.UpperDefiniteArticle;
                                            break;
                                        case "the":
                                            skipSpaces = true;
                                            currentInterpolationType = StringFormatTypes.LowerDefiniteArticle;
                                            break;

                                        case "A":
                                        case "An":
                                            unimplemented = true;
                                            currentInterpolationType = StringFormatTypes.UpperIndefiniteArticle;
                                            break;
                                        case "a":
                                        case "an":
                                            unimplemented = true;
                                            currentInterpolationType = StringFormatTypes.LowerIndefiniteArticle;
                                            break;

                                        case "He":
                                        case "She":
                                            unimplemented = true;
                                            stringBuilder.Append(StringFormatCharacter);
                                            stringBuilder.Append((char)StringFormatTypes.UpperSubjectPronoun);
                                            break;
                                        case "he":
                                        case "she":
                                            unimplemented = true;
                                            stringBuilder.Append(StringFormatCharacter);
                                            stringBuilder.Append((char)StringFormatTypes.LowerSubjectPronoun);
                                            break;

                                        case "His":
                                            unimplemented = true;
                                            stringBuilder.Append(StringFormatCharacter);
                                            stringBuilder.Append((char)StringFormatTypes.UpperPossessiveAdjective);
                                            break;
                                        case "his":
                                            unimplemented = true;
                                            stringBuilder.Append(StringFormatCharacter);
                                            stringBuilder.Append((char)StringFormatTypes.LowerPossessiveAdjective);
                                            break;

                                        case "him":
                                            unimplemented = true;
                                            stringBuilder.Append(StringFormatCharacter);
                                            stringBuilder.Append((char)StringFormatTypes.ObjectPronoun);
                                            break;

                                        case "himself":
                                        case "herself":
                                            unimplemented = true;
                                            stringBuilder.Append(StringFormatCharacter);
                                            stringBuilder.Append((char)StringFormatTypes.ReflexivePronoun);
                                            break;

                                        case "Hers":
                                            unimplemented = true;
                                            stringBuilder.Append(StringFormatCharacter);
                                            stringBuilder.Append((char)StringFormatTypes.UpperPossessivePronoun);
                                            break;
                                        case "hers":
                                            unimplemented = true;
                                            stringBuilder.Append(StringFormatCharacter);
                                            stringBuilder.Append((char)StringFormatTypes.LowerPossessivePronoun);
                                            break;

                                        default:
                                            if (escapeSequence.StartsWith("n")) {
                                                stringBuilder.Append('\n');
                                                stringBuilder.Append(escapeSequence.Skip(1).ToArray());
                                            } else if (escapeSequence.StartsWith("t")) {
                                                stringBuilder.Append('\t');
                                                stringBuilder.Append(escapeSequence.Skip(1).ToArray());
                                            } else if (!DMLexer.ValidEscapeSequences.Contains(escapeSequence)) {
                                                Error($"Invalid escape sequence \"\\{escapeSequence}\"");
                                            }

                                            break;
                                    }

                                    if (unimplemented) {
                                        DMCompiler.UnimplementedWarning(constantToken.Location, $"Unimplemented escape sequence \"{escapeSequence}\"");
                                    }

                                    if (skipSpaces) {
                                        // Note that some macros in BYOND require a single/zero space between them and the []
                                        // This doesn't replicate that
                                        while (i < tokenValue.Length - 1 && tokenValue[i + 1] == ' ') i++;
                                    }

                                } else
                                {
                                    escapeSequence += c;
                                    switch (escapeSequence)
                                    {
                                        case "[":
                                        case "]":
                                        case "<":
                                        case ">":
                                        case "\"":
                                        case "'":
                                        case "\\":
                                        case " ":
                                        case ".":
                                            stringBuilder.Append(escapeSequence);
                                            break;
                                        default: //Unimplemented escape sequence
                                            Error("Invalid escape sequence \"\\" + escapeSequence + "\"");
                                            break;
                                    }
                                }

                                break;
                            }
                            default:
                            {
                                if (bracketNesting == 0) {
                                    stringBuilder.Append(c);
                                }

                                break;
                            }
                        }
                    }

                    if (bracketNesting > 0) Error("Expected ']'");

                    string stringValue = stringBuilder.ToString();
                    if (interpolationValues is null) {
                        return new DMASTConstantString(constantToken.Location, stringValue);
                    } else {
                        return new DMASTStringFormat(constantToken.Location, stringValue, interpolationValues.ToArray());
                    }
                }
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

        private DMASTExpression ParseDereference(DMASTExpression expression, bool allowCalls = true) {
            if (expression != null) {
                while (true) {
                    Token token = Current();

                    if (Check(DereferenceTypes)) {
                        bool invalidDeref = (expression is DMASTExpressionConstant && token.Type == TokenType.DM_Colon);
                        DMASTIdentifier property = null;
                        if (!invalidDeref) {
                            property = Identifier();
                            if (property == null) {
                                if (token.Type == TokenType.DM_Colon) {
                                    invalidDeref = true;
                                } else {
                                    Error("Expected an identifier to dereference");
                                }
                            }
                        }

                        if (invalidDeref) {
                            //Not a valid dereference, but could still be a part of a ternary, so abort
                            ReuseToken(token);
                            break;
                        }

                        (DereferenceType type, bool conditional) = token.Type switch {
                            TokenType.DM_Period => (DereferenceType.Direct, false),
                            TokenType.DM_QuestionPeriod => (DereferenceType.Direct, true),
                            TokenType.DM_QuestionColon => (DereferenceType.Search, true),
                            TokenType.DM_Colon => (DereferenceType.Search, false),
                            _ => throw new InvalidOperationException($"Invalid dereference token {token}")
                        };

                        if (expression is DMASTIdentifier ident && ident.Identifier == "global" && conditional == false) { // global.x
                            expression = new DMASTGlobalIdentifier(expression.Location, property.Identifier);
                        } else {
                            expression = new DMASTDereference(expression.Location, expression, property.Identifier, type, conditional);
                        }
                    } else {
                        break;
                    }
                }

                if (allowCalls) {
                    DMASTExpression procCall = ParseProcCall(expression);

                    if (procCall != expression) { //Successfully parsed a proc call
                        expression = procCall;
                        expression = ParseDereference(expression);
                    }
                }

                Whitespace();
                Token indexToken = Current();
                if (Check(TokenType.DM_LeftBracket) || Check(TokenType.DM_QuestionLeftBracket)) {
                    bool conditional = indexToken.Type == TokenType.DM_QuestionLeftBracket;

                    Whitespace();
                    DMASTExpression index = Expression();
                    ConsumeRightBracket();

                    expression = new DMASTListIndex(expression.Location, expression, index, conditional);
                    expression = ParseDereference(expression);
                    Whitespace();
                }
            }

            return expression;
        }

        private DMASTExpression ParseProcCall(DMASTExpression expression) {
            if (expression is not (DMASTCallable or DMASTIdentifier or DMASTDereference or DMASTGlobalIdentifier)) return expression;

            Whitespace();

            DMASTIdentifier identifier = expression as DMASTIdentifier;

            if (identifier?.Identifier == "pick") {
                DMASTPick.PickValue[] pickValues = PickArguments();

                if (pickValues != null) {
                    return new DMASTPick(identifier.Location, pickValues);
                }
            }

            DMASTCallParameter[] callParameters = ProcCall();
            if (callParameters != null) {
                if (expression is DMASTGlobalIdentifier gid) {
                    var globalProc = new DMASTCallableGlobalProc(expression.Location, gid.Identifier);
                    return new DMASTProcCall(gid.Location, globalProc, callParameters);
                }
                else if (expression is DMASTDereference deref) {
                    DMASTDereferenceProc derefProc = new DMASTDereferenceProc(deref.Location, deref.Expression, deref.Property, deref.Type, deref.Conditional);
                    return new DMASTProcCall(expression.Location, derefProc, callParameters);
                }
                else if (expression is DMASTCallable callable) {
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
                        DMValueType? types = AsTypes();
                        Whitespace();
                        DMASTExpression list = null;

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
                    case "issaved": {
                        if (callParameters.Length != 1) Error("issaved() requires 1 argument");

                        return new DMASTIsSaved(identifier.Location, callParameters[0].Value);
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

                            DMASTExpression container = null;
                            if (Check(TokenType.DM_In)) {
                                Whitespace();

                                container = Expression();
                                if (container == null) Error("Expected a container for locate()");
                            }

                            DMASTExpression type = null;
                            if (callParameters.Length == 2) {
                                type = callParameters[0].Value;
                                container = callParameters[1].Value;
                            } else if (callParameters.Length == 1) {
                                type = callParameters[0].Value;
                            }

                            return new DMASTLocate(identifier.Location, type, container);
                        }
                    }
                    default: return new DMASTProcCall(identifier.Location, new DMASTCallableProcIdentifier(identifier.Location, identifier.Identifier), callParameters);
                }
            }

            return expression;
        }

        private DMValueType? AsTypes() {
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

                    Consume(new TokenType[] { TokenType.DM_Identifier, TokenType.DM_Null }, "Expected value type");
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
