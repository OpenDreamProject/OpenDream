using System;
using System.Collections.Generic;
using OpenDreamShared.Dream;
using DereferenceType = OpenDreamShared.Compiler.DM.DMASTDereference.DereferenceType;
using Dereference = OpenDreamShared.Compiler.DM.DMASTDereference.Dereference;
using OpenDreamShared.Dream.Procs;
using System.Text;
using OpenDreamShared.Compiler.DMPreprocessor;

namespace OpenDreamShared.Compiler.DM {
    public partial class DMParser : Parser<Token> {
        public static char StringFormatCharacter = (char)0xFF;

        private DreamPath _currentPath = DreamPath.Root;
        private int topLevelErrors = 0;
        private int topLevelAttempts = 0;

        public DMParser(DMLexer lexer) : base(lexer) { }

        public DMASTFile File() {
            _saveForToplevel = true;
            DMASTBlockInner blockInner = TopLevel();
            Newline();
            Consume(TokenType.EndOfFile, "Expected EOF");
            Console.WriteLine(new String('=', 80));
            Console.WriteLine("Toplevel statements: " + blockInner.Statements.Length);
            Console.WriteLine("Errors: " + topLevelErrors + " out of " + topLevelAttempts + " toplevel attempts.");
            Console.WriteLine(new String('=', 80));

            return new DMASTFile(blockInner);
        }

        public DMASTBlockInner TopLevel() {
            List<DMASTStatement> statements = new();
            bool valid_state = true;
            do {
                _topLevelTokens.Clear();
                Errors = new();

                bool accepted = false;
                DMASTStatement statement = null;
                if (valid_state) {
                    topLevelAttempts += 1;
                }
                try {
                    statement = Statement();
                }
                catch (Exception e) {
                    if (valid_state) {
                        throw;
                    }
                }
                if (statement != null && Errors.Count == 0) {
                    statements.Add(statement);
                    accepted = true;
                }

                if (!accepted) {
                    LocateNextToplevel();
                    if (valid_state) {
                        HandleBlockInnerErrors(Errors);
                        topLevelErrors += 1;
                        valid_state = false;
                    }
                }
                else {
                    if (!valid_state) {
                        topLevelAttempts += 1;
                    }
                    if (!PeekDelimiter() && !valid_state) {
                        LocateNextToplevel();
                    }
                    valid_state = true;
                }
            } while (Delimiter());
            if (statements.Count == 0) {
                return null;
            }
            else {
                return new DMASTBlockInner(statements.ToArray());
            }
        }

        public DMASTBlockInner BlockInner() {
            DMASTStatement statement = Statement();

            if (statement != null) {
                List<DMASTStatement> statements = new() { statement };

                while (Delimiter()) {
                    statement = Statement();

                    if (statement != null) {
                        statements.Add(statement);
                    }
                }

                return new DMASTBlockInner(statements.ToArray());
            }
            else {
                return null;
            }
        }

