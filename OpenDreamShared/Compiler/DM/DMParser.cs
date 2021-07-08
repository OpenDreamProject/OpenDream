using System;
using System.Collections.Generic;
using OpenDreamShared.Dream;
using DereferenceType = OpenDreamShared.Compiler.DM.DMASTDereference.DereferenceType;
using Dereference = OpenDreamShared.Compiler.DM.DMASTDereference.Dereference;
using OpenDreamShared.Dream.Procs;
using System.Text;
using OpenDreamShared.Compiler.DMPreprocessor;

namespace OpenDreamShared.Compiler.DM {
    public class DMParser : Parser<Token> {
        public static char StringFormatCharacter = (char)0xFF;

        private DreamPath _currentPath = DreamPath.Root;

        public DMParser(DMLexer lexer) : base(lexer) { }

        public DMASTFile File() {
            DMASTBlockInner blockInner = BlockInner();
            Newline();
            Consume(TokenType.EndOfFile, "Expected EOF");

            return new DMASTFile(blockInner);
        }

        public DMASTBlockInner BlockInner() {
            DMASTStatement statement = Statement();

            if (statement != null) {
                List<DMASTStatement> statements = new() { statement };

                while (Delimiter()) {
                    Whitespace();
                    statement = Statement();

                    if (statement != null) {
                        Whitespace();

                        statements.Add(statement);
                    }
                }

                return new DMASTBlockInner(statements.ToArray());
            } else {
                return null;
            }
        }

