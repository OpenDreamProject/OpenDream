using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

#pragma warning disable CA2101

namespace DMCompiler.SpacemanDmm
{
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    internal static unsafe class SpacemanDmmApi
    {
        public static readonly JsonSerializerOptions SerializerOptions = new()
        {
            IncludeFields = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            Converters =
            {
                new RustEnumConverter(),
                new RustTupleConverter(),
                new Converters.InputTypeConverter(),
                new Converters.VarTypeFlagsConverter(),
                new JsonStringEnumConverter(),
            },
            MaxDepth = 256
        };

        private const string LibName = "sdmm_opendream.dll";

        [DllImport(LibName)]
        public static extern ParseResultRaw* sdmm_parse(int filesCount, byte** files);

        [DllImport(LibName)]
        public static extern void sdmm_result_free(ParseResultRaw* result);

        [DllImport(LibName)]
        public static extern byte* sdmm_result_get_diagnostics(ParseResultRaw* result);

        [DllImport(LibName)]
        public static extern byte* sdmm_result_get_special_files(ParseResultRaw* result);

        [DllImport(LibName)]
        public static extern byte* sdmm_result_get_file_list(ParseResultRaw* result);

        [DllImport(LibName)]
        public static extern byte* sdmm_result_get_type_list(ParseResultRaw* result);

        [DllImport(LibName, BestFitMapping = false)]
        public static extern byte* sdmm_result_get_type_info(
            ParseResultRaw* result,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string path);

        public struct ParseResultRaw
        {

        }
    }
}
