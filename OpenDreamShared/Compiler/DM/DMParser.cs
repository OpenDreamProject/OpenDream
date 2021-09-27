using System;
using System.Collections.Generic;
using OpenDreamShared.Dream;
using DereferenceType = OpenDreamShared.Compiler.DM.DMASTDereference.DereferenceType;
using OpenDreamShared.Dream.Procs;
using System.Text;
using OpenDreamShared.Compiler.DMPreprocessor;

namespace OpenDreamShared.Compiler.DM {
    public partial class DMParser : Parser<Token> {
        public static char StringFormatCharacter = (char)0xFF;

        private DreamPath _currentPath = DreamPath.Root;

        public DMParser(DMLexer lexer) : base(lexer) { }

        public DMASTFile File() {
            List<DMASTStatement> statements = new();

            while (Current().Type != TokenType.EndOfFile) {
                try {
                    List<DMASTStatement> blockInner = BlockInner();

                    if (blockInner != null) statements.AddRange(blockInner);
                } catch (CompileErrorException) { }

                if (Current().Type != TokenType.EndOfFile) {
                    Warning("Error recovery had to skip to the next top-level statement");
                    LocateNextTopLevel();
                }
            }

            Newline();
            Consume(TokenType.EndOfFile, "Expected EOF");
            return new DMASTFile(new DMASTBlockInner(statements.ToArray()));
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

        public DMASTStatement Statement(bool requireDelimiter = true) {
            DMASTPath path = Path();

            if (path != null) {
                DreamPath oldPath = _currentPath;
                Whitespace();
                _currentPath = _currentPath.Combine(path.Path);

                try {
                    DMASTStatement statement = null;

                    //Proc definition
                    if (Check(TokenType.DM_LeftParenthesis)) {
                        BracketWhitespace();
                        DMASTDefinitionParameter[] parameters = DefinitionParameters();
                        BracketWhitespace();
                        ConsumeRightParenthesis();
                        Whitespace();

                        DMASTProcBlockInner procBlock = ProcBlock();
                        if (procBlock == null) {
                            DMASTProcStatement procStatement = ProcStatement();

                            if (procStatement != null) {
                                procBlock = new DMASTProcBlockInner(new DMASTProcStatement[] { procStatement });
                            }
                        }

                        statement = new DMASTProcDefinition(_currentPath, parameters, procBlock);
                    }

                    //Object definition
                    if (statement == null) {
                        DMASTBlockInner block = Block();

                        if (block != null) {
                            statement = new DMASTObjectDefinition(_currentPath, block);
                        }
                    }

                    //Var definition(s)
                    if (statement == null && _currentPath.FindElement("var") != -1) {
                        DreamPath varPath = _currentPath;
                        List<DMASTObjectVarDefinition> varDefinitions = new();

                        while (true) {
                            Whitespace();

                            DMASTExpression value = null;
                            if (Check(TokenType.DM_LeftBracket)) //TODO: Multidimensional lists
                            {
                                //Type information
                                if (!varPath.IsDescendantOf(DreamPath.List)) {
                                    varPath = DreamPath.List.AddToPath(varPath.PathString);
                                }

                                DMASTExpression size = Expression();
                                ConsumeRightBracket();
                                Whitespace();

                                if (size is not null) {
                                    value = new DMASTNewPath(new DMASTPath(DreamPath.List),
                                        new[] { new DMASTCallParameter(size) });
                                }
                            }
                            if (Check(TokenType.DM_Equals)) {
                                Whitespace();
                                value = Expression();
                                if (value == null) Error("Expected an expression");
                            }

                            if (value == null) value = new DMASTConstantNull();

                            AsTypes();

                            varDefinitions.Add(new DMASTObjectVarDefinition(varPath, value));
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
                            statement = new DMASTMultipleObjectVarDefinitions(varDefinitions.ToArray());
                        }
                    }

                    //Var override
                    if (statement == null && Check(TokenType.DM_Equals)) {
                        Whitespace();
                        DMASTExpression value = Expression();
                        if (value == null) Error("Expected an expression");

                        statement = new DMASTObjectVarOverride(_currentPath, value);
                    }

                    //Empty object definition
                    if (statement == null) {
                        statement = new DMASTObjectDefinition(_currentPath, null);
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
                pathType = DreamPath.PathType.Absolute;
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

                return new DMASTPath(new DreamPath(pathType, pathElements.ToArray()));
            } else if (hasPathTypeToken) {
                if (expression) ReuseToken(firstToken);

                return null;
            }

            return null;
        }

        public string PathElement() {
            TokenType[] validPathElementTokens = {
                TokenType.DM_Identifier,
                TokenType.DM_Var,
                TokenType.DM_Proc,
                TokenType.DM_NewList,
                TokenType.DM_Step
            };

            Token elementToken = Current();
            if (Check(validPathElementTokens)) {
                return elementToken.Text;
            } else {
                return null;
            }
        }

        public DMASTCallable Callable() {
            if (Check(TokenType.DM_SuperProc)) return new DMASTCallableSuper();
            if (Check(TokenType.DM_Period)) return new DMASTCallableSelf();

            return null;
        }

        public DMASTIdentifier Identifier() {
            Token token = Current();

            if (Check(new TokenType[] { TokenType.DM_Identifier, TokenType.DM_Step })) {
                return new DMASTIdentifier(token.Text);
            }

            return null;
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
            if (Check(TokenType.DM_LeftCurlyBracket)) {
                Whitespace();
                Newline();
                bool isIndented = Check(TokenType.DM_Indent);
                List<DMASTStatement> blockInner = BlockInner();
                if (isIndented) Check(TokenType.DM_Dedent);
                Newline();
                Consume(TokenType.DM_RightCurlyBracket, "Expected '}'");

                return new DMASTBlockInner(blockInner.ToArray());
            }

            return null;
        }

        public DMASTBlockInner IndentedBlock() {
            if (Check(TokenType.DM_Indent)) {
                List<DMASTStatement> blockInner = BlockInner();

                if (blockInner != null) {
                    Newline();
                    Consume(TokenType.DM_Dedent, "Expected dedent");

                    return new DMASTBlockInner(blockInner.ToArray());
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
            if (Check(TokenType.DM_LeftCurlyBracket)) {
                DMASTProcBlockInner block;

                Whitespace();
                Newline();
                if (Current().Type == TokenType.DM_Indent) {
                    block = IndentedProcBlock();
                    Newline();
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

                    block = new DMASTProcBlockInner(statements.ToArray());
                }

                return block;
            }

            return null;
        }

        public DMASTProcBlockInner IndentedProcBlock() {
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

                return new DMASTProcBlockInner(statements.ToArray());
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
                    } else {
                        if (procStatements.Count == 0) return null;
                    }
                } catch (CompileErrorException) {
                    LocateNextStatement();

                    //LocateNextStatement() may have landed us on another indented/braced block
                    DMASTProcBlockInner blockInner = ProcBlock();
                    if (blockInner != null) procStatements.AddRange(blockInner.Statements);
                }
            } while (Delimiter() || statement is DMASTProcStatementLabel);
            Whitespace();

            return procStatements;
        }

        public DMASTProcStatement ProcStatement() {
            DMASTExpression expression = Expression();

            if (expression != null) {
                if (expression is DMASTIdentifier) {
                    Check(TokenType.DM_Colon);

                    return new DMASTProcStatementLabel(((DMASTIdentifier)expression).Identifier);
                } else if (expression is DMASTLeftShift) {
                    DMASTLeftShift leftShift = (DMASTLeftShift)expression;
                    DMASTProcCall procCall = leftShift.B as DMASTProcCall;

                    if (procCall != null && procCall.Callable is DMASTCallableProcIdentifier identifier) {
                        if (identifier.Identifier == "browse") {
                            if (procCall.Parameters.Length != 1 && procCall.Parameters.Length != 2) Error("browse() requires 1 or 2 parameters");

                            DMASTExpression body = procCall.Parameters[0].Value;
                            DMASTExpression options = (procCall.Parameters.Length == 2) ? procCall.Parameters[1].Value : new DMASTConstantNull();
                            return new DMASTProcStatementBrowse(leftShift.A, body, options);
                        } else if (identifier.Identifier == "browse_rsc") {
                            if (procCall.Parameters.Length != 1 && procCall.Parameters.Length != 2) Error("browse_rsc() requires 1 or 2 parameters");

                            DMASTExpression file = procCall.Parameters[0].Value;
                            DMASTExpression filepath = (procCall.Parameters.Length == 2) ? procCall.Parameters[1].Value : new DMASTConstantNull();
                            return new DMASTProcStatementBrowseResource(leftShift.A, file, filepath);
                        } else if (identifier.Identifier == "output") {
                            if (procCall.Parameters.Length != 2) Error("output() requires 2 parameters");

                            DMASTExpression msg = procCall.Parameters[0].Value;
                            DMASTExpression control = procCall.Parameters[1].Value;
                            return new DMASTProcStatementOutputControl(leftShift.A, msg, control);
                        }
                    }
                }

                return new DMASTProcStatementExpression(expression);
            } else {
                DMASTProcStatement procStatement = ProcVarDeclaration();
                if (procStatement == null) procStatement = Return();
                if (procStatement == null) procStatement = Break();
                if (procStatement == null) procStatement = Continue();
                if (procStatement == null) procStatement = Goto();
                if (procStatement == null) procStatement = Del();
                if (procStatement == null) procStatement = Set();
                if (procStatement == null) procStatement = Spawn();
                if (procStatement == null) procStatement = If();
                if (procStatement == null) procStatement = For();
                if (procStatement == null) procStatement = While();
                if (procStatement == null) procStatement = DoWhile();
                if (procStatement == null) procStatement = Switch();
                if (procStatement == null) procStatement = TryCatch();

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
                DMASTPath varPath = Path();
                if (varPath == null) Error("Expected a variable name");

                List<DMASTProcStatementVarDeclaration> varDeclarations = new();
                while (true) {
                    DMASTExpression value = null;
                    Whitespace();

                    //TODO: Multidimensional lists
                    if (Check(TokenType.DM_LeftBracket)) {
                        //Type information
                        if (varPath is not null && !varPath.Path.IsDescendantOf(DreamPath.List)) {
                            varPath = new DMASTPath(new DreamPath(DreamPath.List.PathString + "/" + varPath.Path.PathString));
                        }

                        Whitespace();
                        DMASTExpression size = Expression();
                        ConsumeRightBracket();
                        Whitespace();

                        if (size is not null) {
                            value = new DMASTNewPath(new DMASTPath(DreamPath.List),
                                new[] { new DMASTCallParameter(size) });
                        }
                    }

                    if (Check(TokenType.DM_Equals)) {
                        if (value != null) Error("List doubly initialized");

                        Whitespace();
                        value = Expression();

                        if (value == null) Error("Expected an expression");
                    }

                    AsTypes();

                    varDeclarations.Add(new DMASTProcStatementVarDeclaration(varPath, value));
                    if (allowMultiple && Check(TokenType.DM_Comma)) {
                        Whitespace();
                        varPath = Path();
                        if (varPath == null) Error("Expected a var declaration");
                    } else {
                        break;
                    }
                }

                if (varDeclarations.Count > 1) {
                    return new DMASTProcStatementMultipleVarDeclarations(varDeclarations.ToArray());
                } else {
                    return varDeclarations[0];
                }
            } else if (wasSlash) {
                ReuseToken(firstToken);
            }

            return null;
        }

        public DMASTProcStatementReturn Return() {
            if (Check(TokenType.DM_Return)) {
                Whitespace();
                DMASTExpression value = Expression();

                return new DMASTProcStatementReturn(value);
            } else {
                return null;
            }
        }

        public DMASTProcStatementBreak Break() {
            if (Check(TokenType.DM_Break)) {
                return new DMASTProcStatementBreak();
            } else {
                return null;
            }
        }

        public DMASTProcStatementContinue Continue() {
            if (Check(TokenType.DM_Continue)) {
                return new DMASTProcStatementContinue();
            } else {
                return null;
            }
        }

        public DMASTProcStatementGoto Goto() {
            if (Check(TokenType.DM_Goto)) {
                Whitespace();
                DMASTIdentifier label = Identifier();

                return new DMASTProcStatementGoto(label);
            } else {
                return null;
            }
        }

        public DMASTProcStatementDel Del() {
            if (Check(TokenType.DM_Del)) {
                Whitespace();
                bool hasParenthesis = Check(TokenType.DM_LeftParenthesis);
                Whitespace();
                DMASTExpression value = Expression();
                if (value == null) Error("Expected value to delete");
                if (hasParenthesis) ConsumeRightParenthesis();

                return new DMASTProcStatementDel(value);
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

                    return new DMASTProcStatementSet(attributeToken.Text, value);
                } else {
                    Error("Expected property name");
                }
            }

            return null;
        }

        public DMASTProcStatementSpawn Spawn() {
            if (Check(TokenType.DM_Spawn)) {
                Whitespace();
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                Whitespace();

                DMASTExpression delay;
                if (Check(TokenType.DM_RightParenthesis)) {
                    //No parameters, default to zero
                    delay = new DMASTConstantInteger(0);
                } else {
                    delay = Expression();

                    if (delay == null) Error("Expected an expression");
                    ConsumeRightParenthesis();
                }

                Whitespace();
                Newline();

                DMASTProcBlockInner body = ProcBlock();
                if (body == null) {
                    DMASTProcStatement statement = ProcStatement();

                    if (statement == null) Error("Expected body or statement");
                    body = new DMASTProcBlockInner(new DMASTProcStatement[] { statement });
                }

                return new DMASTProcStatementSpawn(delay, body);
            } else {
                return null;
            }
        }

        public DMASTProcStatementIf If() {
            if (Check(TokenType.DM_If)) {
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
                    body = new DMASTProcBlockInner(new DMASTProcStatement[] { procStatement });
                } else {
                    body = ProcBlock();
                }

                if (body == null) body = new DMASTProcBlockInner(new DMASTProcStatement[0]);
                Token afterIfBody = Current();
                bool newLineAfterIf = Newline();
                if (newLineAfterIf) Whitespace();
                if (Check(TokenType.DM_Else)) {
                    Whitespace();
                    Check(TokenType.DM_Colon);
                    Whitespace();
                    procStatement = ProcStatement();

                    if (procStatement != null) {
                        elseBody = new DMASTProcBlockInner(new DMASTProcStatement[] { procStatement });
                    } else {
                        elseBody = ProcBlock();
                    }

                    if (elseBody == null) elseBody = new DMASTProcBlockInner(new DMASTProcStatement[0]);
                } else if (newLineAfterIf) {
                    ReuseToken(afterIfBody);
                }

                return new DMASTProcStatementIf(condition, body, elseBody);
            } else {
                return null;
            }
        }