        public DMASTStatement Statement() {
            DMASTPath path = Path();

            if (path != null) {
                DreamPath oldPath = _currentPath;
                DMASTStatement statement;

                Whitespace();

                _currentPath = _currentPath.Combine(path.Path);
                if (Check(TokenType.DM_LeftParenthesis)) {
                    Whitespace();
                    DMASTDefinitionParameter[] parameters = DefinitionParameters();
                    Whitespace();
                    Consume(TokenType.DM_RightParenthesis, "Expected ')'");
                    Whitespace();

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

                    if (block != null) {
                        statement = new DMASTObjectDefinition(_currentPath, block);
                    } else {
                        if (Check(TokenType.DM_Equals)) {
                            Whitespace();
                            DMASTExpression value = Expression();

                            if (_currentPath.FindElement("var") != -1) {
                                statement = new DMASTObjectVarDefinition(_currentPath, value);
                            } else {
                                statement = new DMASTObjectVarOverride(_currentPath, value);
                            }
                        } else {
                            if (_currentPath.FindElement("var") != -1) {
                                Whitespace();
                                AsTypes();
                                statement = new DMASTObjectVarDefinition(_currentPath, new DMASTConstantNull());
                            } else {
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
                List<string> pathElements = new List<string>() { pathElement };

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
                TokenType[] dereferenceTokenTypes = new TokenType[] {
                    TokenType.DM_Period,
                    TokenType.DM_QuestionPeriod,
                    TokenType.DM_Colon,
                    TokenType.DM_QuestionColon,
                };

                if (Check(dereferenceTokenTypes)) {
                    List<Dereference> dereferences = new List<Dereference>();
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
                Whitespace();
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
                Whitespace();
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
                    Whitespace();
                    procStatement = ProcStatement();

                    if (procStatement != null) {
                        Whitespace();

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

                if (procStatement != null)
                {
                    Whitespace();
                }


                return procStatement;
            }
        }

        public DMASTProcStatementVarDeclaration ProcVarDeclaration() {
            Token firstToken = Current();
            bool wasSlash = Check(TokenType.DM_Slash);

            if (Check(TokenType.DM_Var)) {
                if (wasSlash) Error("Unsupported root variable declaration");

                Whitespace();
                DMASTPath path = Path();
                if (path == null) Error("Expected a variable name");

                DMASTExpression value = null;

                if (Check(TokenType.DM_LeftBracket)) //TODO: Multidimensional lists
                {
                    //Type information
                    if (path.Path.FindElement("list") != 0)
                    {
                        path = new DMASTPath(new DreamPath("list/" + path.Path.PathString));
                    }

                    Whitespace();

                    DMASTExpression size = Expression();
                    Consume(TokenType.DM_RightBracket, "Expected ']'");

                    if (size is not null)
                    {
                        value = new DMASTNewPath(new DMASTPath(DreamPath.List),
                            new[] {new DMASTCallParameter(size)});
                    }

                }

                Whitespace();

                if (Check(TokenType.DM_Equals)) {
                    if (value is not null)
                    {
                        Error("List doubly initialized");
                    }
                    else
                    {
                        Whitespace();
                        value = Expression();
                    }
                }

                return new DMASTProcStatementVarDeclaration(path, value);
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
                if (hasParenthesis) Consume(TokenType.DM_RightParenthesis, "Expected ')'");

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
                    Consume(TokenType.DM_RightParenthesis, "Expected ')'");
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
                Whitespace();
                DMASTExpression condition = Expression();
                if (condition == null) Error("Expected a condition");
                Consume(TokenType.DM_RightParenthesis, "Expected ')'");
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

                DMASTProcStatementVarDeclaration variableDeclaration = ProcVarDeclaration();
                if (variableDeclaration != null) {
                    initializer = variableDeclaration;
                    variable = new DMASTIdentifier(variableDeclaration.Name);
                } else {
                    variable = Identifier();
                    if (variable == null) Error("Expected an identifier");

                    Whitespace();
                    if (Check(TokenType.DM_Equals)) {
                        Whitespace();
                        DMASTExpression value = Expression();
                        if (value == null) Error("Expected an expression");

                        initializer = new DMASTProcStatementExpression(new DMASTAssign(variable, value));
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

                    Consume(TokenType.DM_RightParenthesis, "Expected ')'");
                    Whitespace();
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
                    Whitespace();
                    DMASTExpression comparator = Expression();
                    Consume(new TokenType[] { TokenType.DM_Comma, TokenType.DM_Semicolon }, "Expected ','");
                    Whitespace();
                    DMASTExpression incrementor = Expression();
                    Consume(TokenType.DM_RightParenthesis, "Expected ')'");
                    Whitespace();
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
                    Whitespace();
                    Consume(TokenType.DM_To, "Expected 'to'");
                    Whitespace();
                    DMASTExpression rangeEnd = Expression();
                    if (rangeEnd == null) Error("Expected an expression");
                    DMASTExpression step = new DMASTConstantInteger(1);

                    if (Check(TokenType.DM_Step)) {
                        Whitespace();

                        step = Expression();
                        if (step == null) Error("Expected a step value");
                    }

                    Consume(TokenType.DM_RightParenthesis, "Expected ')'");
                    Whitespace();
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
                Whitespace();
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                Whitespace();
                DMASTExpression conditional = Expression();
                if (conditional == null) Error("Expected conditional");
                Consume(TokenType.DM_RightParenthesis, "Expected ')'");
                Whitespace();
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
                Whitespace();
                DMASTProcBlockInner body = ProcBlock();

                if (body == null) {
                    DMASTProcStatement statement = ProcStatement();
                    if (statement == null) Error("Expected statement");

                    body = new DMASTProcBlockInner(new DMASTProcStatement[] { statement });
                }

                Newline();
                Consume(TokenType.DM_While, "Expected 'while'");
                Whitespace();
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                Whitespace();
                DMASTExpression conditional = Expression();
                if (conditional == null) Error("Expected conditional");
                Consume(TokenType.DM_RightParenthesis, "Expected ')'");
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
                Consume(TokenType.DM_RightParenthesis, "Expected ')'");
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

                Whitespace();
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                do {
                    Whitespace();
                    DMASTExpression expression = Expression();
                    if (expression == null) Error("Expected an expression");

                    if (Check(TokenType.DM_To)) {
                        Whitespace();
                        DMASTExpression rangeEnd = Expression();
                        if (rangeEnd == null) Error("Expected an upper limit");

                        expressions.Add(new DMASTSwitchCaseRange(expression, rangeEnd));
                    } else {
                        expressions.Add(expression);
                    }
                } while (Check(TokenType.DM_Comma));
                Consume(TokenType.DM_RightParenthesis, "Expected ')'");
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

        public DMASTCallParameter[] ProcCall(bool includeEmptyParameters = true) {
            if (Check(TokenType.DM_LeftParenthesis)) {
                Whitespace();
                DMASTCallParameter[] callParameters = CallParameters(includeEmptyParameters);
                if (callParameters == null) callParameters = new DMASTCallParameter[0];
                Whitespace();
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
                    Whitespace();
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
                    Whitespace();

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
                    Consume(TokenType.DM_RightBracket, "Expected ']'");
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
                Consume(TokenType.DM_Colon, "Expected ':'");
                Whitespace();
                DMASTExpression c = ExpressionTernary();
                if (c == null) Error("Expected an expression");

                return new DMASTTernary(a, b, c);
            }

            return a;
        }

        public DMASTExpression ExpressionOr() {
            DMASTExpression a = ExpressionAnd();

            if (a != null && Check(TokenType.DM_BarBar)) {
                Whitespace();
                DMASTExpression b = ExpressionOr();
                if (b == null) Error("Expected a second value");

                return new DMASTOr(a, b);
            }

            return a;
        }

        public DMASTExpression ExpressionAnd() {
            DMASTExpression a = ExpressionBinaryOr();

            if (a != null && Check(TokenType.DM_AndAnd)) {
                Whitespace();
                DMASTExpression b = ExpressionAnd();
                if (b == null) Error("Expected a second value");

                return new DMASTAnd(a, b);
            }

            return a;
        }

        public DMASTExpression ExpressionBinaryOr() {
            DMASTExpression a = ExpressionBinaryXor();

            if (a != null && Check(TokenType.DM_Bar)) {
                Whitespace();
                DMASTExpression b = ExpressionBinaryOr();
                if (b == null) Error("Expected an expression");

                return new DMASTBinaryOr(a, b);
            }

            return a;
        }

        public DMASTExpression ExpressionBinaryXor() {
            DMASTExpression a = ExpressionBinaryAnd();

            if (a != null && Check(TokenType.DM_Xor)) {
                Whitespace();
                DMASTExpression b = ExpressionBinaryXor();
                if (b == null) Error("Expected an expression");

                return new DMASTBinaryXor(a, b);
            }

            return a;
        }

        public DMASTExpression ExpressionBinaryAnd() {
            DMASTExpression a = ExpressionComparison();

            if (a != null && Check(TokenType.DM_And)) {
                Whitespace();
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
                    Whitespace();
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
                    Whitespace();
                    DMASTExpression b = ExpressionBitShift();
                    if (b == null) Error("Expected an expression");

                    return new DMASTLeftShift(a, b);
                } else if (Check(TokenType.DM_RightShift)) {
                    Whitespace();
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
                    Whitespace();
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
                Token token = Current();
                TokenType[] types = new TokenType[] {
                    TokenType.DM_Star,
                    TokenType.DM_Slash,
                    TokenType.DM_Modulus
                };

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

            if (a != null && Check(TokenType.DM_StarStar)) {
                Whitespace();
                DMASTExpression b = ExpressionPower();
                if (b == null) Error("Expected an expression");

                return new DMASTPower(a, b);
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
                Whitespace();
                DMASTExpression index = Expression();
                Consume(TokenType.DM_RightBracket, "Expected ']'");
                Whitespace();

                expression = new DMASTListIndex(expression, index);
            }

            return expression;
        }

        public DMASTExpression ExpressionNew() {
            if (Check(TokenType.DM_New)) {
                Whitespace();
                DMASTDereference dereference = Dereference();
                DMASTIdentifier identifier = (dereference == null) ? Identifier() : null;
                DMASTPath path = (dereference == null && identifier == null ) ? Path(true) : null;
                Whitespace();
                DMASTCallParameter[] parameters = null;

                if (Check(TokenType.DM_LeftParenthesis)) {
                    Whitespace();
                    parameters = CallParameters(true);
                    Consume(TokenType.DM_RightParenthesis, "Expected ')'");
                    Whitespace();
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
                Whitespace();
                DMASTExpression inner = Expression();
                Consume(TokenType.DM_RightParenthesis, "Expected ')");
                Whitespace();

                return inner;
            } else {
                DMASTExpression primary = Constant();

                if (primary == null) {
                    DMASTPath path = Path(true);

                    if (path != null) primary = new DMASTConstantPath(path);
                }

                if (primary == null) {
                    DMASTDereference dereference = Dereference();
                    DMASTIdentifier identifier = (dereference == null) ? Identifier() : null;

                    if (dereference != null || identifier != null) {
                        Whitespace();
                        DMASTCallParameter[] callParameters = ProcCall();

                        if (callParameters != null) {
                            Whitespace();

                            if (identifier != null && identifier.Identifier == "input") {
                                DMValueType types = AsTypes(defaultType: DMValueType.Text);
                                Whitespace();
                                DMASTExpression list = null;

                                if (Check(TokenType.DM_In)) {
                                    Whitespace();
                                    list = Expression();
                                }

                                return new DMASTInput(callParameters, types, list);
                            } else if (identifier != null && identifier.Identifier == "initial") {
                                if (callParameters.Length != 1) Error("initial() requires 1 argument");

                                return new DMASTInitial(callParameters[0].Value);
                            } else if (identifier != null && identifier.Identifier == "issaved") {
                                if (callParameters.Length != 1) Error("issaved() requires 1 argument");

                                return new DMASTIsSaved(callParameters[0].Value);
                            } else if (identifier != null && identifier.Identifier == "istype") {
                                if (callParameters.Length == 1) {
                                    return new DMASTImplicitIsType(callParameters[0].Value);
                                } else if (callParameters.Length == 2) {
                                    return new DMASTIsType(callParameters[0].Value, callParameters[1].Value);
                                } else {
                                    Error("istype() requires 1 or 2 arguments");
                                }
                            } else if (identifier != null && identifier.Identifier == "text") {
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
                            } else if (identifier != null && identifier.Identifier == "locate") {
                                if (callParameters.Length > 3) Error("locate() was given too many arguments");

                                if (callParameters.Length == 3) { //locate(X, Y, Z)
                                    return new DMASTLocateCoordinates(callParameters[0].Value, callParameters[1].Value, callParameters[2].Value);
                                } else {
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
                        Whitespace();
                        DMASTCallParameter[] callParameters = ProcCall();

                        if (callParameters != null) {
                            primary = new DMASTProcCall(callable, callParameters);
                        } else {
                            primary = callable;
                        }
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

                if (primary == null && Check(TokenType.DM_List)) {
                    Whitespace();
                    DMASTCallParameter[] values = ProcCall(false);

                    primary = new DMASTList(values);
                }

                if (primary == null && Check(TokenType.DM_NewList)) {
                    Whitespace();
                    DMASTCallParameter[] values = ProcCall(false);

                    primary = new DMASTNewList(values);
                }

                if (primary != null) Whitespace();
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
                            } while (c != ' ');

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

                Whitespace();
                bool parenthetical = Check(TokenType.DM_LeftParenthesis);
                bool closed = false;
                Whitespace();

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
                    Whitespace();
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
