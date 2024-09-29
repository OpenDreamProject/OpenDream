﻿// ReSharper disable InconsistentNaming

using System.Runtime.CompilerServices;

namespace DMCompiler.Compiler;

// Must be : byte for ReadOnlySpan<TokenType> x = new TokenType[] { } to be intrinsic'd by the compiler.
public enum TokenType : byte {
    //Base lexer
    Error,
    Warning,
    Unknown,
    Skip, //Internally skipped by the lexer

    //Text lexer
    Newline,
    EndOfFile,

    //DM Preprocessor
    DM_Preproc_ConstantString,
    DM_Preproc_Define,
    DM_Preproc_Else,
    DM_Preproc_EndIf,
    DM_Preproc_Error,
    DM_Preproc_Identifier,
    DM_Preproc_If,
    DM_Preproc_Ifdef,
    DM_Preproc_Ifndef,
    DM_Preproc_Elif,
    DM_Preproc_Include,
    DM_Preproc_LineSplice,
    DM_Preproc_Number,
    DM_Preproc_ParameterStringify,
    DM_Preproc_Pragma,
    DM_Preproc_Punctuator,
    DM_Preproc_Punctuator_Colon,
    DM_Preproc_Punctuator_Comma,
    DM_Preproc_Punctuator_LeftBracket,
    DM_Preproc_Punctuator_LeftParenthesis,
    DM_Preproc_Punctuator_Period,
    DM_Preproc_Punctuator_Question,
    DM_Preproc_Punctuator_RightBracket,
    DM_Preproc_Punctuator_RightParenthesis,
    DM_Preproc_Punctuator_Semicolon,
    DM_Preproc_StringBegin,
    DM_Preproc_StringMiddle,
    DM_Preproc_StringEnd,
    DM_Preproc_TokenConcat,
    DM_Preproc_Undefine,
    DM_Preproc_Warning,
    DM_Preproc_Whitespace,

    //DM
    DM_And,
    DM_AndAnd,
    DM_AndEquals,
    DM_AndAndEquals,
    DM_As,
    DM_AssignInto,
    DM_Bar,
    DM_BarBar,
    DM_BarEquals,
    DM_BarBarEquals,
    DM_Break,
    DM_Call,
    DM_Catch,
    DM_Colon,
    DM_Comma,
    DM_ConstantString,
    DM_Continue,
    DM_Dedent,
    DM_Del,
    DM_Do,
    DM_DoubleColon,
    DM_DoubleSquareBracket,
    DM_DoubleSquareBracketEquals,
    DM_Else,
    DM_Equals,
    DM_EqualsEquals,
    DM_Exclamation,
    DM_ExclamationEquals,
    DM_Float,
    DM_For,
    DM_Goto,
    DM_GreaterThan,
    DM_GreaterThanEquals,
    DM_Identifier,
    DM_If,
    DM_In,
    DM_Indent,
    DM_IndeterminateArgs,
    DM_RightShift,
    DM_RightShiftEquals,
    DM_Integer,
    DM_LeftBracket,
    DM_LeftCurlyBracket,
    DM_LeftParenthesis,
    DM_LeftShift,
    DM_LeftShiftEquals,
    DM_LessThan,
    DM_LessThanEquals,
    DM_Minus,
    DM_MinusEquals,
    DM_MinusMinus,
    DM_Modulus,
    DM_ModulusEquals,
    DM_ModulusModulus,
    DM_ModulusModulusEquals,
    DM_New,
    DM_Null,
    DM_Period,
    DM_Plus,
    DM_PlusEquals,
    DM_PlusPlus,
    DM_Proc,
    DM_Question,
    DM_QuestionColon,
    DM_QuestionLeftBracket,
    DM_QuestionPeriod,
    DM_RawString,
    DM_Resource,
    DM_Return,
    DM_RightBracket,
    DM_RightCurlyBracket,
    DM_RightParenthesis,
    DM_Semicolon,
    DM_Set,
    DM_Slash,
    DM_SlashEquals,
    DM_Spawn,
    DM_Star,
    DM_StarEquals,
    DM_StarStar,
    DM_Step,
    DM_StringBegin,
    DM_StringMiddle,
    DM_StringEnd,
    DM_SuperProc,
    DM_Switch,
    DM_Throw,
    DM_Tilde,
    DM_TildeEquals,
    DM_TildeExclamation,
    DM_To,
    DM_Try,
    DM_Var,
    DM_While,
    DM_Whitespace,
    DM_Xor,
    DM_XorEquals,

    NTSL_Add,
    NTSL_Comma,
    NTSL_Def,
    NTSL_EndFile,
    NTSL_Equals,
    NTSL_Identifier,
    NTSL_LeftCurlyBracket,
    NTSL_LeftParenthesis,
    NTSL_Number,
    NTSL_Return,
    NTSL_RightCurlyBracket,
    NTSL_RightParenthesis,
    NTSL_Semicolon,
    NTSL_StartFile,
    NTSL_String,
    NTSL_VarIdentifierPrefix
}

public struct Token(TokenType type, string text, Location location, object? value) {
    public readonly TokenType Type = type;
    public Location Location = location;
    public readonly object? Value = value;

    /// <remarks> Use <see cref="PrintableText"/> if you intend to show this to the user.</remarks>
    public readonly string Text = text;

    public string PrintableText => Text.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ValueAsInt() {
        if (Value is not int intValue)
            throw new Exception("Token value was not an int");

        return intValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ValueAsFloat() {
        if (Value is not float floatValue)
            throw new Exception("Token value was not a float");

        return floatValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ValueAsString() {
        if (Value is not string strValue)
            throw new Exception("Token value was not a string");

        return strValue;
    }

    public override string ToString() {
        return $"{Type}({Location.ToString()}, {PrintableText})";
    }
}
