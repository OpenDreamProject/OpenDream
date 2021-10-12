using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DMCompiler.DM;
using OpenDreamShared.Compiler;
using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;

#nullable enable

namespace DMCompiler.SpacemanDmm
{
    /// <summary>
    /// Converts SpacemanDMM's AST into that used by the compiler internally.
    /// </summary>
    internal static class AstConverter
    {
        public static void ConvertTypesToObjectTree(ParseResult result)
        {
            var types = result.GetTypeList();

            // Transfer SpacemanDMM AST into OpenDream.
            foreach (var type in types)
            {
                var typeInfo = result.GetTypeInfo(type);
                var typePath = type == "" ? DreamPath.Root : new DreamPath(type);
                var dmObject = DMObjectTree.GetDMObject(typePath);

                // Handle type vars.
                foreach (var (varName, typeVar) in typeInfo.Vars)
                {
                    var value = ConvertExpression(typeVar.Value.Expression) ?? new DMASTConstantNull();

                    if (typeVar.Declaration is { } decl)
                    {
                        var isGlobal = decl.VarType.Flags == VarTypeFlags.Static || typePath == DreamPath.Root;
                        var dmVar = new DMVariable(
                            (DreamPath)decl.VarType.TypePath,
                            varName,
                            isGlobal);

                        dmVar.ValueToEval = value;

                        if (dmVar.IsGlobal)
                            dmObject.GlobalVariables.Add(varName, dmVar);
                        else
                            dmObject.Variables.Add(varName, dmVar);
                    }
                    else
                    {
                        if (varName == "parent_type")
                        {
                            if (value is not DMASTConstantPath { Value: { Path: var path } })
                                Program.Warning(new CompilerWarning(null, "Invalid parent_type expression"));
                            else
                                dmObject.Parent = DMObjectTree.GetDMObject(path);
                        }
                        else
                        {
                            var dmVar = new DMVariable(null, varName, false);
                            dmVar.ValueToEval = value;
                            dmObject.VariableOverrides.Add(varName, dmVar);
                        }
                    }
                }

                foreach (var (procName, typeProc) in typeInfo.Procs)
                {
                    var procs = new List<DMProc>(typeProc.Value.Length);
                    dmObject.Procs.Add(procName, procs);

                    var first = true;
                    foreach (var procValue in typeProc.Value)
                    {
                        try
                        {
                            DMASTProcDefinition procDef;

                            // Convert parameters.
                            var parameters = new DMASTDefinitionParameter[procValue.Parameters.Length];
                            for (var i = 0; i < parameters.Length; i++)
                            {
                                var param = procValue.Parameters[i];
                                parameters[i] = new DMASTDefinitionParameter(
                                    param.Name,
                                    (DreamPath)param.VarType.TypePath,
                                    ConvertExpression(param.Default),
                                    (DMValueType?)param.InputType ?? default,
                                    ConvertExpression(param.InList));
                            }

                            // Block statements.
                            DMASTProcBlockInner inner;
                            switch (procValue.Code)
                            {
                                case Code.Builtin:
                                case Code.Disabled:
                                    continue;
                                case Code.Invalid:
                                    continue;
                                case Code.Present(var block):
                                    inner = ConvertBlock(block);
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }

                            bool isVerb;
                            bool isOverride;
                            if (first && typeProc.Declaration is { } decl)
                            {
                                isVerb = decl.Kind == ProcDeclKind.Verb;
                                isOverride = false;
                            }
                            else
                            {
                                isVerb = false;
                                isOverride = true;
                            }

                            procDef = new DMASTProcDefinition(
                                typePath,
                                procName,
                                isVerb,
                                isOverride,
                                parameters,
                                inner);

                            procs.Add(new DMProc(procDef));
                            first = false;
                        }
                        catch (CompileErrorException e)
                        {
                            Program.Error(e.Error);
                        }
                    }
                }
            }

            // Go through and evaluate variable initializers.
            foreach (var dmObject in DMObjectTree.AllObjects.Values)
            {
                foreach (var dmVar in dmObject.Variables.Values
                    .Union(dmObject.VariableOverrides.Values)
                    .Union(dmObject.GlobalVariables.Values))
                {
                    try
                    {
                        SetVariableValue(dmObject, dmVar, dmVar.ValueToEval, dmVar.Type);
                    }
                    catch (CompileErrorException e)
                    {
                        Program.Error(e.Error);
                    }
                }
            }
        }

