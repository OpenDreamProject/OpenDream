using System.Collections.Generic;
using System.Text.Json.Serialization;
using OpenDreamShared.Dream;

#nullable enable

namespace DMCompiler.SpacemanDmm
{
    // Contains C# equivalents of SpacemanDMM's AST in ast.rs

    public struct Spanned<T>
    {
        public Location Location;
        public T Elem;
    }

    [RustTuple]
    public struct Block
    {
        public Spanned<Statement>[] Statements;

        public Block(Spanned<Statement>[] statements)
        {
            Statements = statements;
        }
    }

    [RustTuple]
    public struct TreePath
    {
        public string[] Parts;

        public TreePath(string[] parts)
        {
            Parts = parts;
        }

        public static explicit operator DreamPath(TreePath path)
        {
            return new DreamPath(DreamPath.PathType.Absolute, path.Parts);
        }
    }

    [RustEnum]
    public abstract record Expression
    {
        public sealed record Base(
            UnaryOp[] Unary,
            Spanned<Term> Term,
            Spanned<Follow>[] Follow) : Expression;

        public sealed record BinaryOp(
            SpacemanDmm.BinaryOp Op,
            Expression Lhs,
            Expression Rhs) : Expression;

        public sealed record AssignOp(
            SpacemanDmm.AssignOp Op,
            Expression Lhs,
            Expression Rhs) : Expression;

        public sealed record TernaryOp(
            Expression Cond,
            [property: JsonPropertyName("if")] Expression If,
            [property: JsonPropertyName("else")] Expression Else) : Expression;
    }

    public struct Pop
    {
        public TreePath Path { get; init; }
        public Dictionary<string, Constant> Vars { get; init; }
    }

    [RustEnum]
    public abstract record Constant
    {
        [RustTuple]
        public sealed record Null(TreePath? Path) : Constant;

        public sealed record New(
            [property: JsonPropertyName("type")] Pop? Type,
            (Constant, Constant?)[]? Args) : Constant;

        [RustTuple]
        public sealed record List((Constant, Constant?)[] Values) : Constant;

        [RustTuple]
        public sealed record Call(ConstFn Function, (Constant, Constant?)[] Arguments) : Constant;

        [RustTuple]
        public sealed record Prefab(Pop Value) : Constant;

        [RustTuple]
        public sealed record String(string Value) : Constant;

        [RustTuple]
        public sealed record Resource(string Value) : Constant;

        [RustTuple]
        public sealed record Int(int Value) : Constant;

        [RustTuple]
        public sealed record Float(float Value) : Constant;
    }

    [RustEnum]
    public abstract record Statement
    {
        [RustTuple]
        public sealed record Expr(Expression Expression) : Statement;

        [RustTuple]
        [JsonConverter(typeof(RustTupleConverter))]
        public sealed record Return(Expression? Expression) : Statement;

        [RustTuple]
        public sealed record Throw(Expression Expression) : Statement;

        public sealed record While(Expression Condition, Block Block) : Statement;

        public sealed record DoWhile(Block Block, Spanned<Expression> Condition) : Statement;

        public sealed record If((Spanned<Expression> Condition, Block Block)[] Arms, Block? ElseArm) : Statement;

        public sealed record ForInfinite(Block Block) : Statement;

        public sealed record ForLoop(Statement? Init, Expression? Test, Statement? Inc, Block Block) : Statement;

        public sealed record ForList(
            [property: JsonPropertyName("var_type")]
            VarType? VarType,
            string Name,
            [property: JsonPropertyName("input_type")]
            InputType? InputType,
            [property: JsonPropertyName("in_list")]
            Expression? InList,
            Block Block) : Statement;

        public sealed record ForRange(
            [property: JsonPropertyName("var_type")]
            VarType? VarType,
            string Name,
            Expression Start,
            Expression End,
            Expression? Step,
            Block Block) : Statement;

        [RustTuple]
        public sealed record Var(VarStatement Statement) : Statement;

        [RustTuple]
        public sealed record Vars(VarStatement[] Statements) : Statement;

        public sealed record Setting(string Name, SettingMode Mode, Expression Value) : Statement;

        public sealed record Spawn(Expression? Delay, Block Block) : Statement;

        public sealed record Switch(
            Expression? Input,
            (Spanned<Case[]> Case, Block Block)[] Cases,
            Block? Default) : Statement;

        public sealed record TryCatch(
            [property: JsonPropertyName("try_block")]
            Block TryBlock,
            [property: JsonPropertyName("catch_params")]
            TreePath[] CatchParams,
            [property: JsonPropertyName("catch_block")]
            Block CatchBlock) : Statement
        {
        }

        [RustTuple]
        public sealed record Continue(string? LabelName) : Statement;

        [RustTuple]
        public sealed record Break(string? LabelName) : Statement;

        [RustTuple]
        public sealed record Goto(string LabelName) : Statement;

        public sealed record Label(string Name, Block Block) : Statement;

        [RustTuple]
        public sealed record Del(Expression Expression) : Statement;

