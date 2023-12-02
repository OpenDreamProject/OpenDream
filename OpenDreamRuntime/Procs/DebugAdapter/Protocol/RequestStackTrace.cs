using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

[UsedImplicitly]
public sealed class RequestStackTrace : Request {
    [JsonPropertyName("arguments")] public required RequestSetBreakpointsArguments Arguments { get; set; }

    [UsedImplicitly]
    public sealed class RequestSetBreakpointsArguments {
        /**
         * Retrieve the stacktrace for this thread.
         */
        [JsonPropertyName("threadId")] public int ThreadId { get; set; }

        /**
         * The index of the first frame to return; if omitted frames start at 0.
         */
        [JsonPropertyName("startFrame")] public int? StartFrame { get; set; }

        /**
         * The maximum number of frames to return. If levels is not specified or 0,
         * all frames are returned.
         */
        [JsonPropertyName("levels")] public int? Levels { get; set; }

        /**
         * Specifies details on how to format the stack frames.
         * The attribute is only honored by a debug adapter if the corresponding
         * capability `supportsValueFormattingOptions` is true.
         */
        [JsonPropertyName("format")] public StackFrameFormat? Format { get; set; }
    }

    public void Respond(DebugAdapterClient client, IEnumerable<StackFrame> stackFrames, int? totalFrames = null) {
        client.SendMessage(Response.NewSuccess(this, new { stackFrames, totalFrames }));
    }
}
