using System.Threading.Tasks;
using OpenDreamRuntime.Objects.Types;

namespace OpenDreamRuntime.Procs.Native;

internal static class DreamProcNativeClient {
    [DreamProc("SoundQuery")]
    public static async Task<DreamValue> NativeProc_SoundQuery(AsyncNativeProc.State state) {
        var client = (DreamObjectClient)state.Src!;
        return await client.Connection.SoundQuery();
    }
}
