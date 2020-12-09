using System;
using System.Collections.Generic;
using OpenDreamShared.Compiler;
using OpenDreamShared.Dream;
using DereferenceType = DMCompiler.DM.DMASTCallableDereference.DereferenceType;
using Dereference = DMCompiler.DM.DMASTCallableDereference.Dereference;
using OpenDreamShared.Dream.Procs;

namespace DMCompiler.DM {
    class DMParser : Parser {
        public static char StringFormatCharacter = (char)0xFF;

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
                List<DMASTStatement> statements = new List<DMASTStatement>() { statement };

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
                Whitespace();

                if (Check(TokenType.DM_LeftParenthesis)) {
                    Whitespace();
                    DMASTDefinitionParameter[] parameters = DefinitionParameters();
                    Whitespace();
                    Consume(TokenType.DM_RightParenthesis, "Expected ')'");
                    Whitespace();
                    DMASTProcBlockInner procBlock = ProcBlock();

                    return new DMASTProcDefinition(path, parameters, procBlock);
                } else {
                    DMASTBlockInner block = Block();

                    if (block != null) {
                        return new DMASTObjectDefinition(path, block);
                    } else {
                        if (Check(TokenType.DM_Equals)) {
                            Whitespace();
                            DMASTExpression value = Expression();

                            if (path.Path.FindElement("var") != -1) {
                                return new DMASTObjectVarDefinition(path, value);
                            } else {
                                return new DMASTObjectVarOverride(path, value);
                            }
                        } else {
                            if (path.Path.FindElement("var") != -1) {
                                return new DMASTObjectVarDefinition(path, new DMASTConstantNull());
                            } else {
                                return new DMASTObjectDefinition(path, null);
                            }
                        }
                    }
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
            Token elementToken = Current();

            if (Check(new TokenType[] { TokenType.DM_Identifier, TokenType.DM_Var, TokenType.DM_Proc, TokenType.DM_List })) {
                return elementToken.Text;
            } else {
                return null;
            }
        }

        public DMASTCallable Callable() {
            DMASTCallable callable = Dereference();

            if (callable == null && Check(TokenType.DM_SuperProc)) callable = new DMASTCallableSuper();
            if (callable == null && Check(TokenType.DM_Period)) callable = new DMASTCallableSelf();
            if (callable == null) callable = Identifier();
            
            return callable;
        }

        public DMASTCallableIdentifier Identifier() {
            Token token = Current();

            if (Check(TokenType.DM_Identifier)) {
                return new DMASTCallableIdentifier(token.Text);
            }

            return null;
        }