        public DMASTProcStatement For() {
            if (Check(TokenType.DM_For)) {
                Whitespace();
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                Whitespace();
                DMASTProcStatement initializer = null;
                DMASTIdentifier variable;

                DMASTProcStatementVarDeclaration variableDeclaration = ProcVarDeclaration(allowMultiple: false) as DMASTProcStatementVarDeclaration;
                if (variableDeclaration != null) {
                    initializer = variableDeclaration;
                    variable = new DMASTIdentifier(variableDeclaration.Name);
                } else {
                    variable = Identifier();
                    if (variable != null) {
                        Whitespace();
                        if (Check(TokenType.DM_Equals)) {
                            Whitespace();
                            DMASTExpression value = Expression();
                            if (value == null) Error("Expected an expression");

                            initializer = new DMASTProcStatementExpression(new DMASTAssign(variable, value));
                        }
                    }
                }

                Whitespace();
                AsTypes(); //TODO: Correctly handle
                Whitespace();

                if (Check(TokenType.DM_In)) {
                    Whitespace();
                    DMASTExpression enumerateValue = Expression();
                    DMASTExpression toValue = null;
                    DMASTExpression step = new DMASTConstantInteger(1);

                    if (Check(TokenType.DM_To)) {
                        Whitespace();

                        toValue = Expression();
                        if (toValue == null) Error("Expected an end to the range");

                        if (Check(TokenType.DM_Step)) {
                            Whitespace();

                            step = Expression();
                            if (step == null) Error("Expected a step value");
                        }
                    }

                    ConsumeRightParenthesis();
                    Whitespace();
                    Newline();

                    DMASTProcBlockInner body = ProcBlock();
                    if (body == null) {
                        DMASTProcStatement statement = ProcStatement();

                        //Loops without a body are valid DM
                        if (statement == null) statement = new DMASTProcStatementContinue();
                        body = new DMASTProcBlockInner(new DMASTProcStatement[] { statement });
                    }

                    if (toValue == null) {
                        return new DMASTProcStatementForList(initializer, variable, enumerateValue, body);
                    } else {
                        return new DMASTProcStatementForRange(initializer, variable, enumerateValue, toValue, step, body);
                    }
                } else if (Check(new TokenType[] { TokenType.DM_Comma, TokenType.DM_Semicolon })) {
                    Whitespace();
                    DMASTExpression comparator = Expression();
                    DMASTExpression incrementor = null;
                    if (Check(new[] { TokenType.DM_Comma, TokenType.DM_Semicolon })) {
                        Whitespace();
                        incrementor = Expression();
                    }
                    Whitespace();
                    ConsumeRightParenthesis();
                    Whitespace();
                    Newline();

                    return new DMASTProcStatementForStandard(initializer, comparator, incrementor, GetForBody());
                } else if (variableDeclaration != null) {
                    DMASTExpression rangeBegin = variableDeclaration.Value;
                    Whitespace();
                    if (variableDeclaration.Value is not null) {
                        Consume(TokenType.DM_To, "Expected 'to'");
                    }

                    Whitespace();
                    DMASTExpression rangeEnd = Expression();
                    if (variableDeclaration.Value is not null && rangeEnd == null) Error("Expected an expression");
                    DMASTExpression step = new DMASTConstantInteger(1);

                    var defaultStep = true;

                    if (Check(TokenType.DM_Step)) {
                        Whitespace();

                        step = Expression();
                        if (step == null) Error("Expected a step value");
                        defaultStep = false;
                    }

                    ConsumeRightParenthesis();
                    Whitespace();
                    Newline();

                    //Implicit "in world"
                    if (variableDeclaration.Value is null && rangeEnd is null && defaultStep) {
                        return new DMASTProcStatementForList(initializer, variable, new DMASTIdentifier("world"), GetForBody());
                    }

                    return new DMASTProcStatementForRange(initializer, variable, rangeBegin, rangeEnd, step, GetForBody());
                } else {
                    Error("Expected 'in'");
                }
            }

            return null;

            DMASTProcBlockInner GetForBody() {
                DMASTProcBlockInner body = ProcBlock();
                if (body == null) {
                    DMASTProcStatement statement = ProcStatement();

                    if (statement == null) Error("Expected body or statement");
                    body = new DMASTProcBlockInner(new DMASTProcStatement[] { statement });
                }

                return body;
            }
        }

