namespace OpenDreamRuntime.Resources
{
    /// <summary>
    /// A special resource that outputs to the console
    /// <c>world.log</c> defaults to this
    /// </summary>
    sealed class ConsoleOutputResource : DreamResource {
        public ConsoleOutputResource() : base(null, null) { }

        public override string ReadAsString() {
            return null;
        }

        public override void Output(DreamValue value) {
            Logger.LogS(LogLevel.Info, "world.log", value.Stringify());
        }
    }
}