        public DMASTStatement Statement() {
            DMASTPath path = Path();

            if (path != null) {
                DreamPath oldPath = _currentPath;
                DMASTStatement statement;

                _currentPath = _currentPath.Combine(path.Path);
                //Console.WriteLine(_currentPath);

                //Proc definition
                if (Check(TokenType.DM_LeftParenthesis)) {
                    DMASTDefinitionParameter[] parameters = DefinitionParameters();
                    Consume(TokenType.DM_RightParenthesis, "Expected ')'");

                    DMASTProcBlockInner procBlock = ProcBlock();
                    if (procBlock == null) {
                        DMASTProcStatement procStatement = ProcStatement();

                        if (procStatement != null) {
                            procBlock = new DMASTProcBlockInner(new DMASTProcStatement[] { procStatement });
                        }
                    }

                    statement = new DMASTProcDefinition(_currentPath, parameters, procBlock);
                } else {
                    DMASTBlockInner block = Block();

                    //Object definition
                    if (block != null) {
                        statement = new DMASTObjectDefinition(_currentPath, block);
                    } else {
                        //Var definition(s)
                        if (_currentPath.FindElement("var") != -1) {
                            DreamPath varPath = _currentPath;
                            List<DMASTObjectVarDefinition> varDefinitions = new();

                            while (true) {
                                DMASTExpression value;
                                if (Check(TokenType.DM_Equals)) {
                                    value = Expression();
                                    if (value == null) Error("Expected an expression");
                                } else {
                                    value = new DMASTConstantNull();
                                }

                                AsTypes();

                                varDefinitions.Add(new DMASTObjectVarDefinition(varPath, value));
                                if (Check(TokenType.DM_Comma)) {
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
                        } else {
                            //Var override
                            if (Check(TokenType.DM_Equals)) {
                                DMASTExpression value = Expression();
                                if (value == null) Error("Expected an expression");

                                statement = new DMASTObjectVarOverride(_currentPath, value);
                            } else {
                                //Empty object definition
                                statement = new DMASTObjectDefinition(_currentPath, null);
                            }
                        }
                    }
                }

                _currentPath = oldPath;
                return statement;
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
                TokenType.DM_List,
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

        public DMASTDereference Dereference() {
            Token leftToken = Current();

            if (Check(TokenType.DM_Identifier)) {
                Token dereferenceToken = Current();
                TokenType[] dereferenceTokenTypes = {
                    TokenType.DM_Period,
                    TokenType.DM_QuestionPeriod,
                    TokenType.DM_Colon,
                    TokenType.DM_QuestionColon,
                };

                if (Check(dereferenceTokenTypes)) {
                    List<Dereference> dereferences = new();
                    DMASTIdentifier identifier = Identifier();

                    if (identifier != null) {
                        do {
                            DereferenceType type;
                            bool conditional;
                            switch (dereferenceToken.Type) {
                                case TokenType.DM_Period:
                                    type = DereferenceType.Direct;
                                    conditional = false;
                                    break;
                                case TokenType.DM_QuestionPeriod:
                                    type = DereferenceType.Direct;
                                    conditional = true;
                                    break;
                                case TokenType.DM_Colon:
                                    type = DereferenceType.Search;
                                    conditional = false;
                                    break;
                                case TokenType.DM_QuestionColon:
                                    type = DereferenceType.Search;
                                    conditional = true;
                                    break;
                                default:
                                    throw new InvalidOperationException();
                            }

                            dereferences.Add(new Dereference(type, conditional, identifier.Identifier));

                            dereferenceToken = Current();
                            if (Check(dereferenceTokenTypes)) {
                                identifier = Identifier();
                                if (identifier == null) Error("Expected an identifier");
                            } else {
                                identifier = null;
                            }
                        } while (identifier != null);

                        return new DMASTDereference(new DMASTIdentifier(leftToken.Text), dereferences.ToArray());
                    } else {
                        ReuseToken(dereferenceToken);
                        ReuseToken(leftToken);
                    }
                } else {
                    ReuseToken(leftToken);
                }
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
                Newline();
                bool isIndented = Check(TokenType.DM_Indent);
                DMASTBlockInner blockInner = BlockInner();
                if (isIndented) Consume(TokenType.DM_Dedent, "Expected dedent");
                Consume(TokenType.DM_RightCurlyBracket, "Expected '}'");

                return blockInner;
            }

            return null;
        }

        public DMASTBlockInner IndentedBlock() {
            if (Check(TokenType.DM_Indent)) {
                DMASTBlockInner blockInner = BlockInner();

                if (blockInner != null) {
                    Newline();
                    Consume(TokenType.DM_Dedent, "Expected dedent");

                    return blockInner;
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
                Newline();
                bool isIndented = Check(TokenType.DM_Indent);
                DMASTProcBlockInner procBlock = ProcBlockInner();
                if (isIndented) Consume(TokenType.DM_Dedent, "Expected dedent");
                Consume(TokenType.DM_RightCurlyBracket, "Expected '}'");

                return procBlock;
            }

            return null;
        }

        public DMASTProcBlockInner IndentedProcBlock() {
            if (Check(TokenType.DM_Indent)) {
                DMASTProcBlockInner procBlock = ProcBlockInner();
                Consume(TokenType.DM_Dedent, "Expected dedent");

                return procBlock;
            }

            return null;
        }

        public DMASTProcBlockInner ProcBlockInner() {
            DMASTProcStatement procStatement = ProcStatement();

            if (procStatement != null) {
                List<DMASTProcStatement> procStatements = new List<DMASTProcStatement>() { procStatement };
                while (procStatement is DMASTProcStatementLabel || Delimiter()) {
                    procStatement = ProcStatement();
                    if (procStatement != null) {
                        procStatements.Add(procStatement);
                    }
                }

                return new DMASTProcBlockInner(procStatements.ToArray());
            } else {
                return null;
            }
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

                    if (procCall != null && procCall.Callable is DMASTCallableProcIdentifier) {
                        DMASTCallableProcIdentifier identifier = (DMASTCallableProcIdentifier)procCall.Callable;

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

                return procStatement;
            }
        }

        public DMASTProcStatement ProcVarDeclaration(bool allowMultiple = true) {
            Token firstToken = Current();
            bool wasSlash = Check(TokenType.DM_Slash);

            if (Check(TokenType.DM_Var)) {
                if (wasSlash) Error("Unsupported root variable declaration");

                DMASTPath varPath = Path();
                if (varPath == null) Error("Expected a variable name");

                List<DMASTProcStatementVarDeclaration> varDeclarations = new();
                while (true) {
                    DMASTExpression value = null;

                    //TODO: Multidimensional lists
                    if (Check(TokenType.DM_LeftBracket)) {
                        //Type information
                        if (varPath.Path.FindElement("list") != 0) {
                            varPath = new DMASTPath(DreamPath.List.Combine(varPath.Path));
                        }

                        DMASTExpression size = Expression();
                        Consume(TokenType.DM_RightBracket, "Expected ']'");

                        if (size is not null) {
                            value = new DMASTNewPath(new DMASTPath(DreamPath.List),
                                new[] { new DMASTCallParameter(size) });
                        }
                    }

                    if (Check(TokenType.DM_Equals)) {
                        if (value != null) Error("List doubly initialized");

                        value = Expression();
                        if (value == null) Error("Expected an expression");
                    }

                    AsTypes();

                    varDeclarations.Add(new DMASTProcStatementVarDeclaration(varPath, value ?? new DMASTConstantNull()));
                    if (allowMultiple && Check(TokenType.DM_Comma)) {
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
                DMASTIdentifier label = Identifier();

                return new DMASTProcStatementGoto(label);
            } else {
                return null;
            }
        }

        public DMASTProcStatementDel Del() {
            if (Check(TokenType.DM_Del)) {
                bool hasParenthesis = Check(TokenType.DM_LeftParenthesis);
                DMASTExpression value = Expression();
                if (value == null) Error("Expected value to delete");
                if (hasParenthesis) Consume(TokenType.DM_RightParenthesis, "Expected ')'");

                return new DMASTProcStatementDel(value);
            } else {
                return null;
            }
        }

        public DMASTProcStatementSet Set() {
            if (Check(TokenType.DM_Set)) {
                Token attributeToken = Current();

                if (Check(TokenType.DM_Identifier)) {
                    Consume(new TokenType[] { TokenType.DM_Equals, TokenType.DM_In }, "Expected '=' or 'in'");
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
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");

                DMASTExpression delay;
                if (Check(TokenType.DM_RightParenthesis)) {
                    //No parameters, default to zero
                    delay = new DMASTConstantInteger(0);
                } else {
                    delay = Expression();

                    if (delay == null) Error("Expected an expression");
                    Consume(TokenType.DM_RightParenthesis, "Expected ')'");
                }

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
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                DMASTExpression condition = Expression();
                if (condition == null) Error("Expected a condition");
                Consume(TokenType.DM_RightParenthesis, "Expected ')'");
                Check(TokenType.DM_Colon);

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
                    Check(TokenType.DM_Colon);
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
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                DMASTProcStatement initializer = null;
                DMASTIdentifier variable;

                DMASTProcStatementVarDeclaration variableDeclaration = ProcVarDeclaration(allowMultiple: false) as DMASTProcStatementVarDeclaration;
                if (variableDeclaration != null) {
                    initializer = variableDeclaration;
                    variable = new DMASTIdentifier(variableDeclaration.Name);
                } else {
                    variable = Identifier();
                    if (variable == null) Error("Expected an identifier");

                    if (Check(TokenType.DM_Equals)) {
                        DMASTExpression value = Expression();
                        if (value == null) Error("Expected an expression");

                        initializer = new DMASTProcStatementExpression(new DMASTAssign(variable, value));
                    }
                }

                AsTypes(); //TODO: Correctly handle

                if (Check(TokenType.DM_In)) {
                    DMASTExpression enumerateValue = Expression();
                    DMASTExpression toValue = null;
                    DMASTExpression step = new DMASTConstantInteger(1);

                    if (Check(TokenType.DM_To)) {
                        toValue = Expression();
                        if (toValue == null) Error("Expected an end to the range");

                        if (Check(TokenType.DM_Step)) {
                            step = Expression();
                            if (step == null) Error("Expected a step value");
                        }
                    }

                    Consume(TokenType.DM_RightParenthesis, "Expected ')'");
                    Newline();

                    DMASTProcBlockInner body = ProcBlock();
                    if (body == null) {
                        DMASTProcStatement statement = ProcStatement();

                        if (statement == null) Error("Expected body or statement");
                        body = new DMASTProcBlockInner(new DMASTProcStatement[] { statement });
                    }

                    if (toValue == null) {
                        return new DMASTProcStatementForList(initializer, variable, enumerateValue, body);
                    } else {
                        return new DMASTProcStatementForRange(initializer, variable, enumerateValue, toValue, step, body);
                    }
                } else if (Check(new TokenType[] { TokenType.DM_Comma, TokenType.DM_Semicolon })) {
                    DMASTExpression comparator = Expression();
                    Consume(new TokenType[] { TokenType.DM_Comma, TokenType.DM_Semicolon }, "Expected ','");
                    DMASTExpression incrementor = Expression();
                    Consume(TokenType.DM_RightParenthesis, "Expected ')'");
                    Newline();

                    DMASTProcBlockInner body = ProcBlock();
                    if (body == null) {
                        DMASTProcStatement statement = ProcStatement();

                        if (statement == null) Error("Expected body or statement");
                        body = new DMASTProcBlockInner(new DMASTProcStatement[] { statement });
                    }

                    return new DMASTProcStatementForStandard(initializer, comparator, incrementor, body);
                } else if (variableDeclaration != null) {
                    DMASTExpression rangeBegin = variableDeclaration.Value;
                    Consume(TokenType.DM_To, "Expected 'to'");
                    DMASTExpression rangeEnd = Expression();
                    if (rangeEnd == null) Error("Expected an expression");
                    DMASTExpression step = new DMASTConstantInteger(1);

                    if (Check(TokenType.DM_Step)) {
                        step = Expression();
                        if (step == null) Error("Expected a step value");
                    }

                    Consume(TokenType.DM_RightParenthesis, "Expected ')'");
                    Newline();

                    DMASTProcBlockInner body = ProcBlock();
                    if (body == null) {
                        DMASTProcStatement statement = ProcStatement();

                        if (statement == null) Error("Expected body or statement");
                        body = new DMASTProcBlockInner(new DMASTProcStatement[] { statement });
                    }

                    return new DMASTProcStatementForRange(initializer, variable, rangeBegin, rangeEnd, step, body);
                } else {
                    Error("Expected 'in'");
                }
            }

            return null;
        }

        public DMASTProcStatementWhile While() {
            if (Check(TokenType.DM_While)) {
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                DMASTExpression conditional = Expression();
                if (conditional == null) Error("Expected conditional");
                Consume(TokenType.DM_RightParenthesis, "Expected ')'");
                DMASTProcBlockInner body = ProcBlock();

                if (body == null) {
                    DMASTProcStatement statement = ProcStatement();
                    if (statement == null) Error("Expected statement");

                    body = new DMASTProcBlockInner(new DMASTProcStatement[] { statement });
                }

                return new DMASTProcStatementWhile(conditional, body);
            }

            return null;
        }

        public DMASTProcStatementDoWhile DoWhile() {
            if (Check(TokenType.DM_Do)) {
                DMASTProcBlockInner body = ProcBlock();

                if (body == null) {
                    DMASTProcStatement statement = ProcStatement();
                    if (statement == null) Error("Expected statement");

                    body = new DMASTProcBlockInner(new DMASTProcStatement[] { statement });
                }

                Newline();
                Consume(TokenType.DM_While, "Expected 'while'");
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                DMASTExpression conditional = Expression();
                if (conditional == null) Error("Expected conditional");
                Consume(TokenType.DM_RightParenthesis, "Expected ')'");

                return new DMASTProcStatementDoWhile(conditional, body);
            }

            return null;
        }

        public DMASTProcStatementSwitch Switch() {
            if (Check(TokenType.DM_Switch)) {
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                DMASTExpression value = Expression();
                Consume(TokenType.DM_RightParenthesis, "Expected ')'");
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
            return null; //TODO: Braced switch blocks
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
                    switchCase = SwitchCase();
                } while (switchCase != null);
            }

            return switchCases.ToArray();
        }

        public DMASTProcStatementSwitch.SwitchCase SwitchCase() {
            if (Check(TokenType.DM_If)) {
                List<DMASTExpression> expressions = new();

                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                do {
                    DMASTExpression expression = Expression();
                    if (expression == null) Error("Expected an expression");

                    if (Check(TokenType.DM_To)) {
                        DMASTExpression rangeEnd = Expression();
                        if (rangeEnd == null) Error("Expected an upper limit");

                        expressions.Add(new DMASTSwitchCaseRange(expression, rangeEnd));
                    } else {
                        expressions.Add(expression);
                    }
                } while (Check(TokenType.DM_Comma));
                Consume(TokenType.DM_RightParenthesis, "Expected ')'");
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

        public DMASTCallParameter[] ProcCall(bool includeEmptyParameters = true) {
            if (Check(TokenType.DM_LeftParenthesis)) {
                DMASTCallParameter[] callParameters = CallParameters(includeEmptyParameters);
                if (callParameters == null) callParameters = new DMASTCallParameter[0];
                Consume(TokenType.DM_RightParenthesis, "Expected ')'");

                return callParameters;
            }

            return null;
        }

        public DMASTCallParameter[] CallParameters(bool includeEmpty) {
            List<DMASTCallParameter> parameters = new List<DMASTCallParameter>();
            DMASTCallParameter parameter = CallParameter();

            while (parameter != null) {
                parameters.Add(parameter);

                if (Check(TokenType.DM_Comma)) {
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
                if (Check(TokenType.DM_LeftBracket)) {
                    DMASTExpression expression = Expression();
                    if (expression != null && expression is not DMASTExpressionConstant) Error("Expected a constant expression");
                    Consume(TokenType.DM_RightBracket, "Expected ']'");
                }

                DMASTExpression value = null;
                DMValueType type;
                DMASTExpression possibleValues = null;

                if (Check(TokenType.DM_Equals)) {
                    value = Expression();
                }

                type = AsTypes();

                if (Check(TokenType.DM_In)) {
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
                TokenType[] assignTypes = new TokenType[] {
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
                DMASTExpression b = ExpressionTernary();
                if (b == null) Error("Expected an expression");
                Consume(TokenType.DM_Colon, "Expected ':'");
                DMASTExpression c = ExpressionTernary();
                if (c == null) Error("Expected an expression");

                return new DMASTTernary(a, b, c);
            }

            return a;
        }

        public DMASTExpression ExpressionOr() {
            DMASTExpression a = ExpressionAnd();

            if (a != null && Check(TokenType.DM_BarBar)) {
                DMASTExpression b = ExpressionOr();
                if (b == null) Error("Expected a second value");

                return new DMASTOr(a, b);
            }

            return a;
        }

        public DMASTExpression ExpressionAnd() {
            DMASTExpression a = ExpressionBinaryOr();

            if (a != null && Check(TokenType.DM_AndAnd)) {
                DMASTExpression b = ExpressionAnd();
                if (b == null) Error("Expected a second value");

                return new DMASTAnd(a, b);
            }

            return a;
        }

        public DMASTExpression ExpressionBinaryOr() {
            DMASTExpression a = ExpressionBinaryXor();

            if (a != null && Check(TokenType.DM_Bar)) {
                DMASTExpression b = ExpressionBinaryOr();
                if (b == null) Error("Expected an expression");

                return new DMASTBinaryOr(a, b);
            }

            return a;
        }

        public DMASTExpression ExpressionBinaryXor() {
            DMASTExpression a = ExpressionBinaryAnd();

            if (a != null && Check(TokenType.DM_Xor)) {
                DMASTExpression b = ExpressionBinaryXor();
                if (b == null) Error("Expected an expression");

                return new DMASTBinaryXor(a, b);
            }

            return a;
        }

        public DMASTExpression ExpressionBinaryAnd() {
            DMASTExpression a = ExpressionComparison();

            if (a != null && Check(TokenType.DM_And)) {
                DMASTExpression b = ExpressionBinaryAnd();

                if (b == null) Error("Expected an expression");
                return new DMASTBinaryAnd(a, b);
            }

            return a;
        }

        public DMASTExpression ExpressionComparison() {
            DMASTExpression expression = ExpressionBitShift();

            if (expression != null) {
                Token token = Current();
                if (Check(new TokenType[] { TokenType.DM_EqualsEquals, TokenType.DM_ExclamationEquals })) {
                    DMASTExpression b = ExpressionComparison();

                    if (b == null) Error("Expected an expression to compare to");
                    switch (token.Type) {
                        case TokenType.DM_EqualsEquals: return new DMASTEqual(expression, b);
                        case TokenType.DM_ExclamationEquals: return new DMASTNotEqual(expression, b);
                    }
                }
            }

            return expression;
        }

        public DMASTExpression ExpressionBitShift() {
            DMASTExpression a = ExpressionComparisonLtGt();

            if (a != null) {
                if (Check(TokenType.DM_LeftShift)) {
                    DMASTExpression b = ExpressionBitShift();
                    if (b == null) Error("Expected an expression");

                    return new DMASTLeftShift(a, b);
                } else if (Check(TokenType.DM_RightShift)) {
                    DMASTExpression b = ExpressionBitShift();
                    if (b == null) Error("Expected an expression");

                    return new DMASTRightShift(a, b);
                }
            }

            return a;
        }

        public DMASTExpression ExpressionComparisonLtGt() {
            DMASTExpression a = ExpressionAdditionSubtraction();

            if (a != null) {
                Token token = Current();
                TokenType[] types = new TokenType[] {
                    TokenType.DM_LessThan,
                    TokenType.DM_LessThanEquals,
                    TokenType.DM_GreaterThan,
                    TokenType.DM_GreaterThanEquals
                };

                if (Check(types)) {
                    DMASTExpression b = ExpressionComparisonLtGt();
                    if (b == null) Error("Expected an expression");

                    switch (token.Type) {
                        case TokenType.DM_LessThan: return new DMASTLessThan(a, b);
                        case TokenType.DM_LessThanEquals: return new DMASTLessThanOrEqual(a, b);
                        case TokenType.DM_GreaterThan: return new DMASTGreaterThan(a, b);
                        case TokenType.DM_GreaterThanEquals: return new DMASTGreaterThanOrEqual(a, b);
                    }
                }
            }

            return a;
        }

        public DMASTExpression ExpressionAdditionSubtraction() {
            DMASTExpression a = ExpressionMultiplicationDivisionModulus();

            if (a != null) {
                Token token = Current();
                TokenType[] types = new TokenType[] {
                    TokenType.DM_Plus,
                    TokenType.DM_Minus,
                };

                while (Check(types)) {
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
                Token token = Current();
                TokenType[] types = new TokenType[] {
                    TokenType.DM_Star,
                    TokenType.DM_Slash,
                    TokenType.DM_Modulus
                };

                while (Check(types)) {
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

            if (a != null && Check(TokenType.DM_StarStar)) {
                DMASTExpression b = ExpressionPower();
                if (b == null) Error("Expected an expression");

                return new DMASTPower(a, b);
            }

            return a;
        }

        public DMASTExpression ExpressionIn() {
            DMASTExpression value = ExpressionUnary();

            if (value != null && Check(TokenType.DM_In)) {
                DMASTExpression list = ExpressionIn();

                return new DMASTExpressionIn(value, list);
            }

            return value;
        }

        public DMASTExpression ExpressionUnary() {
            if (Check(TokenType.DM_Exclamation)) {
                DMASTExpression expression = ExpressionUnary();
                if (expression == null) Error("Expected an expression");

                return new DMASTNot(expression);
            } else if (Check(TokenType.DM_Tilde)) {
                DMASTExpression expression = ExpressionUnary();
                if (expression == null) Error("Expected an expression");

                return new DMASTBinaryNot(expression);
            } else if (Check(TokenType.DM_PlusPlus)) {
                DMASTExpression expression = ExpressionSign();
                if (expression == null) Error("Expected an expression");

                return new DMASTPreIncrement(expression);
            } else if (Check(TokenType.DM_MinusMinus)) {
                DMASTExpression expression = ExpressionSign();
                if (expression == null) Error("Expected an expression");

                return new DMASTPreDecrement(expression);
            } else {
                DMASTExpression expression = ExpressionSign();

                if (expression != null) {
                    if (Check(TokenType.DM_PlusPlus)) {
                        expression = new DMASTPostIncrement(expression);
                    } else if (Check(TokenType.DM_MinusMinus)) {
                        expression = new DMASTPostDecrement(expression);
                    }
                }

                return expression;
            }
        }

        public DMASTExpression ExpressionSign() {
            Token token = Current();

            if (Check(new TokenType[] { TokenType.DM_Plus, TokenType.DM_Minus })) {
                DMASTExpression expression = ExpressionListIndex();

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
            } else {
                return ExpressionListIndex();
            }
        }

        public DMASTExpression ExpressionListIndex() {
            DMASTExpression expression = ExpressionNew();

            while (Check(TokenType.DM_LeftBracket)) {
                DMASTExpression index = Expression();
                Consume(TokenType.DM_RightBracket, "Expected ']'");

                expression = new DMASTListIndex(expression, index);
            }

            return expression;
        }

        public DMASTExpression ExpressionNew() {
            if (Check(TokenType.DM_New)) {
                DMASTDereference dereference = Dereference();
                DMASTIdentifier identifier = (dereference == null) ? Identifier() : null;
                DMASTPath path = (dereference == null && identifier == null ) ? Path(true) : null;
                DMASTCallParameter[] parameters = null;

                if (Check(TokenType.DM_LeftParenthesis)) {
                    parameters = CallParameters(true);
                    Consume(TokenType.DM_RightParenthesis, "Expected ')'");
                }

                if (dereference != null) {
                    return new DMASTNewDereference(dereference, parameters);
                } else if (identifier != null) {
                    return new DMASTNewIdentifier(identifier, parameters);
                } else if (path != null) {
                    return new DMASTNewPath(path, parameters);
                } else {
                    return new DMASTNewInferred(parameters);
                }
            } else {
                return ExpressionPrimary();
            }
        }

        public DMASTExpression ExpressionPrimary() {
            if (Check(TokenType.DM_LeftParenthesis)) {
                DMASTExpression inner = Expression();
                Consume(TokenType.DM_RightParenthesis, "Expected ')");

                return inner;
            } else {
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
                    DMASTDereference dereference = Dereference();
                    DMASTIdentifier identifier = (dereference == null) ? Identifier() : null;

                    if (dereference != null || identifier != null) {
                        DMASTCallParameter[] callParameters = ProcCall();

                        if (callParameters != null) {
                            if (identifier?.Identifier == "input") {
                                DMValueType types = AsTypes(defaultType: DMValueType.Text);
                                DMASTExpression list = null;

                                if (Check(TokenType.DM_In)) {
                                    list = Expression();
                                }

                                return new DMASTInput(callParameters, types, list);
                            } else if (identifier?.Identifier == "initial") {
                                if (callParameters.Length != 1) Error("initial() requires 1 argument");

                                return new DMASTInitial(callParameters[0].Value);
                            } else if (identifier?.Identifier == "issaved") {
                                if (callParameters.Length != 1) Error("issaved() requires 1 argument");

                                return new DMASTIsSaved(callParameters[0].Value);
                            } else if (identifier?.Identifier == "istype") {
                                if (callParameters.Length == 1) {
                                    return new DMASTImplicitIsType(callParameters[0].Value);
                                } else if (callParameters.Length == 2) {
                                    return new DMASTIsType(callParameters[0].Value, callParameters[1].Value);
                                } else {
                                    Error("istype() requires 1 or 2 arguments");
                                }
                            } else if (identifier?.Identifier == "text") {
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

                                    return null;
                                }
                            } else if (identifier?.Identifier == "locate") {
                                if (callParameters.Length > 3) Error("locate() was given too many arguments");

                                if (callParameters.Length == 3) { //locate(X, Y, Z)
                                    return new DMASTLocateCoordinates(callParameters[0].Value, callParameters[1].Value, callParameters[2].Value);
                                } else {
                                    DMASTExpression container = null;
                                    if (Check(TokenType.DM_In)) {
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
                            } else {
                                DMASTCallable callable;

                                if (dereference != null) {
                                    callable = new DMASTDereferenceProc(dereference.Expression, dereference.Dereferences);
                                } else {
                                    callable = new DMASTCallableProcIdentifier(identifier.Identifier);
                                }

                                primary = new DMASTProcCall(callable, callParameters);
                            }
                        } else {
                            primary = (dereference != null) ? dereference : identifier;
                        }
                    }
                }

                if (primary == null) {
                    DMASTCallable callable = Callable();

                    if (callable != null) {
                        DMASTCallParameter[] callParameters = ProcCall();

                        if (callParameters != null) {
                            primary = new DMASTProcCall(callable, callParameters);
                        } else {
                            primary = callable;
                        }
                    }
                }

                if (primary == null && Check(TokenType.DM_Call)) {
                    DMASTCallParameter[] callParameters = ProcCall();
                    if (callParameters == null || callParameters.Length < 1 || callParameters.Length > 2) Error("Call must have 2 parameters");
                    DMASTCallParameter[] procParameters = ProcCall();
                    if (procParameters == null) Error("Expected proc parameters");

                    primary = new DMASTCall(callParameters, procParameters);
                }

                if (primary == null && Check(TokenType.DM_List)) {
                    DMASTCallParameter[] values = ProcCall(false);

                    primary = new DMASTList(values);
                }

                if (primary == null && Check(TokenType.DM_NewList)) {
                    DMASTCallParameter[] values = ProcCall(false);

                    primary = new DMASTNewList(values);
                }

                return primary;
            }
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

                                    DMLexer expressionLexer = new TokenWhitespaceFilter(constantToken.SourceFile, preprocTokens);
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

                while (Check(new TokenType[] { TokenType.DM_Whitespace, TokenType.DM_Indent, TokenType.DM_Dedent })) hadWhitespace = true;
                return hadWhitespace;
            } else {
                return Check(TokenType.DM_Whitespace);
            }
        }

        private DMValueType AsTypes(DMValueType defaultType = DMValueType.Anything) {
            DMValueType type = DMValueType.Anything;

            if (Check(TokenType.DM_As)) {
                bool parenthetical = Check(TokenType.DM_LeftParenthesis);
                bool closed = false;

                do {
                    Token typeToken = Current();

                    if (parenthetical)
                    {
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
                        default: Error("Invalid value type '" + typeToken.Text + "'"); break;
                    }
                } while (Check(TokenType.DM_Bar));

                if (parenthetical && !closed) {
                    Consume(TokenType.DM_RightParenthesis, "Expected closing parenthesis");
                }
            }
            else {
                return defaultType;
            }

            return type;
        }

        private bool Delimiter() {
            return Check(TokenType.DM_Semicolon) || Newline();
        }
    }
}
