using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using static DMCompiler.SpacemanDmm.SpacemanDmmApi;

namespace DMCompiler.SpacemanDmm
{
    internal sealed unsafe class ParseResult : IDisposable
    {
        private readonly ParseResultRaw* _ptr;
        private bool _disposed;

        private ParseResult(ParseResultRaw* ptr)
        {
            _ptr = ptr;
        }

        public static ParseResult Parse(string[] files)
        {
            Span<nint> filesPtr = stackalloc nint[files.Length];
            for (var i = 0; i < files.Length; i++)
            {
                filesPtr[i] = Marshal.StringToCoTaskMemUTF8(files[i]);
            }

            ParseResultRaw* result;
            fixed (nint* ptr = filesPtr)
            {
                result = sdmm_parse(files.Length, (byte**)ptr);
            }

            foreach (var ptr in filesPtr)
            {
                Marshal.FreeCoTaskMem(ptr);
            }

            return new ParseResult(result);
        }

        public string[] GetFileList()
        {
            return ParseJson<string[]>(sdmm_result_get_file_list(_ptr));
        }

        public string[] GetTypeList()
        {
            return ParseJson<string[]>(sdmm_result_get_type_list(_ptr));
        }

        public DMError[] GetDiagnostics()
        {
            return ParseJson<DMError[]>(sdmm_result_get_diagnostics(_ptr));
        }

        public SpecialFiles GetSpecialFiles()
        {
            return ParseJson<SpecialFiles>(sdmm_result_get_special_files(_ptr));
        }

        public Type GetTypeInfo(string path)
        {
            return ParseJson<Type>(sdmm_result_get_type_info(_ptr, path));
        }

        private static T ParseJson<T>(byte* output, bool print = false)
        {
            var span = new ReadOnlySpan<byte>(output, strlen(output));
            if (print)
                Console.WriteLine(Encoding.UTF8.GetString(span));
            return JsonSerializer.Deserialize<T>(span, SerializerOptions);
        }

        // TODO: use CreateReadOnlySpanFromNullTerminated instead in .NET 6.
        public static int strlen(byte* s)
        {
            var c = 0;
            while (s[c] != 0)
                c += 1;

            return c;
        }

        private void ReleaseUnmanagedResources()
        {
            if (_disposed)
                return;

            _disposed = true;
            sdmm_result_free(_ptr);
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~ParseResult()
        {
            ReleaseUnmanagedResources();
        }
    }
}
