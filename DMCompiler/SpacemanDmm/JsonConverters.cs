using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

#nullable enable

namespace DMCompiler.SpacemanDmm
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    internal sealed class RustTupleAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    internal sealed class RustEnumAttribute : Attribute
    {
    }

    public sealed class RustEnumConverter : JsonConverterFactory
    {
        public override bool CanConvert(System.Type typeToConvert)
        {
            return Attribute.IsDefined(typeToConvert, typeof(RustEnumAttribute));
        }

        public override JsonConverter? CreateConverter(System.Type typeToConvert, JsonSerializerOptions options)
        {
            return (JsonConverter?)Activator.CreateInstance(typeof(Impl<>).MakeGenericType(typeToConvert));
        }

        private sealed class Impl<T> : JsonConverter<T>
        {
            // ReSharper disable once StaticMemberInGenericType
            private static readonly ImmutableDictionary<string, System.Type> Variants;

            static Impl()
            {
                Variants = typeof(T).GetNestedTypes()
                    .Where(nested => nested.IsAssignableTo(typeof(T)))
                    .ToImmutableDictionary(nested => nested.Name);
            }

            public override T? Read(ref Utf8JsonReader reader, System.Type typeToConvert,
                JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    // Assume this is an enum variant with no data like Term::Null.
                    return (T?)Activator.CreateInstance(Variants[reader.GetString()!]);
                }

                Debug.Assert(reader.TokenType == JsonTokenType.StartObject);
                reader.Read();
                Debug.Assert(reader.TokenType == JsonTokenType.PropertyName);
                var name = reader.GetString()!;

                if (!Variants.TryGetValue(name, out var variantType))
                    throw new JsonException($"Invalid enum variant name: {variantType}");

                reader.Read();
                var val = JsonSerializer.Deserialize(ref reader, variantType, options);

                reader.Read();
                Debug.Assert(reader.TokenType == JsonTokenType.EndObject);
                return (T?)val;
            }

            public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
            {
                throw new NotSupportedException();
            }
        }
    }

    public sealed class RustTupleConverter : JsonConverterFactory
    {
        public override bool CanConvert(System.Type typeToConvert)
        {
            if (typeToConvert.Name is "ValueTuple`2" or "ValueTuple`3" or "ValueTuple`4" or "ValueTuple`5")
                return true;

            return Attribute.IsDefined(typeToConvert, typeof(RustTupleAttribute));
        }

        public override JsonConverter? CreateConverter(System.Type typeToConvert, JsonSerializerOptions options)
        {
            return (JsonConverter?)Activator.CreateInstance(typeof(Impl<>).MakeGenericType(typeToConvert));
        }

        private sealed class Impl<T> : JsonConverter<T>
        {
            // ReSharper disable once StaticMemberInGenericType
            private static readonly ConstructorInfo Constructor;

            static Impl()
            {
                Constructor = typeof(T).GetConstructors().First(c =>
                {
                    var p = c.GetParameters();
                    return p.Length != 1 || p[0].ParameterType != typeof(T);
                });
            }

            public override bool HandleNull => true;

            public override T? Read(ref Utf8JsonReader reader, System.Type typeToConvert,
                JsonSerializerOptions options)
            {
                var parameters = Constructor.GetParameters();
                var arguments = new object?[parameters.Length];
                if (parameters.Length == 1)
                {
                    arguments[0] = JsonSerializer.Deserialize(ref reader, parameters[0].ParameterType, options);
                }
                else
                {
                    Debug.Assert(reader.TokenType == JsonTokenType.StartArray);

                    for (var i = 0; i < parameters.Length; i++)
                    {
                        reader.Read();
                        arguments[i] = JsonSerializer.Deserialize(ref reader, parameters[i].ParameterType, options);
                    }

                    reader.Read();
                    Debug.Assert(reader.TokenType == JsonTokenType.EndArray);
                }

                return (T?)Constructor.Invoke(arguments);
            }

            public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            {
                throw new NotSupportedException();
            }
        }
    }
}
