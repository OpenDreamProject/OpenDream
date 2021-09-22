using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

#nullable enable

namespace DMCompiler.SpacemanDmm
{
    // Contains C# equivalents of SpacemanDMM's object tree in objtree.rs

    public sealed class Type
    {
        public string Name { get; init; }
        public string Path { get; init; }
        public Location Location;

        public Dictionary<string, TypeVar> Vars { get; init; }
        public Dictionary<string, TypeProc> Procs { get; init; }
    }

    public sealed class TypeVar
    {
        public VarValue Value { get; init; }
        public VarDeclaration? Declaration;
    }

    public sealed class TypeProc
    {
        public ProcValue[] Value { get; init; }
        public ProcDeclaration? Declaration;
    }

    public enum VarTypeFlags : byte
    {
        // DM flags
        Static = 1 << 0,
        Const = 1 << 2,
        Tmp = 1 << 3,

        // SpacemanDMM flags
        Final = 1 << 4,
        Private = 1 << 5,
        Protected = 1 << 6
    }

    public sealed class VarType
    {
        public VarTypeFlags Flags;
        [JsonPropertyName("type_path")] public TreePath TypePath { get; init; }
    }

    public sealed class VarDeclaration
    {
        [JsonPropertyName("var_type")] public VarType VarType;
        public Location Location;
        public uint Id;
    }

    public sealed class VarValue
    {
        public Location Location;
        public Expression? Expression;
        public Constant? Constant;
    }

    public sealed class ProcValue
    {
        public Location Location;
        public Parameter[] Parameters { get; init; }
        public Code Code { get; init; }
    }

    public enum ProcDeclKind : byte
    {
        Proc,
        Verb
    }

    public sealed class ProcDeclaration
    {
        public Location Location;
        public ProcDeclKind Kind;
        public uint Id;
        [JsonPropertyName("is_private")] public bool IsPrivate;
        [JsonPropertyName("is_protected")] public bool IsProtected;
    }

    public enum InputType
    {
        Mob = 1 << 0,
        Obj = 1 << 1,
        Text = 1 << 2,
        Num = 1 << 3,
        File = 1 << 4,
        Turf = 1 << 5,
        Key = 1 << 6,
        Null = 1 << 7,
        Area = 1 << 8,
        Icon = 1 << 9,
        Sound = 1 << 10,
        Message = 1 << 11,
        Anything = 1 << 12,
        Password = 1 << 15,
        CommandText = 1 << 16,
        Color = 1 << 17
    }

    public sealed class Parameter
    {
        [JsonPropertyName("var_type")] public VarType VarType { get; init; }
        public string Name { get; init; }
        public Expression? Default;
        [JsonPropertyName("input_type")] public InputType? InputType;
        [JsonPropertyName("in_list")] public Expression? InList;
        public Location Location;
    }

    [RustEnum]
    public abstract record Code
    {
        [RustTuple]
        public sealed record Present(Block Block) : Code;

        [RustTuple]
        public sealed record Invalid(DMError Error) : Code;

        public sealed record Builtin : Code;

        public sealed record Disabled : Code;
    }

    public static partial class Converters
    {
        public abstract class BitFlagConverter<T> : JsonConverter<T>
        {
            public sealed override T? Read(ref Utf8JsonReader reader, System.Type typeToConvert,
                JsonSerializerOptions options)
            {
                Debug.Assert(reader.TokenType == JsonTokenType.StartObject);
                reader.Read();
                Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);
                var name = reader.GetString()!;
                Debug.Assert(name == "bits");
                reader.Read();
                Debug.Assert(reader.TokenType == JsonTokenType.Number);
                var intVal = reader.GetInt32();

                var val = ReadValue(intVal);

                reader.Read();
                Debug.Assert(reader.TokenType == JsonTokenType.EndObject);
                return val;
            }

            protected abstract T ReadValue(int value);

            public sealed override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            {
                throw new NotSupportedException();
            }
        }

        public sealed class InputTypeConverter : BitFlagConverter<InputType>
        {
            protected override InputType ReadValue(int value) => (InputType)value;
        }

        public sealed class VarTypeFlagsConverter : BitFlagConverter<VarTypeFlags>
        {
            protected override VarTypeFlags ReadValue(int value) => (VarTypeFlags)value;
        }
    }
}
