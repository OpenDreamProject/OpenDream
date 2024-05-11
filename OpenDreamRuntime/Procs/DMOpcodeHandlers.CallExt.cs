using System.Runtime.InteropServices;
using DMCompiler.Bytecode;

namespace OpenDreamRuntime.Procs;

internal static partial class DMOpcodeHandlers {
    private static ProcStatus CallExt(
        DMProcState state,
        DreamValue source,
        (DMCallArgumentsType Type, int StackSize) argumentsInfo) {
        if(!source.TryGetValueAsString(out var dllName))
            throw new Exception($"{source} is not a valid DLL");

        var popProc = state.Pop();
        if(!popProc.TryGetValueAsString(out var procName)) {
            throw new Exception($"{popProc} is not a valid proc name");
        }

        DreamProcArguments arguments = state.PopProcArguments(null, argumentsInfo.Type, argumentsInfo.StackSize);

        if (procName.StartsWith("byond:")) {
            return CallExtByond(state, dllName, procName, arguments);
        } else {
            return CallExtString(state, dllName, procName, arguments);
        }
    }

    private static unsafe ProcStatus CallExtByond(
        DMProcState state,
        string dllName,
        string procName,
        DreamProcArguments arguments) {
        // TODO: Don't allocate string copy
        // TODO: Handle stdcall (do we care?)
        var entryPoint = (delegate* unmanaged[Cdecl]<uint, ByondApi.CByondValue*, ByondApi.CByondValue>)
            DllHelper.ResolveDllTarget(state.Proc.DreamResourceManager, dllName, procName["byond:".Length..]);

        Span<ByondApi.CByondValue> args = stackalloc ByondApi.CByondValue[arguments.Count];
        args.Clear();

        for (var i = 0; i < args.Length; i++) {
            var arg = arguments.GetArgument(i);
            args[i] = ByondApi.ValueToByondApi(arg);
        }

        ByondApi.CByondValue result;
        fixed (ByondApi.CByondValue* argV = args) {
            result = entryPoint((uint)arguments.Count, argV);
        }

        state.Push(ByondApi.ValueFromDreamApi(result));
        return ProcStatus.Continue;
    }

    private static unsafe ProcStatus CallExtString(
        DMProcState state,
        string dllName,
        string procName,
        DreamProcArguments arguments) {
        var entryPoint = DllHelper.ResolveDllTarget(state.Proc.DreamResourceManager, dllName, procName);

        Span<nint> argV = stackalloc nint[arguments.Count];
        argV.Fill(0);
        try {
            for (var i = 0; i < argV.Length; i++) {
                var arg = arguments.GetArgument(i).Stringify();
                argV[i] = Marshal.StringToCoTaskMemUTF8(arg);
            }

            byte* ret;
            if (arguments.Count > 0) {
                fixed (nint* ptr = &argV[0]) {
                    ret = entryPoint(arguments.Count, (byte**)ptr);
                }
            } else {
                ret = entryPoint(0, (byte**)0);
            }

            if (ret == null) {
                state.Push(DreamValue.Null);
                return ProcStatus.Continue;
            }

            var retString = Marshal.PtrToStringUTF8((nint)ret);
            state.Push(new DreamValue(retString));
            return ProcStatus.Continue;
        } finally {
            foreach (var arg in argV) {
                if (arg != 0)
                    Marshal.ZeroFreeCoTaskMemUTF8(arg);
            }
        }
    }
}