        [return: NotNullIfNotNull("block")]
        private static DMASTProcBlockInner? ConvertBlock(Block? block)
        {
            if (block is not { } b)
                return null;

            var statements = new List<DMASTProcStatement>(b.Statements.Length);

            ConvertBlockCore(b, statements);

            Debug.Assert(statements.All(s => s != null));

            return new DMASTProcBlockInner(statements.ToArray());
        }

        private static void ConvertBlockCore(Block block, List<DMASTProcStatement> outStatements)
        {
            foreach (var statement in block.Statements)
            {
                outStatements.Add(ConvertStatement(statement.Elem));

                // Fold label blocks out here.
                if (statement.Elem is Statement.Label l)
                    ConvertBlockCore(l.Block, outStatements);
            }
        }

        [return: NotNullIfNotNull("statement")]
        private static DMASTProcStatement? ConvertStatement(Statement? statement)
        {
            return statement switch
            {
                null => null,
                Statement.Break @break => ConvertStatementBreak(@break),
                Statement.Continue @continue => ConvertStatementContinue(@continue),
                Statement.Crash crash => ConvertStatementCrash(crash),
                Statement.Del(var delExpr) => ConvertStatementDel(delExpr),
                Statement.DoWhile(var block, var condition) => ConvertStatementDoWhile(condition, block),
                Statement.Expr(var expr) => new DMASTProcStatementExpression(ConvertExpression(expr)),
                Statement.ForInfinite forInfinite => ConvertStatementForInfinite(forInfinite),
                Statement.ForList forList => ConvertStatementForList(forList),
                Statement.ForLoop forLoop => ConvertStatementForLoop(forLoop),
                Statement.ForRange forRange => ConvertStatementForRange(forRange),
                Statement.Goto @goto => ConvertStatementGoto(@goto),
                Statement.If @if => ConvertStatementIf(@if),
                Statement.Label label => ConvertStatementLabel(label),
                Statement.Return @return => ConvertStatementReturn(@return),
                Statement.Setting setting => ConvertStatementSetting(setting),
                Statement.Spawn spawn => ConvertStatementSpawn(spawn),
                Statement.Switch @switch => ConvertStatementSwitch(@switch),
                Statement.Throw => throw new CompileErrorException("throw is not currently supported."),
                Statement.TryCatch => throw new CompileErrorException("try/catch are not currently supported."),
                Statement.Var var => ConvertStatementVar(var),
                Statement.Vars vars => ConvertStatementVars(vars),
                Statement.While @while => ConvertStatementWhile(@while),
                _ => throw new ArgumentOutOfRangeException(nameof(statement))
            };
        }

        private static DMASTProcStatementDoWhile ConvertStatementDoWhile(Spanned<Expression> condition, Block block)
        {
            return new DMASTProcStatementDoWhile(
                ConvertExpression(condition.Elem),
                ConvertBlock(block));
        }

        private static DMASTProcStatementDel ConvertStatementDel(Expression? delExpr)
        {
            return new DMASTProcStatementDel(ConvertExpression(delExpr));
        }

        private static DMASTProcStatement ConvertStatementWhile(Statement.While @while)
        {
            return new DMASTProcStatementWhile(ConvertExpression(@while.Condition), ConvertBlock(@while.Block));
        }

        private static DMASTProcStatement ConvertStatementVars(Statement.Vars vars)
        {
            return new DMASTProcStatementMultipleVarDeclarations(vars.Statements.Select(ConvertVarStatement).ToArray());
        }

        private static DMASTProcStatement ConvertStatementVar(Statement.Var var)
        {
            return ConvertVarStatement(var.Statement);
        }

        private static DMASTProcStatementVarDeclaration ConvertVarStatement(VarStatement statement)
        {
            var (type, name, value) = statement;

            return new DMASTProcStatementVarDeclaration(name, (DreamPath?)type.TypePath, ConvertExpression(value));
        }

        private static DMASTProcStatement ConvertStatementSwitch(Statement.Switch @switch)
        {
            var cases = new List<DMASTProcStatementSwitch.SwitchCase>();
            foreach (var @case in @switch.Cases)
            {
                var values = new DMASTExpression[@case.Case.Elem.Length];
                for (var i = 0; i < values.Length; i++)
                {
                    var caseValue = @case.Case.Elem[i];
                    switch (caseValue)
                    {
                        case Case.Exact(var expr):
                            values[i] = ConvertExpression(expr);
                            break;
                        case Case.Range(var start, var end):
                            values[i] = new DMASTSwitchCaseRange(ConvertExpression(start), ConvertExpression(end));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(caseValue));
                    }
                }

                cases.Add(new DMASTProcStatementSwitch.SwitchCaseValues(values, ConvertBlock(@case.Block)));
            }