        [RustTuple]
        public sealed record Crash(Expression? Expression) : Statement;
    }

    [RustEnum]
    public abstract record Case
    {
        [RustTuple]
        public sealed record Exact(Expression Expression) : Case;

        [RustTuple]
        public sealed record Range(Expression Start, Expression End) : Case;
    }

    public sealed record VarStatement(
        [property: JsonPropertyName("var_type")]
        VarType VarType,
        string Name,
        Expression? Value)
    {
    }

    public enum SettingMode : byte
    {
        Assign,
        In
    }

    public enum ConstFn : byte
    {
        Icon,
        Matrix,
        Newlist,
        Sound,
        Filter,
        File,
        Generator
    }

    public enum UnaryOp : byte
    {
        Neg,
        Not,
        BitNot,
        PreIncr,
        PostIncr,
        PreDecr,
        PostDecr,
    }

    public enum BinaryOp : byte
    {
        Add,
        Sub,
        Mul,
        Div,
        Pow,
        Mod,
        Eq,
        NotEq,
        Less,
        Greater,
        LessEq,
        GreaterEq,
        Equiv,
        NotEquiv,
        BitAnd,
        BitXor,
        BitOr,
        LShift,
        RShift,
        And,
        Or,
        In,
        To,
    }

    public enum AssignOp : byte
    {
        Assign,
        AddAssign,
        SubAssign,
        MulAssign,
        DivAssign,
        ModAssign,
        AssignInto,
        BitAndAssign,
        AndAssign,
        BitOrAssign,
        OrAssign,
        BitXorAssign,
        LShiftAssign,
        RShiftAssign,
    }

    public enum ListAccessKind : byte
    {
        // `[]`
        Normal,

        // `?[]`
        Safe
    }

    public enum PropertyAccessKind : byte
    {
        // `a.b`
        Dot,

        // `a:b`
        Colon,

        // `a?.b`
        SafeDot,

        // `a?:b`
        SafeColon,
    }

    [RustEnum]
    public abstract record Follow
    {
        [RustTuple]
        public sealed record Index(ListAccessKind Kind, Expression Expression) : Follow;

        [RustTuple]
        public sealed record Field(PropertyAccessKind Kind, string Ident) : Follow;

        [RustTuple]
        public sealed record Call(PropertyAccessKind Kind, string Ident, Expression[] Args) : Follow;
    }

    [RustEnum]
    public abstract record Term
    {
        public sealed record Null : Term;

        [RustTuple]
        public sealed record Int(int Value) : Term;

        [RustTuple]
        public sealed record Float(float Value) : Term;

        [RustTuple]
        public sealed record Ident(string Value) : Term;

        [RustTuple]
        public sealed record String(string Value) : Term;

        [RustTuple]
        public sealed record Resource(string Value) : Term;

        [RustTuple]
        public sealed record As(InputType Type) : Term;

        [RustTuple]
        public sealed record Expr(Expression Expression) : Term;

        [RustTuple]
        public sealed record Prefab(SpacemanDmm.Prefab Value) : Term;

        [RustTuple]
        public sealed record InterpString(string Start, (Expression? Expression, string Follow)[] Parts) : Term;

        [RustTuple]
        public sealed record Call(string IdentName, Expression[] Args) : Term;

        [RustTuple]
        public sealed record SelfCall(Expression[] Args) : Term;

        [RustTuple]
        public sealed record ParentCall(Expression[] Args) : Term;

        public sealed record New(
            [property: JsonPropertyName("type")] NewType Type,
            Expression[]? Args) : Term;

        [RustTuple]
        public sealed record List(Expression[] Values) : Term;

        public sealed record Input(
            Expression[] Args,
            [property: JsonPropertyName("input_type")]
            InputType? InputType,
            [property: JsonPropertyName("in_list")]
            Expression? InList) : Term;

        public sealed record Locate(
            Expression[] Args,
            [property: JsonPropertyName("in_list")]
            Expression? InList) : Term;

        [RustTuple]
        public sealed record Pick((Expression? Weight, Expression Value)[] Entries) : Term;

        [RustTuple]
        public sealed record DynamicCall(Expression[] Left, Expression[] Right) : Term;
    }

    public enum PathOp : byte
    {
        Slash,
        Dot,
        Colon
    }

    [RustTuple]
    public struct TypePath
    {
        public (PathOp Op, string Elem)[] Path;

        public TypePath((PathOp, string)[] path)
        {
            Path = path;
        }
    }

    public struct Prefab
    {
        public TypePath Path { get; init; }
        public Dictionary<string, Expression> Vars { get; init; }
    }

    [RustEnum]
    public abstract record NewType
    {
        public sealed record Implicit : NewType;

        [RustTuple]
        public sealed record Prefab(SpacemanDmm.Prefab Value) : NewType;

        public sealed record MiniExpr : NewType
        {
            public string Ident { get; init; }
            public Field[] Fields { get; init; }
        }
    }

    public struct Field
    {
        public PropertyAccessKind Kind;
        public string Ident { get; init; }
    }
}