        public DMASTProcStatementWhile While() {
            if (Check(TokenType.DM_While)) {
                Whitespace();
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                Whitespace();
                DMASTExpression conditional = Expression();
                if (conditional == null) Error("Expected conditional");
                ConsumeRightParenthesis();
                Whitespace();
                DMASTProcBlockInner body = ProcBlock();

                if (body == null) {
                    DMASTProcStatement statement = ProcStatement();

                    //Loops without a body are valid DM
                    if (statement == null) statement = new DMASTProcStatementContinue();

                    body = new DMASTProcBlockInner(new DMASTProcStatement[] { statement });
                }

                return new DMASTProcStatementWhile(conditional, body);
            }

            return null;
        }

        public DMASTProcStatementDoWhile DoWhile() {
            if (Check(TokenType.DM_Do)) {
                Whitespace();
                DMASTProcBlockInner body = ProcBlock();

                if (body == null) {
                    DMASTProcStatement statement = ProcStatement();
                    if (statement == null) Error("Expected statement");

                    body = new DMASTProcBlockInner(new DMASTProcStatement[] { statement });
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

                return new DMASTProcStatementDoWhile(conditional, body);
            }

            return null;
        }

        public DMASTProcStatementSwitch Switch() {
            if (Check(TokenType.DM_Switch)) {
                Whitespace();
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                Whitespace();
                DMASTExpression value = Expression();
                ConsumeRightParenthesis();
                Whitespace();

                DMASTProcStatementSwitch.SwitchCase[] switchCases = SwitchCases();

                if (switchCases == null) Error("Expected switch cases");
                return new DMASTProcStatementSwitch(value, switchCases);
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
                Consume(TokenType.DM_Dedent, "Expected dedent");

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
                        Whitespace();
                        DMASTExpression rangeEnd = Expression();
                        if (rangeEnd == null) Error("Expected an upper limit");

                        expressions.Add(new DMASTSwitchCaseRange(expression, rangeEnd));
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

                    if (statement != null) {
                        body = new DMASTProcBlockInner(new DMASTProcStatement[] { statement });
                    } else {
                        body = new DMASTProcBlockInner(new DMASTProcStatement[0]);
                    }
                }

                return new DMASTProcStatementSwitch.SwitchCaseValues(expressions.ToArray(), body);
            } else if (Check(TokenType.DM_Else)) {
                Whitespace();
                DMASTProcBlockInner body = ProcBlock();

                if (body == null) {
                    DMASTProcStatement statement = ProcStatement();

                    if (statement != null) {
                        body = new DMASTProcBlockInner(new DMASTProcStatement[] { statement });
                    } else {
                        body = new DMASTProcBlockInner(new DMASTProcStatement[0]);
                    }
                }

                return new DMASTProcStatementSwitch.SwitchCaseDefault(body);
            }

            return null;
        }

