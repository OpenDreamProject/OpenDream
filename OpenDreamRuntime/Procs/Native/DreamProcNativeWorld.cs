using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using OpenDreamRuntime.Objects;

namespace OpenDreamRuntime.Procs.Native
{
    internal static class DreamProcNativeWorld
    {
        [DreamProc("Export")]
        [DreamProcParameter("Addr", Type = DreamValue.DreamValueType.String)]
        [DreamProcParameter("File", Type = DreamValue.DreamValueType.DreamObject, DefaultValue = null)]
        [DreamProcParameter("Persist", Type = DreamValue.DreamValueType.Float, DefaultValue = 0)]
        [DreamProcParameter("Clients", Type = DreamValue.DreamValueType.DreamObject, DefaultValue = null)]
        public static async Task<DreamValue> NativeProc_Export(AsyncNativeProc.State state)
        {
            var addr = state.Arguments.GetArgument(0, "Addr").Stringify();

            if (!Uri.TryCreate(addr, UriKind.RelativeOrAbsolute, out var uri))
                throw new ArgumentException("Unable to parse URI.");

            if (uri.Scheme is not ("http" or "https"))
                throw new NotSupportedException("non-HTTP world.Export is not supported.");

            // TODO: Maybe cache HttpClient.
            var client = new HttpClient();
            var response = await client.GetAsync(uri);

            var list = DreamList.Create();
            foreach (var header in response.Headers)
            {
                // TODO: How to handle headers with multiple values?
                list.SetValue(new DreamValue(header.Key), new DreamValue(header.Value.First()));
            }

            list.SetValue(new DreamValue("STATUS"), new DreamValue(((int) response.StatusCode).ToString()));
            list.SetValue(new DreamValue("CONTENT"), new DreamValue(await response.Content.ReadAsStringAsync()));

            return new DreamValue(list);
        }
    }
}