        public DMASTCallableDereference Dereference() {
            Token leftToken = Current();

            if (Check(TokenType.DM_Identifier)) {
                Token dereferenceToken = Current();
                TokenType[] dereferenceTokenTypes = new TokenType[] {
                    TokenType.DM_Period,
                    TokenType.DM_Colon
                };

                if (Check(dereferenceTokenTypes)) {
                    List<Dereference> dereferences = new List<Dereference>();
                    DMASTCallableIdentifier identifier = Identifier();

                    if (identifier != null) {
                        do {
                            DereferenceType type = (dereferenceToken.Type == TokenType.DM_Period) ? DereferenceType.Direct : DereferenceType.Search;
                            dereferences.Add(new Dereference(type, identifier.Identifier));

                            dereferenceToken = Current();
                            if (Check(dereferenceTokenTypes)) {
                                identifier = Identifier();
                                if (identifier == null) throw new Exception("Expected an identifier");
                            } else {
                                identifier = null;
                            }
                        } while (identifier != null);

                        return new DMASTCallableDereference(new DMASTCallableIdentifier(leftToken.Text), dereferences.ToArray());
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
            DMASTBlockInner block = BracedBlock();
            if (block == null) block = IndentedBlock();

            return block;
        }

        public DMASTBlockInner BracedBlock() {
            return null;
        }

        public DMASTBlockInner IndentedBlock() {
            if (Check(TokenType.DM_Indent)) {
                Newline();
                DMASTBlockInner blockInner = BlockInner();

                if (blockInner != null) {
                    Newline();

                    if (Check(TokenType.DM_Dedent)) {
                        return blockInner;
                    } else {
                        throw new Exception("Expected dedent");
                    }
                }
            }

            return null;
        }

        public DMASTProcBlockInner ProcBlock() {
            DMASTProcBlockInner procBlock = BracedProcBlock();
            if (procBlock == null) procBlock = IndentedProcBlock();

            return procBlock;
        }

        public DMASTProcBlockInner BracedProcBlock() {
            if (Check(TokenType.DM_LeftCurlyBracket)) {
                Whitespace();
                bool isIndented = Check(TokenType.DM_Indent);
                Newline();
                DMASTProcBlockInner procBlock = ProcBlockInner();
                if (isIndented) Consume(TokenType.DM_Dedent, "Expected dedent");
                Consume(TokenType.DM_RightCurlyBracket, "Expected '}'");

                return procBlock;
            }

            return null;
        }

        public DMASTProcBlockInner IndentedProcBlock() {
            if (Check(TokenType.DM_Indent)) {
                Newline();
                DMASTProcBlockInner procBlock = ProcBlockInner();
                Newline();
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
                if (expression is DMASTCallableIdentifier) {
                    Check(TokenType.DM_Colon);

                    return new DMASTProcStatementLabel(((DMASTCallableIdentifier)expression).Identifier);
                } else if (expression is DMASTLeftShift) {
                    DMASTLeftShift leftShift = (DMASTLeftShift)expression;
                    DMASTProcCall procCall = leftShift.B as DMASTProcCall;

                    if (procCall != null && procCall.Callable is DMASTCallableIdentifier) {
                        DMASTCallableIdentifier identifier = (DMASTCallableIdentifier)procCall.Callable;

                        if (identifier.Identifier == "browse") {
                            if (procCall.Parameters.Length != 1 && procCall.Parameters.Length != 2) throw new Exception("browse() requires 1 or 2 parameters");

                            DMASTExpression body = procCall.Parameters[0].Value;
                            DMASTExpression options = (procCall.Parameters.Length == 2) ? procCall.Parameters[1].Value : new DMASTConstantNull();
                            return new DMASTProcStatementBrowse(leftShift.A, body, options);
                        } else if (identifier.Identifier == "browse_rsc") {
                            if (procCall.Parameters.Length != 1 && procCall.Parameters.Length != 2) throw new Exception("browse_rsc() requires 1 or 2 parameters");

                            DMASTExpression file = procCall.Parameters[0].Value;
                            DMASTExpression filepath = (procCall.Parameters.Length == 2) ? procCall.Parameters[1].Value : new DMASTConstantNull();
                            return new DMASTProcStatementBrowseResource(leftShift.A, file, filepath);
                        } else if (identifier.Identifier == "output") {
                            if (procCall.Parameters.Length != 2) throw new Exception("output() requires 2 parameters");

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

        public DMASTProcStatementVarDeclaration ProcVarDeclaration() {
            Token firstToken = Current();
            bool wasSlash = Check(TokenType.DM_Slash);

            if (Check(TokenType.DM_Var)) {
                if (wasSlash) throw new Exception("Unsupported root variable declaration");

                Whitespace();
                DMASTPath path = Path();
                if (path == null) throw new Exception("Expected a variable name");
                Whitespace();
                
                DMASTExpression value = null;

                if (Check(TokenType.DM_Equals)) {
                    Whitespace();
                    value = Expression();
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
                DMASTCallableIdentifier label = Identifier();

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
                if (value == null) throw new Exception("Expected value to delete");
                if (hasParenthesis) Consume(TokenType.DM_RightParenthesis, "Expected ')'");

                return new DMASTProcStatementDel(value);
            } else {
                return null;
            }
        }

        public DMASTProcStatementSet Set() {
            if (Check(TokenType.DM_Set)) {
                Whitespace();
                Token propertyToken = Current();

                if (Check(TokenType.DM_Identifier)) {
                    Whitespace();
                    Consume(new TokenType[] { TokenType.DM_Equals, TokenType.DM_In }, "Expected '=' or 'in'");
                    Whitespace();
                    DMASTExpression value = Expression();
                    if (value == null) throw new Exception("Expected an expression");

                    return new DMASTProcStatementSet(propertyToken.Text, value);
                } else {
                    throw new Exception("Expected property name");
                }
            } else {
                return null;
            }
        }

        public DMASTProcStatementSpawn Spawn() {
            if (Check(TokenType.DM_Spawn)) {
                Whitespace();
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                Whitespace();
                DMASTExpression time = Expression();
                if (time == null) throw new Exception("Expected an expression");
                Consume(TokenType.DM_RightParenthesis, "Expected ')'");
                Whitespace();
                Newline();

                DMASTProcBlockInner body = ProcBlock();
                if (body == null) {
                    DMASTProcStatement statement = ProcStatement();

                    if (statement == null) throw new Exception("Expected body or statement");
                    body = new DMASTProcBlockInner(new DMASTProcStatement[] { statement });
                }

                return new DMASTProcStatementSpawn(time, body);
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
                if (condition == null) throw new Exception("Expected a condition");
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
                DMASTCallableIdentifier variable;

                DMASTProcStatementVarDeclaration variableDeclaration = ProcVarDeclaration();
                if (variableDeclaration != null) {
                    initializer = variableDeclaration;
                    variable = new DMASTCallableIdentifier(variableDeclaration.Name);
                } else {
                    variable = Identifier();
                    if (variable == null) throw new Exception("Expected an identifier");

                    Whitespace();
                    if (Check(TokenType.DM_Equals)) {
                        Whitespace();
                        DMASTExpression value = Expression();
                        if (value == null) throw new Exception("Expected an expression");

                        initializer = new DMASTProcStatementExpression(new DMASTAssign(variable, value));
                    }
                }

                Whitespace();
                AsTypes(); //TODO: Correctly handle
                Whitespace();

                if (Check(TokenType.DM_In)) {
                    Whitespace();
                    DMASTExpression list = Expression();
                    Consume(TokenType.DM_RightParenthesis, "Expected ')'");
                    Whitespace();
                    Newline();

                    DMASTProcBlockInner body = ProcBlock();
                    if (body == null) {
                        DMASTProcStatement statement = ProcStatement();

                        if (statement == null) throw new Exception("Expected body or statement");
                        body = new DMASTProcBlockInner(new DMASTProcStatement[] { statement });
                    }

                    return new DMASTProcStatementForList(initializer, variable, list, body);
                } else if (Check(new TokenType[] { TokenType.DM_Comma, TokenType.DM_Semicolon })) {
                    Whitespace();
                    DMASTProcStatement comparator = ProcStatement();
                    Consume(new TokenType[] { TokenType.DM_Comma, TokenType.DM_Semicolon }, "Expected ','");
                    Whitespace();
                    DMASTProcStatement incrementor = ProcStatement();
                    Consume(TokenType.DM_RightParenthesis, "Expected ')'");
                    Whitespace();
                    Newline();

                    DMASTProcBlockInner body = ProcBlock();
                    if (body == null) {
                        DMASTProcStatement statement = ProcStatement();

                        if (statement == null) throw new Exception("Expected body or statement");
                        body = new DMASTProcBlockInner(new DMASTProcStatement[] { statement });
                    }

                    return new DMASTProcStatementForStandard(initializer, comparator, incrementor, body);
                } else if (variableDeclaration != null) {
                    DMASTExpression rangeBegin = variableDeclaration.Value;
                    Whitespace();
                    Consume(TokenType.DM_To, "Expected 'to'");
                    Whitespace();
                    DMASTExpression rangeEnd = Expression();
                    if (rangeEnd == null) throw new Exception("Expected an expression");
                    Consume(TokenType.DM_RightParenthesis, "Expected ')'");
                    Whitespace();
                    Newline();

                    DMASTProcBlockInner body = ProcBlock();
                    if (body == null) {
                        DMASTProcStatement statement = ProcStatement();

                        if (statement == null) throw new Exception("Expected body or statement");
                        body = new DMASTProcBlockInner(new DMASTProcStatement[] { statement });
                    }

                    return new DMASTProcStatementForNumberRange(initializer, variable, rangeBegin, rangeEnd, new DMASTConstantInteger(1), body);
                } else {
                    throw new Exception("Expected 'in'");
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
                if (conditional == null) throw new Exception("Expected conditional");
                Consume(TokenType.DM_RightParenthesis, "Expected ')'");
                Whitespace();
                DMASTProcBlockInner body = ProcBlock();

                if (body == null) {
                    DMASTProcStatement statement = ProcStatement();
                    if (statement == null) throw new Exception("Expected statement");

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
                    if (statement == null) throw new Exception("Expected statement");

                    body = new DMASTProcBlockInner(new DMASTProcStatement[] { statement });
                }

                Newline();
                Consume(TokenType.DM_While, "Expected 'while'");
                Whitespace();
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                Whitespace();
                DMASTExpression conditional = Expression();
                if (conditional == null) throw new Exception("Expected conditional");
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

                if (switchCases == null) throw new Exception("Expected switch cases");
                return new DMASTProcStatementSwitch(value, switchCases);
            }

            return null;
        }

        public DMASTProcStatementSwitch.SwitchCase[] SwitchCases() {
            DMASTProcStatementSwitch.SwitchCase[] switchCases = BracedSwitchInner();
            if (switchCases == null) switchCases = IndentedSwitchInner();

            return switchCases;
        }

        public DMASTProcStatementSwitch.SwitchCase[] BracedSwitchInner() {
            return null;
        }

        public DMASTProcStatementSwitch.SwitchCase[] IndentedSwitchInner() {
            if (Check(TokenType.DM_Indent)) {
                Newline();
                DMASTProcStatementSwitch.SwitchCase[] switchInner = SwitchInner();
                Newline();
                Consume(TokenType.DM_Dedent, "Expected dedent");

                return switchInner;
            }

            return null;
        }

        public DMASTProcStatementSwitch.SwitchCase[] SwitchInner() {
            List<DMASTProcStatementSwitch.SwitchCase> switchCases = new List<DMASTProcStatementSwitch.SwitchCase>();
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
                List<DMASTExpressionConstant> expressions = new List<DMASTExpressionConstant>();

                Whitespace();
                Consume(TokenType.DM_LeftParenthesis, "Expected '('");
                do {
                    Whitespace();
                    DMASTExpressionConstant expression = Expression() as DMASTExpressionConstant;
                    if (expression == null) throw new Exception("Expected a constant expression");
                    expressions.Add(expression);
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
                    } else if (assign.Expression is DMASTCallableIdentifier) {
                        return new DMASTCallParameter(assign.Value, ((DMASTCallableIdentifier)assign.Expression).Identifier);
                    }
                }
                
                return new DMASTCallParameter(expression);
            }

            return null;
        }

        public DMASTDefinitionParameter[] DefinitionParameters() {
            List<DMASTDefinitionParameter> parameters = new List<DMASTDefinitionParameter>();
            DMASTDefinitionParameter parameter = DefinitionParameter();

            while (parameter != null) {
                parameters.Add(parameter);

                if (Check(TokenType.DM_Comma)) {
                    Whitespace();
                    parameter = DefinitionParameter();

                    if (parameter == null) throw new Exception("Expected parameter definition");
                } else {
                    parameter = null;
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
                    if (expression != null && expression is not DMASTExpressionConstant) throw new Exception("Expected a constant expression");
                    Whitespace();
                    Consume(TokenType.DM_RightBracket, "Expected ']'");
                }

                DMASTExpression value = null;
                DMASTDefinitionParameter.ParameterType type;
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
                    TokenType.DM_SlashEquals
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
                        }
                    } else {
                        throw new Exception("Expected a value");
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
                if (b == null) throw new Exception("Expected an expression");
                Consume(TokenType.DM_Colon, "Expected ':'");
                Whitespace();
                DMASTExpression c = ExpressionTernary();
                if (c == null) throw new Exception("Expected an expression");

                return new DMASTTernary(a, b, c);
            }

            return a;
        }

        public DMASTExpression ExpressionOr() {
            DMASTExpression a = ExpressionAnd();

            if (a != null && Check(TokenType.DM_BarBar)) {
                Whitespace();
                DMASTExpression b = ExpressionOr();
                if (b == null) throw new Exception("Expected a second value");

                return new DMASTOr(a, b);
            }

            return a;
        }

        public DMASTExpression ExpressionAnd() {
            DMASTExpression a = ExpressionBinaryOr();

            if (a != null && Check(TokenType.DM_AndAnd)) {
                Whitespace();
                DMASTExpression b = ExpressionAnd();
                if (b == null) throw new Exception("Expected a second value");

                return new DMASTAnd(a, b);
            }

            return a;
        }

        public DMASTExpression ExpressionBinaryOr() {
            DMASTExpression a = ExpressionBinaryXor();

            if (a != null && Check(TokenType.DM_Bar)) {
                Whitespace();
                DMASTExpression b = ExpressionBinaryOr();
                if (b == null) throw new Exception("Expected an expression");

                return new DMASTBinaryOr(a, b);
            }

            return a;
        }

        public DMASTExpression ExpressionBinaryXor() {
            DMASTExpression a = ExpressionBinaryAnd();

            if (a != null && Check(TokenType.DM_Xor)) {
                Whitespace();
                DMASTExpression b = ExpressionBinaryXor();
                if (b == null) throw new Exception("Expected an expression");

                return new DMASTBinaryXor(a, b);
            }

            return a;
        }

        public DMASTExpression ExpressionBinaryAnd() {
            DMASTExpression a = ExpressionComparison();

            if (a != null && Check(TokenType.DM_And)) {
                Whitespace();
                DMASTExpression b = ExpressionBinaryAnd();

                if (b == null) throw new Exception("Expected an expression");
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

                    if (b == null) throw new Exception("Expected an expression to compare to");
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
                    if (b == null) throw new Exception("Expected an expression");

                    return new DMASTLeftShift(a, b);
                } else if (Check(TokenType.DM_RightShift)) {
                    Whitespace();
                    DMASTExpression b = ExpressionBitShift();
                    if (b == null) throw new Exception("Expected an expression");

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
                    if (b == null) throw new Exception("Expected an expression");

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
                if (Check(TokenType.DM_Plus)) {
                    Whitespace();
                    DMASTExpression b = ExpressionAdditionSubtraction();
                    if (b == null) throw new Exception("Expected value to add");

                    return new DMASTAdd(a, b);
                } else if (Check(TokenType.DM_Minus)) {
                    Whitespace();
                    DMASTExpression b = ExpressionAdditionSubtraction();
                    if (b == null) throw new Exception("Expected value to subtract");

                    return new DMASTSubtract(a, b);
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

                if (Check(types)) {
                    Whitespace();
                    DMASTExpression b = ExpressionMultiplicationDivisionModulus();
                    if (b == null) throw new Exception("Expected an expression");

                    switch (token.Type) {
                        case TokenType.DM_Star: return new DMASTMultiply(a, b);
                        case TokenType.DM_Slash: return new DMASTDivide(a, b);
                        case TokenType.DM_Modulus: return new DMASTModulus(a, b);
                    }
                }
            }

            return a;
        }

        public DMASTExpression ExpressionPower() {
            DMASTExpression a = ExpressionIn();

            if (a != null && Check(TokenType.DM_StarStar)) {
                Whitespace();
                DMASTExpression b = ExpressionPower();
                if (b == null) throw new Exception("Expected an expression");

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
                if (expression == null) throw new Exception("Expected an expression");

                return new DMASTNot(expression);
            } else if (Check(TokenType.DM_Tilde)) {
                Whitespace();
                DMASTExpression expression = ExpressionUnary();
                if (expression == null) throw new Exception("Expected an expression");

                return new DMASTBinaryNot(expression);
            } else if (Check(TokenType.DM_PlusPlus)) {
                Whitespace();
                DMASTExpression expression = ExpressionSign();
                if (expression == null) throw new Exception("Expected an expression");

                return new DMASTPreIncrement(expression);
            } else if (Check(TokenType.DM_MinusMinus)) {
                Whitespace();
                DMASTExpression expression = ExpressionSign();
                if (expression == null) throw new Exception("Expected an expression");

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
                Whitespace();
                DMASTExpression expression = ExpressionListIndex();

                if (expression == null) throw new Exception("Expected an expression");
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

            if (Check(TokenType.DM_LeftBracket)) {
                Whitespace();
                DMASTExpression index = Expression();
                Consume(TokenType.DM_RightBracket, "Expected ']'");
                Whitespace();

                return new DMASTListIndex(expression, index);
            }

            return expression;
        }

        public DMASTExpression ExpressionNew() {
            if (Check(TokenType.DM_New)) {
                Whitespace();
                DMASTCallableDereference dereference = Dereference();
                DMASTPath path = (dereference == null) ? Path() : null;
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
                    DMASTCallable callable = Callable();

                    if (callable != null) {
                        Whitespace();
                        DMASTCallParameter[] callParameters = ProcCall();

                        if (callParameters != null) {
                            DMASTCallableIdentifier identifier = callable as DMASTCallableIdentifier;
                            Whitespace();

                            if (identifier != null && identifier.Identifier == "input") {
                                DMASTDefinitionParameter.ParameterType types = AsTypes();
                                Whitespace();
                                DMASTExpression list = null;

                                if (Check(TokenType.DM_In)) {
                                    Whitespace();
                                    list = Expression();
                                }

                                return new DMASTInput(callParameters, types, list);
                            } else {
                                primary = new DMASTProcCall(callable, callParameters);
                            }
                        } else {
                            primary = callable;
                        }
                    }
                }

                if (primary == null && Check(TokenType.DM_Call)) {
                    Whitespace();
                    DMASTCallParameter[] callParameters = ProcCall();
                    if (callParameters == null || callParameters.Length < 1 || callParameters.Length > 2) throw new Exception("Call must have 2 parameters");
                    Whitespace();
                    DMASTCallParameter[] procParameters = ProcCall();
                    if (procParameters == null) throw new Exception("Expected proc parameters");

                    primary = new DMASTCall(callParameters, procParameters);
                }

                if (primary == null && Check(TokenType.DM_List)) {
                    Whitespace();
                    DMASTCallParameter[] values = ProcCall(false);

                    primary = new DMASTList(values);
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
                case TokenType.DM_String: {
                    string tokenValue = (string)constantToken.Value;
                    string stringValue = String.Empty;
                    List<DMASTExpression> interpolationValues = new List<DMASTExpression>();
                    Advance();

                    int bracketNesting = 0;
                    string insideBrackets = String.Empty;
                    StringFormatTypes currentInterpolationType = StringFormatTypes.Stringify;
                    for (int i = 0; i < tokenValue.Length; i++) {
                        char c = tokenValue[i];

                        if (bracketNesting > 0) insideBrackets += c;

                        if (c == '[') {
                            bracketNesting++;
                        } else if (c == ']' && bracketNesting > 0) {
                            bracketNesting--;

                            if (bracketNesting == 0) { //End of expression
                                DMLexer expressionLexer = new DMLexer(insideBrackets);
                                DMParser expressionParser = new DMParser(expressionLexer);

                                expressionParser.Whitespace();
                                DMASTExpression expression = expressionParser.Expression();
                                if (expression == null) throw new Exception("Expected an expression");

                                interpolationValues.Add(expression);
                                stringValue += StringFormatCharacter;
                                stringValue += (char)currentInterpolationType;
                                currentInterpolationType = StringFormatTypes.Stringify;

                                insideBrackets = String.Empty;
                            }
                        } else if (c == '\\' && bracketNesting == 0) {
                            string escapeSequence = String.Empty;

                            do {
                                c = tokenValue[++i];
                                escapeSequence += c;

                                if (escapeSequence == "[" || escapeSequence == "]") {
                                    stringValue += escapeSequence;
                                    break;
                                } else if (escapeSequence == "ref") {
                                    currentInterpolationType = StringFormatTypes.Ref;
                                    break;
                                } else if (DMLexer.ValidEscapeSequences.Contains(escapeSequence)) { //Unimplemented escape sequence
                                    break;
                                }
                            } while (c != ' ');

                            if (!DMLexer.ValidEscapeSequences.Contains(escapeSequence)) {
                                throw new Exception("Invalid escape sequence \"\\" + escapeSequence + "\"");
                            }
                        } else if (bracketNesting == 0) {
                            stringValue += c;
                        }
                    }

                    if (bracketNesting > 0) throw new Exception("Expected ']'");

                    if (interpolationValues.Count == 0) {
                        return new DMASTConstantString(stringValue);
                    } else {
                        return new DMASTStringFormat(stringValue, interpolationValues.ToArray());
                    }
                }
                default: return null;
            }
        }

        private bool Newline() {
            return Check(TokenType.Newline);
        }

        private bool Whitespace() {
            return Check(TokenType.DM_Whitespace);
        }

        private DMASTDefinitionParameter.ParameterType AsTypes() {
            DMASTDefinitionParameter.ParameterType type = DMASTDefinitionParameter.ParameterType.Default;

            if (Check(TokenType.DM_As)) {
                Whitespace();

                do {
                    Token typeToken = Current();

                    Consume(new TokenType[] { TokenType.DM_Identifier, TokenType.DM_Null }, "Expected parameter type");
                    switch (typeToken.Text) {
                        case "anything": type |= DMASTDefinitionParameter.ParameterType.Anything; break;
                        case "null": type |= DMASTDefinitionParameter.ParameterType.Null; break;
                        case "text": type |= DMASTDefinitionParameter.ParameterType.Text; break;
                        case "obj": type |= DMASTDefinitionParameter.ParameterType.Obj; break;
                        case "mob": type |= DMASTDefinitionParameter.ParameterType.Mob; break;
                        case "turf": type |= DMASTDefinitionParameter.ParameterType.Turf; break;
                        case "num": type |= DMASTDefinitionParameter.ParameterType.Num; break;
                        case "message": type |= DMASTDefinitionParameter.ParameterType.Message; break;
                        case "area": type |= DMASTDefinitionParameter.ParameterType.Area; break;
                        case "color": type |= DMASTDefinitionParameter.ParameterType.Color; break;
                        default: throw new Exception("Invalid parameter type '" + typeToken.Text + "'");
                    }
                } while (Check(TokenType.DM_Bar));
            }

            return type;
        }

        private bool Delimiter() {
            return Check(TokenType.DM_Semicolon) || Newline();
        }
    }
}