            if (@switch.Default is { } def)
                cases.Add(new DMASTProcStatementSwitch.SwitchCaseDefault(ConvertBlock(def)));

            return new DMASTProcStatementSwitch(ConvertExpression(@switch.Input), cases.ToArray());
        }

        private static DMASTProcStatement ConvertStatementSpawn(Statement.Spawn spawn)
        {
            return new DMASTProcStatementSpawn(
                ConvertExpression(spawn.Delay) ?? new DMASTConstantInteger(0),
                ConvertBlock(spawn.Block));
        }

        private static DMASTProcStatement ConvertStatementSetting(Statement.Setting setting)
        {
            // TODO: Mode not respected.
            return new DMASTProcStatementSet(setting.Name, ConvertExpression(setting.Value));
        }

        private static DMASTProcStatement ConvertStatementReturn(Statement.Return @return)
        {
            return new DMASTProcStatementReturn(ConvertExpression(@return.Expression));
        }

        private static DMASTProcStatement ConvertStatementLabel(Statement.Label label)
        {
            // NOTE: block is ignored.
            // ConvertBlock() calling ConvertStatement detects label and folds out the nested block.
            return new DMASTProcStatementLabel(label.Name);
        }

        private static DMASTProcStatement ConvertStatementIf(Statement.If @if)
        {
            // SpacemanDMM's if statement is flat for all if else arms.
            // The compiler's is not, so we need to nest arms into the else clause recursively.

            // Handle the last [else] if () ... else ... set.
            var statement = new DMASTProcStatementIf(
                ConvertExpression(@if.Arms[^1].Condition.Elem),
                ConvertBlock(@if.Arms[^1].Block),
                ConvertBlock(@if.ElseArm));

            // If there is more than one if arm, go backwards encapsulating the previous one.
            for (var i = @if.Arms.Length - 2; i >= 0; i--)
            {
                statement = new DMASTProcStatementIf(
                    ConvertExpression(@if.Arms[i].Condition.Elem),
                    ConvertBlock(@if.Arms[i].Block),
                    new DMASTProcBlockInner(new DMASTProcStatement[] { statement })
                );
            }

            return statement;
        }

        private static DMASTProcStatement ConvertStatementGoto(Statement.Goto @goto)
        {
            return new DMASTProcStatementGoto(new DMASTIdentifier(@goto.LabelName));
        }

        private static DMASTProcStatement ConvertStatementForRange(Statement.ForRange forRange)
        {
            return new DMASTProcStatementForRange(
                forRange.VarType is { } t
                    ? new DMASTProcStatementVarDeclaration(forRange.Name, (DreamPath)t.TypePath, null)
                    : null,
                new DMASTIdentifier(forRange.Name),
                ConvertExpression(forRange.Start),
                ConvertExpression(forRange.End),
                ConvertExpression(forRange.Step) ?? new DMASTConstantInteger(1),
                ConvertBlock(forRange.Block)
            );
        }

        private static DMASTProcStatement ConvertStatementForLoop(Statement.ForLoop forLoop)
        {
            var expr = forLoop.Inc switch
            {
                null => null,
                Statement.Expr { Expression: var e } => e,
                _ => throw new CompileErrorException("Cannot use non-expressions as for loop increment")
            };

            return new DMASTProcStatementForStandard(
                ConvertStatement(forLoop.Init),
                ConvertExpression(forLoop.Test),
                ConvertExpression(expr),
                ConvertBlock(forLoop.Block)
            );
        }

        private static DMASTProcStatement ConvertStatementForList(Statement.ForList forList)
        {
            return new DMASTProcStatementForList(
                forList.VarType is { } t
                    ? new DMASTProcStatementVarDeclaration(forList.Name, (DreamPath)t.TypePath, null)
                    : null,
                new DMASTIdentifier(forList.Name),
                ConvertExpression(forList.InList),
                ConvertBlock(forList.Block)
            );
        }