        public DMASTProcStatementTryCatch TryCatch() {
            if (Check(TokenType.DM_Try)) {
                DMASTProcBlockInner tryBody = ProcBlock();
                if (tryBody == null) {
                    DMASTProcStatement statement = ProcStatement();

                    if (statement == null) Error("Expected body or statement");
                    tryBody = new DMASTProcBlockInner(new DMASTProcStatement[] { statement });
                }

                Warning("Exceptions in 'try/catch' blocks are currently not caught");

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

                    catchBody = new DMASTProcBlockInner(new DMASTProcStatement[] { statement });
                }

                return new DMASTProcStatementTryCatch(tryBody, catchBody, parameter);
            }

            return null;
        }

        public DMASTCallParameter[] ProcCall(bool includeEmptyParameters = true) {
            if (Check(TokenType.DM_LeftParenthesis)) {
                BracketWhitespace();

                DMASTCallParameter[] callParameters = CallParameters(includeEmptyParameters);
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

        public DMASTCallParameter[] CallParameters(bool includeEmpty) {
            List<DMASTCallParameter> parameters = new();
            DMASTCallParameter parameter = CallParameter();

            while (parameter != null) {
                parameters.Add(parameter);

                if (Check(TokenType.DM_Comma)) {
                    BracketWhitespace();
                    parameter = CallParameter();

                    if (parameter == null) {
                        if (includeEmpty) parameter = new DMASTCallParameter(new DMASTConstantNull());
                        else while (Check(TokenType.DM_Comma)) Whitespace();
                    }
                } else {
                    parameter = null;
                }
            }

            if (parameters.Count > 0) {
                return parameters.ToArray();
            } else {
                return null;
            }
        }

        public DMASTCallParameter CallParameter() {
            DMASTExpression expression = Expression();

            if (expression != null) {
                DMASTAssign assign = expression as DMASTAssign;

                if (assign != null) {
                    if (assign.Expression is DMASTConstantString) {
                        return new DMASTCallParameter(assign.Value, ((DMASTConstantString)assign.Expression).Value);
                    } else if (assign.Expression is DMASTIdentifier) {
                        return new DMASTCallParameter(assign.Value, ((DMASTIdentifier)assign.Expression).Identifier);
                    }
                }

                return new DMASTCallParameter(expression);
            }

            return null;
        }

        public DMASTDefinitionParameter[] DefinitionParameters() {
            List<DMASTDefinitionParameter> parameters = new();
            DMASTDefinitionParameter parameter = DefinitionParameter();

            if (parameter != null || Check(TokenType.DM_IndeterminateArgs)) {
                if (parameter != null) parameters.Add(parameter);

                while (Check(TokenType.DM_Comma)) {
                    BracketWhitespace();

                    parameter = DefinitionParameter();
                    if (parameter != null) {
                        parameters.Add(parameter);
                    } else if (!Check(TokenType.DM_IndeterminateArgs)) {
                        Error("Expected parameter definition");
                    }
                }
            }

            return parameters.ToArray();
        }

        public DMASTDefinitionParameter DefinitionParameter() {
            DMASTPath path = Path();

            if (path != null) {
                Whitespace();
                if (Check(TokenType.DM_LeftBracket)) {
                    Whitespace();
                    DMASTExpression expression = Expression();
                    if (expression != null && expression is not DMASTExpressionConstant) Error("Expected a constant expression");
                    Whitespace();
                    ConsumeRightBracket();
                }

                DMASTExpression value = null;
                DMValueType type;
                DMASTExpression possibleValues = null;

                if (Check(TokenType.DM_Equals)) {
                    Whitespace();
                    value = Expression();
                }

                type = AsTypes();
                Whitespace();

                if (Check(TokenType.DM_In)) {
                    Whitespace();
                    possibleValues = Expression();
                }

                return new DMASTDefinitionParameter(path, value, type, possibleValues);
            }

            return null;
        }

        public DMASTExpression Expression() {
            return ExpressionAssign();
        }

        public DMASTExpression ExpressionAssign() {
            DMASTExpression expression = ExpressionTernary();

            if (expression != null) {
                Token token = Current();
                ReadOnlySpan<TokenType> assignTypes = new TokenType[] {
                    TokenType.DM_Equals,
                    TokenType.DM_PlusEquals,
                    TokenType.DM_MinusEquals,
                    TokenType.DM_BarEquals,
                    TokenType.DM_AndEquals,
                    TokenType.DM_StarEquals,
                    TokenType.DM_SlashEquals,
                    TokenType.DM_LeftShiftEquals,
                    TokenType.DM_RightShiftEquals,
                    TokenType.DM_XorEquals,
                    TokenType.DM_ModulusEquals
                };

                if (Check(assignTypes)) {
                    Whitespace();
                    DMASTExpression value = ExpressionAssign();

                    if (value != null) {
                        switch (token.Type) {
                            case TokenType.DM_Equals: return new DMASTAssign(expression, value);
                            case TokenType.DM_PlusEquals: return new DMASTAppend(expression, value);
                            case TokenType.DM_MinusEquals: return new DMASTRemove(expression, value);
                            case TokenType.DM_BarEquals: return new DMASTCombine(expression, value);
                            case TokenType.DM_AndEquals: return new DMASTMask(expression, value);
                            case TokenType.DM_StarEquals: return new DMASTMultiplyAssign(expression, value);
                            case TokenType.DM_SlashEquals: return new DMASTDivideAssign(expression, value);
                            case TokenType.DM_LeftShiftEquals: return new DMASTLeftShiftAssign(expression, value);
                            case TokenType.DM_RightShiftEquals: return new DMASTRightShiftAssign(expression, value);
                            case TokenType.DM_XorEquals: return new DMASTXorAssign(expression, value);
                            case TokenType.DM_ModulusEquals: return new DMASTModulusAssign(expression, value);
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
                                c = new DMASTIdentifier(deref.Property);
                            } else {
                                c = new DMASTDereference(new DMASTIdentifier(deref.Property), ((DMASTIdentifier)c).Identifier, type, conditional);
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

                return new DMASTTernary(a, b, c);
            }

            return a;
        }

        public DMASTExpression ExpressionOr() {
            DMASTExpression a = ExpressionAnd();
            if (a != null) {
                while (Check(TokenType.DM_BarBar)) {
                    Whitespace();
                    DMASTExpression b = ExpressionAnd();
                    if (b == null) Error("Expected a second value");
                    a = new DMASTOr(a, b);
                }
            }
            return a;
        }

        public DMASTExpression ExpressionAnd() {
            DMASTExpression a = ExpressionBinaryOr();
            if (a != null) {
                while (Check(TokenType.DM_AndAnd)) {
                    Whitespace();
                    DMASTExpression b = ExpressionBinaryOr();
                    if (b == null) Error("Expected a second value");
                    a = new DMASTAnd(a, b);
                }
            }
            return a;
        }

        public DMASTExpression ExpressionBinaryOr() {
            DMASTExpression a = ExpressionBinaryXor();
            if (a != null) {
                while (Check(TokenType.DM_Bar)) {
                    Whitespace();
                    DMASTExpression b = ExpressionBinaryXor();
                    if (b == null) Error("Expected an expression");
                    a = new DMASTBinaryOr(a, b);
                }
            }
            return a;
        }

        public DMASTExpression ExpressionBinaryXor() {
            DMASTExpression a = ExpressionBinaryAnd();
            if (a != null) {
                while (Check(TokenType.DM_Xor)) {
                    Whitespace();
                    DMASTExpression b = ExpressionBinaryAnd();
                    if (b == null) Error("Expected an expression");
                    a = new DMASTBinaryXor(a, b);
                }
            }
            return a;
        }

        public DMASTExpression ExpressionBinaryAnd() {
            DMASTExpression a = ExpressionComparison();
            if (a != null) {
                while (Check(TokenType.DM_And)) {
                    Whitespace();
                    DMASTExpression b = ExpressionComparison();

                    if (b == null) Error("Expected an expression");
                    a = new DMASTBinaryAnd(a, b);
                }
            }
            return a;
        }

        public DMASTExpression ExpressionComparison() {
            DMASTExpression a = ExpressionBitShift();

            if (a != null) {
                ReadOnlySpan<TokenType> types = new TokenType[] {
                    TokenType.DM_EqualsEquals,
                    TokenType.DM_ExclamationEquals,
                    TokenType.DM_TildeEquals,
                    TokenType.DM_TildeExclamation };

                Token token = Current();
                while (Check(types)) {
                    Whitespace();
                    DMASTExpression b = ExpressionBitShift();
                    if (b == null) Error("Expected an expression to compare to");
                    switch (token.Type) {
                        case TokenType.DM_EqualsEquals: a = new DMASTEqual(a, b); break;
                        case TokenType.DM_ExclamationEquals: a = new DMASTNotEqual(a, b); break;
                        case TokenType.DM_TildeEquals: a = new DMASTEquivalent(a, b); break;
                        case TokenType.DM_TildeExclamation: a = new DMASTNotEquivalent(a, b); break;
                    }
                    token = Current();
                }
            }

            return a;
        }

        public DMASTExpression ExpressionBitShift() {
            DMASTExpression a = ExpressionComparisonLtGt();

            if (a != null) {
                ReadOnlySpan<TokenType> types = new TokenType[] {
                    TokenType.DM_LeftShift,
                    TokenType.DM_RightShift
                };

                Token token = Current();
                while (Check(types)) {
                    Whitespace();
                    DMASTExpression b = ExpressionComparisonLtGt();
                    if (b == null) Error("Expected an expression");

                    switch (token.Type) {
                        case TokenType.DM_LeftShift: a = new DMASTLeftShift(a, b); break;
                        case TokenType.DM_RightShift: a = new DMASTRightShift(a, b); break;
                    }
                    token = Current();
                }
            }

            return a;
        }

        public DMASTExpression ExpressionComparisonLtGt() {
            DMASTExpression a = ExpressionAdditionSubtraction();

            if (a != null) {
                ReadOnlySpan<TokenType> types = new TokenType[] {
                    TokenType.DM_LessThan,
                    TokenType.DM_LessThanEquals,
                    TokenType.DM_GreaterThan,
                    TokenType.DM_GreaterThanEquals
                };

                Token token = Current();
                while (Check(types)) {
                    Whitespace();
                    DMASTExpression b = ExpressionAdditionSubtraction();
                    if (b == null) Error("Expected an expression");

                    switch (token.Type) {
                        case TokenType.DM_LessThan: a = new DMASTLessThan(a, b); break;
                        case TokenType.DM_LessThanEquals: a = new DMASTLessThanOrEqual(a, b); break;
                        case TokenType.DM_GreaterThan: a = new DMASTGreaterThan(a, b); break;
                        case TokenType.DM_GreaterThanEquals: a = new DMASTGreaterThanOrEqual(a, b); break;
                    }
                    token = Current();
                }
            }

            return a;
        }

        public DMASTExpression ExpressionAdditionSubtraction() {
            DMASTExpression a = ExpressionMultiplicationDivisionModulus();

            if (a != null) {
                ReadOnlySpan<TokenType> types = new TokenType[] {
                    TokenType.DM_Plus,
                    TokenType.DM_Minus,
                };

                Token token = Current();
                while (Check(types)) {
                    Whitespace();
                    DMASTExpression b = ExpressionMultiplicationDivisionModulus();
                    if (b == null) Error("Expected an expression");

                    switch (token.Type) {
                        case TokenType.DM_Plus: a = new DMASTAdd(a, b); break;
                        case TokenType.DM_Minus: a = new DMASTSubtract(a, b); break;
                    }

                    token = Current();
                }
            }

            return a;
        }

        public DMASTExpression ExpressionMultiplicationDivisionModulus() {
            DMASTExpression a = ExpressionPower();

            if (a != null) {
                ReadOnlySpan<TokenType> types = new[] {
                    TokenType.DM_Star,
                    TokenType.DM_Slash,
                    TokenType.DM_Modulus
                };

                Token token = Current();
                while (Check(types)) {
                    Whitespace();
                    DMASTExpression b = ExpressionPower();
                    if (b == null) Error("Expected an expression");

                    switch (token.Type) {
                        case TokenType.DM_Star: a = new DMASTMultiply(a, b); break;
                        case TokenType.DM_Slash: a = new DMASTDivide(a, b); break;
                        case TokenType.DM_Modulus: a = new DMASTModulus(a, b); break;
                    }

                    token = Current();
                }
            }

            return a;
        }

        public DMASTExpression ExpressionPower() {
            DMASTExpression a = ExpressionIn();

            if (a != null) {
                while (Check(TokenType.DM_StarStar)) {
                    Whitespace();
                    DMASTExpression b = ExpressionIn();
                    if (b == null) Error("Expected an expression");
                    a = new DMASTPower(a, b);
                }
            }

            return a;
        }

        public DMASTExpression ExpressionIn() {
            DMASTExpression value = ExpressionUnary();

            if (value != null && Check(TokenType.DM_In)) {
                Whitespace();
                DMASTExpression list = ExpressionIn();

                return new DMASTExpressionIn(value, list);
            }

            return value;
        }

        public DMASTExpression ExpressionUnary() {
            if (Check(TokenType.DM_Exclamation)) {
                Whitespace();
                DMASTExpression expression = ExpressionUnary();
                if (expression == null) Error("Expected an expression");

                return new DMASTNot(expression);
            } else if (Check(TokenType.DM_Tilde)) {
                Whitespace();
                DMASTExpression expression = ExpressionUnary();
                if (expression == null) Error("Expected an expression");

                return new DMASTBinaryNot(expression);
            } else if (Check(TokenType.DM_PlusPlus)) {
                Whitespace();
                DMASTExpression expression = ExpressionSign();
                if (expression == null) Error("Expected an expression");

                return new DMASTPreIncrement(expression);
            } else if (Check(TokenType.DM_MinusMinus)) {
                Whitespace();
                DMASTExpression expression = ExpressionSign();
                if (expression == null) Error("Expected an expression");

                return new DMASTPreDecrement(expression);
            } else {
                DMASTExpression expression = ExpressionSign();

                if (expression != null) {
                    if (Check(TokenType.DM_PlusPlus)) {
                        Whitespace();
                        expression = new DMASTPostIncrement(expression);
                    } else if (Check(TokenType.DM_MinusMinus)) {
                        Whitespace();
                        expression = new DMASTPostDecrement(expression);
                    }
                }

                return expression;
            }
        }

        public DMASTExpression ExpressionSign() {
            Token token = Current();

            if (Check(new TokenType[] { TokenType.DM_Plus, TokenType.DM_Minus })) {
                Whitespace();
                DMASTExpression expression = ExpressionSign();

                if (expression == null) Error("Expected an expression");
                if (token.Type == TokenType.DM_Minus) {
                    if (expression is DMASTConstantInteger) {
                        int value = ((DMASTConstantInteger)expression).Value;

                        return new DMASTConstantInteger(-value);
                    } else if (expression is DMASTConstantFloat) {
                        float value = ((DMASTConstantFloat)expression).Value;

                        return new DMASTConstantFloat(-value);
                    }

                    return new DMASTNegate(expression);
                } else {
                    return expression;
                }
            }

            return ExpressionNew();
        }

        public DMASTExpression ExpressionNew() {
            if (Check(TokenType.DM_New)) {
                Whitespace();
                DMASTExpression type = ExpressionPrimary(allowParentheses: false);
                type = ParseDereference(type, allowCalls: false);
                DMASTCallParameter[] parameters = ProcCall();

                //TODO: These don't need to be separate types
                DMASTExpression newExpression = type switch {
                    DMASTDereference deref => new DMASTNewDereference(deref, parameters),
                    DMASTIdentifier identifier => new DMASTNewIdentifier(identifier, parameters),
                    DMASTConstantPath path => new DMASTNewPath(path.Value, parameters),
                    null => new DMASTNewInferred(parameters),
                    _ => null
                };

                if (newExpression == null) Error("Invalid type");
                newExpression = ParseDereference(newExpression);
                return newExpression;
            }

            return ParseDereference(ExpressionPrimary());
        }

        public DMASTExpression ExpressionPrimary(bool allowParentheses = true) {
            if (allowParentheses && Check(TokenType.DM_LeftParenthesis)) {
                Whitespace();
                DMASTExpression inner = Expression();
                ConsumeRightParenthesis();

                return inner;
            }

            DMASTExpression primary = Constant();

            if (primary == null) {
                DMASTPath path = Path(true);

                if (path != null) {
                    primary = new DMASTConstantPath(path);

                    while (Check(TokenType.DM_Period)) {
                        DMASTPath search = Path();
                        if (search == null) Error("Expected a path for an upward search");

                        primary = new DMASTUpwardPathSearch((DMASTExpressionConstant)primary, search);
                    }
                }
            }

            if (primary == null) {
                primary = Identifier();
            }

            if (primary == null) {
                primary = Callable();

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

                primary = new DMASTCall(callParameters, procParameters);
            }

            if (primary == null && Check(TokenType.DM_NewList)) {
                Whitespace();
                DMASTCallParameter[] values = ProcCall(false);

                primary = new DMASTNewList(values);
            }

            return primary;
        }

        public DMASTExpression Constant() {
            Token constantToken = Current();

            switch (constantToken.Type) {
                case TokenType.DM_Integer: Advance(); return new DMASTConstantInteger((int)constantToken.Value);
                case TokenType.DM_Float: Advance(); return new DMASTConstantFloat((float)constantToken.Value);
                case TokenType.DM_Resource: Advance(); return new DMASTConstantResource((string)constantToken.Value);
                case TokenType.DM_Null: Advance(); return new DMASTConstantNull();
                case TokenType.DM_RawString: Advance(); return new DMASTConstantString((string)constantToken.Value);
                case TokenType.DM_String: {
                    string tokenValue = (string)constantToken.Value;
                    StringBuilder stringBuilder = new StringBuilder();
                    List<DMASTExpression> interpolationValues = new();
                    Advance();

                    int bracketNesting = 0;
                    StringBuilder insideBrackets = new StringBuilder();
                    StringFormatTypes currentInterpolationType = StringFormatTypes.Stringify;
                    for (int i = 0; i < tokenValue.Length; i++) {
                        char c = tokenValue[i];

                        if (bracketNesting > 0) {
                            insideBrackets.Append(c);
                        }

                        if (c == '[') {
                            bracketNesting++;
                        } else if (c == ']' && bracketNesting > 0) {
                            bracketNesting--;

                            if (bracketNesting == 0) { //End of expression
                                insideBrackets.Remove(insideBrackets.Length - 1, 1); //Remove the ending bracket

                                string insideBracketsText = insideBrackets.ToString();
                                if (insideBracketsText != String.Empty) {
                                    DMPreprocessorLexer preprocLexer = new DMPreprocessorLexer(constantToken.SourceFile, insideBracketsText);
                                    List<Token> preprocTokens = new();
                                    Token preprocToken;
                                    do {
                                        preprocToken = preprocLexer.GetNextToken();
                                        preprocToken.SourceFile = constantToken.SourceFile;
                                        preprocToken.Line = constantToken.Line;
                                        preprocToken.Column = constantToken.Column;
                                        preprocTokens.Add(preprocToken);
                                    } while (preprocToken.Type != TokenType.EndOfFile);

                                    DMLexer expressionLexer = new DMLexer(constantToken.SourceFile, preprocTokens);
                                    DMParser expressionParser = new DMParser(expressionLexer);

                                    expressionParser.Whitespace(true);
                                    DMASTExpression expression = expressionParser.Expression();
                                    if (expression == null) Error("Expected an expression");
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
                        } else if (c == '\\' && bracketNesting == 0) {
                            string escapeSequence = String.Empty;

                            do {
                                c = tokenValue[++i];
                                escapeSequence += c;

                                if (escapeSequence == "[" || escapeSequence == "]") {
                                    stringBuilder.Append(escapeSequence);
                                    break;
                                } else if (escapeSequence == "\"" || escapeSequence == "\\" || escapeSequence == "'") {
                                    stringBuilder.Append(escapeSequence);
                                    break;
                                } else if (escapeSequence == "n") {
                                    stringBuilder.Append('\n');
                                    break;
                                } else if (escapeSequence == "t") {
                                    stringBuilder.Append('\t');
                                    break;
                                } else if (escapeSequence == "ref") {
                                    currentInterpolationType = StringFormatTypes.Ref;
                                    break;
                                } else if (DMLexer.ValidEscapeSequences.Contains(escapeSequence)) { //Unimplemented escape sequence
                                    break;
                                }
                            } while (c != ' ' && i < tokenValue.Length - 1);

                            if (!DMLexer.ValidEscapeSequences.Contains(escapeSequence)) {
                                Error("Invalid escape sequence \"\\" + escapeSequence + "\"");
                            }
                        } else if (bracketNesting == 0) {
                            stringBuilder.Append(c);
                        }
                    }

                    if (bracketNesting > 0) Error("Expected ']'");

                    string stringValue = stringBuilder.ToString();
                    if (interpolationValues.Count == 0) {
                        return new DMASTConstantString(stringValue);
                    } else {
                        return new DMASTStringFormat(stringValue, interpolationValues.ToArray());
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
                ReadOnlySpan<TokenType> types = new TokenType[] {
                    TokenType.DM_Whitespace,
                    TokenType.DM_Indent,
                    TokenType.DM_Dedent };

                while (Check(types)) hadWhitespace = true;
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
                ReadOnlySpan<TokenType> dereferenceTokenTypes = new TokenType[] {
                    TokenType.DM_Period,
                    TokenType.DM_Colon,
                    TokenType.DM_QuestionPeriod,
                    TokenType.DM_QuestionColon
                };

                while (true) {
                    Token token = Current();

                    if (Check(dereferenceTokenTypes)) {
                        DMASTIdentifier property = Identifier();
                        if (property == null) {
                            if (token.Type == TokenType.DM_Colon) {
                                //Not a valid dereference, but could still be a part of a ternary, so abort
                                ReuseToken(token);
                                break;
                            } else {
                                Error("Expected an identifier to dereference");
                            }
                        }

                        (DereferenceType type, bool conditional) = token.Type switch {
                            TokenType.DM_Period => (DereferenceType.Direct, false),
                            TokenType.DM_QuestionPeriod => (DereferenceType.Direct, true),
                            TokenType.DM_QuestionColon => (DereferenceType.Search, true),
                            TokenType.DM_Colon => (DereferenceType.Search, false),
                            _ => throw new InvalidOperationException($"Invalid dereference token {token}")
                        };

                        expression = new DMASTDereference(expression, property.Identifier, type, conditional);
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
                if (Check(TokenType.DM_LeftBracket)) {
                    Whitespace();
                    DMASTExpression index = Expression();
                    ConsumeRightBracket();

                    expression = new DMASTListIndex(expression, index);
                    expression = ParseDereference(expression);
                    Whitespace();
                }
            }

            return expression;
        }

        private DMASTExpression ParseProcCall(DMASTExpression expression) {
            if (expression is not (DMASTCallable or DMASTIdentifier or DMASTDereference)) return expression;

            Whitespace();

            DMASTIdentifier identifier = expression as DMASTIdentifier;

            if (identifier?.Identifier == "pick") {
                DMASTPick.PickValue[] pickValues = PickArguments();

                if (pickValues != null) {
                    return new DMASTPick(pickValues);
                }
            }

            DMASTCallParameter[] callParameters = ProcCall();
            if (callParameters != null) {
                if (expression is DMASTDereference deref) {
                    DMASTDereferenceProc derefProc = new DMASTDereferenceProc(deref.Expression, deref.Property, deref.Type, deref.Conditional);

                    return new DMASTProcCall(derefProc, callParameters);
                } else if (expression is DMASTCallable callable) {
                    return new DMASTProcCall(callable, callParameters);
                }

                switch (identifier.Identifier) {
                    case "list": return new DMASTList(callParameters);
                    case "input": {
                        Whitespace();
                        DMValueType types = AsTypes(defaultType: DMValueType.Text);
                        Whitespace();
                        DMASTExpression list = null;

                        if (Check(TokenType.DM_In)) {
                            Whitespace();
                            list = Expression();
                        }

                        return new DMASTInput(callParameters, types, list);
                    }
                    case "initial": {
                        if (callParameters.Length != 1) Error("initial() requires 1 argument");

                        return new DMASTInitial(callParameters[0].Value);
                    }
                    case "issaved": {
                        if (callParameters.Length != 1) Error("issaved() requires 1 argument");

                        return new DMASTIsSaved(callParameters[0].Value);
                    }
                    case "istype": {
                        if (callParameters.Length == 1) {
                            return new DMASTImplicitIsType(callParameters[0].Value);
                        } else if (callParameters.Length == 2) {
                            return new DMASTIsType(callParameters[0].Value, callParameters[1].Value);
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
                            return new DMASTLocateCoordinates(callParameters[0].Value, callParameters[1].Value, callParameters[2].Value);
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

                            return new DMASTLocate(type, container);
                        }
                    }
                    default: return new DMASTProcCall(new DMASTCallableProcIdentifier(identifier.Identifier), callParameters);
                }
            }

            return expression;
        }

        private DMValueType AsTypes(DMValueType defaultType = DMValueType.Anything) {
            DMValueType type = DMValueType.Anything;

            if (Check(TokenType.DM_As)) {
                Whitespace();
                bool parenthetical = Check(TokenType.DM_LeftParenthesis);
                bool closed = false;
                Whitespace();

                do {
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
                        default: Error("Invalid value type '" + typeToken.Text + "'"); break;
                    }
                } while (Check(TokenType.DM_Bar));

                if (parenthetical && !closed) {
                    Whitespace();
                    ConsumeRightParenthesis();
                }
            } else {
                return defaultType;
            }

            return type;
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
