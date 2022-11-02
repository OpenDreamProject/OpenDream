using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public sealed class RequestExceptionInfo : Request {
    [JsonPropertyName("arguments")] public RequestExceptionInfoArguments Arguments { get; set; }

    public sealed class RequestExceptionInfoArguments {
        /**
         * Thread for which exception information should be retrieved.
         */
        [JsonPropertyName("threadId")] public int ThreadId { get; set; }
    }

    public sealed class ExceptionInfoResponse {
        /**
         * ID of the exception that was thrown.
         */
        [JsonPropertyName("exceptionId")] public string ExceptionId { get; set; }

        /**
         * Descriptive text for the exception.
         */
        [JsonPropertyName("description")] public string? Description { get; set; }

        /**
         * Mode that caused the exception notification to be raised.
         */
        [JsonPropertyName("breakMode")] public string BreakMode { get; set; }
        // enum ExceptionBreakMode, no custom values
        public const string BreakModeNever = "never";
        public const string BreakModeAlways = "always";
        public const string BreakModeUnhandled = "unhandled";
        public const string BreakModeUserUnhandled = "userUnhandled";

        /**
         * Detailed information about the exception.
         */
        [JsonPropertyName("details")] public ExceptionDetails? Details { get; set; }
    }

    public void Respond(DebugAdapterClient client, ExceptionInfoResponse response) {
        client.SendMessage(Response.NewSuccess(this, response));
    }
}