        private static DMASTProcStatement ConvertStatementForInfinite(Statement.ForInfinite forInfinite)
        {
            // Infinite for loop compiled as while(1) right now.
            // TODO: Dedicated AST node.
            return new DMASTProcStatementWhile(new DMASTConstantInteger(1), ConvertBlock(forInfinite.Block));
        }

        private static DMASTProcStatement ConvertStatementCrash(Statement.Crash crash)
        {
            return new DMASTProcStatementExpression(
                new DMASTProcCall(
                    new DMASTCallableProcIdentifier("CRASH"),
                    ConvertCallArgs(crash.Expression == null
                        ? Array.Empty<Expression>()
                        : new[] { crash.Expression })));
        }

        private static DMASTProcStatement ConvertStatementContinue(Statement.Continue @continue)
        {
            if (@continue.LabelName != null)
                Program.Error(new CompilerError(null, "Label continue not implemented"));

            return new DMASTProcStatementContinue();
        }

        private static DMASTProcStatement ConvertStatementBreak(Statement.Break @break)
        {
            if (@break.LabelName != null)
                Program.Error(new CompilerError(null, "Label break not implemented"));

            return new DMASTProcStatementBreak();
        }

        private static void SetVariableValue(DMObject dmObject, DMVariable variable, DMASTExpression value,
            DreamPath? type)
        {
            var expression = DMExpression.Create(dmObject,
                variable.IsGlobal ? DMObjectTree.GlobalInitProc : null, value, type);

            switch (expression)
            {
                case DM.Expressions.List:
                case DM.Expressions.NewPath:
                    variable.Value = new DM.Expressions.Null();
                    EmitInitializationAssign(dmObject, variable, expression);
                    break;
                case DM.Expressions.StringFormat:
                case DM.Expressions.ProcCall:
                    if (!variable.IsGlobal)
                        throw new CompileErrorException($"Invalid initial value for \"{variable.Name}\"");

                    variable.Value = new DM.Expressions.Null();
                    EmitInitializationAssign(dmObject, variable, expression);
                    break;
                default:
                    try
                    {
                        variable.Value = expression.ToConstant();
                    }
                    catch (CompileErrorException e)
                    {
                        Program.Error(e.Error);
                    }

                    break;
            }
        }

        private static void EmitInitializationAssign(DMObject dmObject, DMVariable variable, DMExpression expression)
        {
            var field = new DM.Expressions.Field(variable.Type, variable.Name);
            var assign = new DM.Expressions.Assignment(field, expression);

            if (variable.IsGlobal)
            {
                DMObjectTree.AddGlobalInitProcAssign(assign);
            }
            else
            {
                dmObject.InitializationProcExpressions.Add(assign);
            }
        }

        [return: NotNullIfNotNull("expression")]
        private static DMASTExpression? ConvertExpression(Expression? expression)
        {
            return expression switch
            {
                null => null,
                Expression.Base @base => ConvertExpressionBase(@base),
                Expression.BinaryOp binaryOp => ConvertExpressionBinaryOp(binaryOp),
                Expression.TernaryOp ternaryOp => ConvertExpressionTernaryOp(ternaryOp),
                Expression.AssignOp assignOp => ConvertExpressionAssignOp(assignOp),
                _ => throw new ArgumentOutOfRangeException(nameof(expression))
            };
        }

