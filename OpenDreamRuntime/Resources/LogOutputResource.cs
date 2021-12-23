using System;
using System.IO;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace OpenDreamRuntime.Resources
{
    /// <summary>
    /// A special resource that outputs to the console or (if specified) a file
    /// <c>world.log</c> defaults to this
    /// </summary>
    public class LogOutputResource : DreamResource
    {

        public LogOutputResource(string? resourcePath) : base(
            Path.Combine(IoCManager.Resolve<DreamResourceManager>().RootPath, resourcePath ?? ""), resourcePath)
        {
            ResourcePath = resourcePath;
        }

        public override string ReadAsString() {
            return null;
        }

        public override void Output(DreamValue value) {

            if (ResourcePath is null)
            {
                Logger.LogS(LogLevel.Info, "world.log", value.Stringify());
            }
            else
            {
                base.Output(value);
            }
        }

        public void WriteLog(LogLevel level, string message)
        {
            if (ResourcePath != null)
            {
                base.Output(new DreamValue(message));
                return;
            }

            Logger.LogS(level, "world.log", message);
        }
    }
}
