using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

[UsedImplicitly]
public sealed class RequestSetExceptionBreakpoints : Request {
    [JsonPropertyName("arguments")] public required RequestSetExceptionBreakpointsArguments Arguments { get; set; }

    [UsedImplicitly]
    public sealed class RequestSetExceptionBreakpointsArguments {
        /**
         * Set of exception filters specified by their ID. The set of all possible
         * exception filters is defined by the `exceptionBreakpointFilters`
         * capability. The `filter` and `filterOptions` sets are additive.
         */
        [JsonPropertyName("filters")] public required string[] Filters { get; set; }

        /*
        /**
         * Set of exception filters and their options. The set of all possible
         * exception filters is defined by the `exceptionBreakpointFilters`
         * capability. This attribute is only honored by a debug adapter if the
         * corresponding capability `supportsExceptionFilterOptions` is true. The
         * `filter` and `filterOptions` sets are additive.
         */
        //[JsonPropertyName("filterOptions")] public ExceptionFilterOptions[]? FilterOptions { get; set; }

        /*
        /**
         * Configuration options for selected exceptions.
         * The attribute is only honored by a debug adapter if the corresponding
         * capability `supportsExceptionOptions` is true.
         */
        //[JsonPropertyName("exceptionOptions")] public ExceptionOptions[]? ExceptionOptions { get; set; }
    }

    public void Respond(DebugAdapterClient client, Breakpoint[]? breakpoints) {
        client.SendMessage(Response.NewSuccess(this, new { breakpoints = breakpoints }));
    }
}