        private static DMASTExpression ConvertExpressionAssignOp(Expression.AssignOp assignOp)
        {
            var lhs = ConvertExpression(assignOp.Lhs);
            var rhs = ConvertExpression(assignOp.Rhs);

            return assignOp.Op switch
            {
                AssignOp.Assign => new DMASTAssign(lhs, rhs),
                AssignOp.AddAssign => new DMASTAppend(lhs, rhs),
                AssignOp.SubAssign => new DMASTRemove(lhs, rhs),
                AssignOp.MulAssign => new DMASTMultiplyAssign(lhs, rhs),
                AssignOp.DivAssign => new DMASTDivideAssign(lhs, rhs),
                AssignOp.ModAssign => new DMASTModulusAssign(lhs, rhs),
                AssignOp.AssignInto => throw new CompileErrorException(":= is not yet supported"),
                AssignOp.BitAndAssign => new DMASTMask(lhs, rhs),
                AssignOp.AndAssign => throw new CompileErrorException("&&= is not yet supported"),
                AssignOp.BitOrAssign => new DMASTCombine(lhs, rhs),
                AssignOp.OrAssign => throw new CompileErrorException("||= is not yet supported"),
                AssignOp.BitXorAssign => new DMASTXorAssign(lhs, rhs),
                AssignOp.LShiftAssign => new DMASTLeftShiftAssign(lhs, rhs),
                AssignOp.RShiftAssign => new DMASTRightShiftAssign(lhs, rhs),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static DMASTExpression ConvertExpressionTernaryOp(Expression.TernaryOp ternaryOp)
        {
            return new DMASTTernary(
                ConvertExpression(ternaryOp.Cond),
                ConvertExpression(ternaryOp.If),
                ConvertExpression(ternaryOp.Else));
        }

        private static DMASTExpression ConvertExpressionBinaryOp(Expression.BinaryOp binaryOp)
        {
            var lhs = ConvertExpression(binaryOp.Lhs);
            var rhs = ConvertExpression(binaryOp.Rhs);

            return binaryOp.Op switch
            {
                BinaryOp.Add => new DMASTAdd(lhs, rhs),
                BinaryOp.Sub => new DMASTSubtract(lhs, rhs),
                BinaryOp.Mul => new DMASTMultiply(lhs, rhs),
                BinaryOp.Div => new DMASTDivide(lhs, rhs),
                BinaryOp.Pow => new DMASTPower(lhs, rhs),
                BinaryOp.Mod => new DMASTModulus(lhs, rhs),
                BinaryOp.Eq => new DMASTEqual(lhs, rhs),
                BinaryOp.NotEq => new DMASTNotEqual(lhs, rhs),
                BinaryOp.Less => new DMASTLessThan(lhs, rhs),
                BinaryOp.Greater => new DMASTGreaterThan(lhs, rhs),
                BinaryOp.LessEq => new DMASTLessThanOrEqual(lhs, rhs),
                BinaryOp.GreaterEq => new DMASTGreaterThanOrEqual(lhs, rhs),
                BinaryOp.Equiv => new DMASTEquivalent(lhs, rhs),
                BinaryOp.NotEquiv => new DMASTNotEquivalent(lhs, rhs),
                BinaryOp.BitAnd => new DMASTBinaryAnd(lhs, rhs),
                BinaryOp.BitXor => new DMASTBinaryXor(lhs, rhs),
                BinaryOp.BitOr => new DMASTBinaryOr(lhs, rhs),
                BinaryOp.LShift => new DMASTLeftShift(lhs, rhs),
                BinaryOp.RShift => new DMASTRightShift(lhs, rhs),
                BinaryOp.And => new DMASTAnd(lhs, rhs),
                BinaryOp.Or => new DMASTOr(lhs, rhs),
                BinaryOp.In => new DMASTExpressionIn(lhs, rhs),
                BinaryOp.To => throw new CompileErrorException("to operator not yet supported"),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static DMASTExpression ConvertExpressionBase(Expression.Base expressionBase)
        {
            // Term
            var ast = ConvertTerm(expressionBase.Term.Elem);

            // Follows
            foreach (var spannedFollow in expressionBase.Follow)
            {
                ast = spannedFollow.Elem switch
                {
                    Follow.Call call => ConvertFollowCall(call, ast),
                    Follow.Field field => ConvertFollowField(field, ast),
                    Follow.Index index => ConvertFollowIndex(index, ast),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            // Unary operators
            for (var i = expressionBase.Unary.Length - 1; i >= 0; i--)
            {
                var op = expressionBase.Unary[i];
                ast = op switch
                {
                    UnaryOp.Neg => new DMASTNegate(ast),
                    UnaryOp.Not => new DMASTNot(ast),
                    UnaryOp.BitNot => new DMASTBinaryNot(ast),
                    UnaryOp.PreIncr => new DMASTPreIncrement(ast),
                    UnaryOp.PostIncr => new DMASTPostDecrement(ast),
                    UnaryOp.PreDecr => new DMASTPreDecrement(ast),
                    UnaryOp.PostDecr => new DMASTPostDecrement(ast),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            return ast;
        }

        private static DMASTExpression ConvertTerm(Term term)
        {
            switch (term)
            {
                case Term.Call call:
                    return ConvertTermCall(call);

                // TODO: Make an AST node for this.
                case Term.As(var type):
                    return new DMASTConstantInteger((int)type);

                case Term.DynamicCall(var left, var right):
                    return new DMASTCall(ConvertCallArgs(left), ConvertCallArgs(right));

                case Term.Expr(var expr):
                    return ConvertExpression(expr);

                case Term.Float(var f):
                    return new DMASTConstantFloat(f);

                case Term.Ident(var identName):
                    if (identName == ".")
                        return new DMASTCallableSelf();

                    return new DMASTIdentifier(identName);

                case Term.Input input:
                    return new DMASTInput(
                        ConvertCallArgs(input.Args),
                        (DMValueType?)input.InputType ?? DMValueType.Text,
                        ConvertExpression(input.InList));

                case Term.Int(var i):
                    return new DMASTConstantInteger(i);

                case Term.InterpString interpString:
                    // TODO: Impl
                    return new DMASTConstantString(interpString.Start);

                case Term.List(var values):
                    return new DMASTList(ConvertCallArgs(values));

                case Term.Locate locate:
                    if (locate.Args.Length == 3)
                    {
                        return new DMASTLocateCoordinates(
                            ConvertExpression(locate.Args[0]),
                            ConvertExpression(locate.Args[1]),
                            ConvertExpression(locate.Args[2]));
                    }

                    var container = locate.InList;
                    Expression? locateType = null;

                    if (locate.Args.Length == 2)
                    {
                        locateType = locate.Args[0];
                        container = locate.Args[1];
                    }
                    else if (locate.Args.Length == 1)
                    {
                        locateType = locate.Args[0];
                    }

                    return new DMASTLocate(ConvertExpression(locateType), ConvertExpression(container));

                case Term.New(var newType, var args):
                    var argsConverted = args == null ? Array.Empty<DMASTCallParameter>() : ConvertCallArgs(args);

                    switch (newType)
                    {
                        case NewType.Implicit:
                            return new DMASTNewInferred(argsConverted);
                        case NewType.MiniExpr miniExpr:
                            DMASTExpression expr = new DMASTIdentifier(miniExpr.Ident);
                            if (miniExpr.Fields.Length == 0) {
                                return new DMASTNewIdentifier(expr as DMASTIdentifier, argsConverted);
                            }
                            for (int i = 0; i < miniExpr.Fields.Length; i++) {
                                var (conditional, type) = ConvertPropertyAccessKind(miniExpr.Fields[i].Kind);
                                expr = new DMASTDereference(expr, miniExpr.Fields[i].Ident, type, conditional);
                            }
                            return new DMASTNewDereference(expr as DMASTDereference, argsConverted);
                        case NewType.Prefab(var prefab):
                            var prefabPath = ConvertPrefab(prefab);
                            if (prefabPath is not DMASTConstantPath { Value: var pathValue })
                                throw new CompileErrorException("Complex path lookup not supported in new()");

                            return new DMASTNewPath(pathValue, argsConverted);
                        default:
                            throw new ArgumentOutOfRangeException(nameof(newType));
                    }

                case Term.Null:
                    return new DMASTConstantNull();

                case Term.ParentCall(var args):
                    return new DMASTProcCall(new DMASTCallableSuper(), ConvertCallArgs(args));

                case Term.Pick(var values):
                    return new DMASTPick(values.Select(v =>
                        new DMASTPick.PickValue(ConvertExpression(v.Weight), ConvertExpression(v.Value))).ToArray());

                case Term.Prefab(var prefab):
                    return ConvertPrefab(prefab);

                case Term.Resource(var value):
                    return new DMASTConstantResource(value);

                case Term.SelfCall(var args):
                    return new DMASTProcCall(new DMASTCallableSelf(), ConvertCallArgs(args));

                case Term.String(var value):
                    return new DMASTConstantString(value);

                default:
                    throw new ArgumentOutOfRangeException(nameof(term));
            }
        }

        private static DMASTExpression ConvertTermCall(Term.Call call)
        {
            if (call.IdentName == "istype")
            {
                if (call.Args.Length == 1)
                    return new DMASTImplicitIsType(ConvertExpression(call.Args[0]));

                if (call.Args.Length == 2)
                    return new DMASTIsType(ConvertExpression(call.Args[0]), ConvertExpression(call.Args[1]));

                throw new CompileErrorException("istype() requires 1 or 2 arguments");
            }

            return new DMASTProcCall(
                new DMASTCallableProcIdentifier(call.IdentName),
                ConvertCallArgs(call.Args));
        }

        private static DMASTExpression ConvertPrefab(Prefab prefab)
        {
            if (prefab.Vars.Count != 0)
                Program.Warning(new CompilerWarning(null, "Constructed types are unsupported"));

            return ConvertTypePath(prefab.Path);
        }

        private static DMASTExpression ConvertTypePath(TypePath path)
        {
            // Convert the path into "runs" connected by /.
            // so "/a/b.proc/c" turns into "/a/b, .proc/c"
            var runs = new List<(PathOp op, string[] elems)>();
            for (var i = 0; i < path.Path.Length;)
            {
                var (opStart, elemStart) = path.Path[i];
                var run = new List<string> { elemStart };

                i++;

                for (; i < path.Path.Length; i++)
                {
                    var (op, elem) = path.Path[i];
                    if (op == PathOp.Slash)
                        run.Add(elem);
                    else
                        break;
                }

                runs.Add((opStart, run.ToArray()));
            }

            var pathType = runs[0].op switch
            {
                PathOp.Slash => DreamPath.PathType.Absolute,
                PathOp.Dot => DreamPath.PathType.DownwardSearch,
                PathOp.Colon => DreamPath.PathType.UpwardSearch,
                _ => throw new ArgumentOutOfRangeException()
            };

            DMASTExpression primary = new DMASTConstantPath(new DMASTPath(new DreamPath(pathType, runs[0].elems)));

            for (var i = 1; i < runs.Count; i++)
            {
                var (op, elems) = runs[i];
                if (op != PathOp.Dot)
                    throw new CompileErrorException("Unable to handle complex path with upwards search");

                var relPath = new DreamPath(DreamPath.PathType.Relative, elems);
                primary = new DMASTUpwardPathSearch((DMASTExpressionConstant)primary, new DMASTPath(relPath));
            }

            return primary;
        }

        private static DMASTExpression ConvertFollowCall(Follow.Call call, DMASTExpression from)
        {
            var (conditional, type) = ConvertPropertyAccessKind(call.Kind);
            return new DMASTProcCall(
                new DMASTDereferenceProc(from, call.Ident, type, conditional),
                ConvertCallArgs(call.Args)
            );
        }

        private static DMASTExpression ConvertFollowField(Follow.Field field, DMASTExpression from)
        {
            var (conditional, type) = ConvertPropertyAccessKind(field.Kind);
            return new DMASTDereference(from, field.Ident, type, conditional);
        }

        private static DMASTExpression ConvertFollowIndex(Follow.Index index, DMASTExpression from)
        {
            var conditional = index.Kind == ListAccessKind.Safe;

            if (conditional)
                Program.Warning(new CompilerWarning(null, "null-coalescing list syntax (?[]) not implemented."));

            return new DMASTListIndex(from, ConvertExpression(index.Expression));
        }

        private static DMASTCallParameter[] ConvertCallArgs(Expression[] inputArgs)
        {
            var arguments = new DMASTCallParameter[inputArgs.Length];
            for (var i = 0; i < arguments.Length; i++)
            {
                var arg = inputArgs[i];
                if (arg is Expression.AssignOp
                {
                    Op: AssignOp.Assign,
                    Lhs: Expression.Base
                    {
                        Follow: { Length: 0 },
                        Unary: { Length: 0 },
                        Term: { Elem: (Term.Ident or Term.String) and var paramTerm}
                    },
                    Rhs: var rhs
                })
                {
                    var paramName = paramTerm switch
                    {
                        Term.Ident ident => ident.Value,
                        Term.String str => str.Value,
                        _ => throw new InvalidOperationException()
                    };

                    // named parameter syntax.
                    arguments[i] = new DMASTCallParameter(ConvertExpression(rhs), paramName);
                }
                else
                {
                    arguments[i] = new DMASTCallParameter(ConvertExpression(arg));
                }
            }

            return arguments;
        }

        private static (bool conditional, DMASTDereference.DereferenceType) ConvertPropertyAccessKind(
            PropertyAccessKind kind)
        {
            return kind switch
            {
                PropertyAccessKind.Dot => (false, DMASTDereference.DereferenceType.Direct),
                PropertyAccessKind.Colon => (false, DMASTDereference.DereferenceType.Search),
                PropertyAccessKind.SafeDot => (true, DMASTDereference.DereferenceType.Direct),
                PropertyAccessKind.SafeColon => (true, DMASTDereference.DereferenceType.Search),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
