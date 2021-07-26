using Content.Shared.Compiler;
using Content.Shared.Compiler.DMF;
using Content.Shared.Interface;
using Robust.Client.ResourceManagement;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Content.Client.Resources {
    class DMFResource : BaseResource {
        public InterfaceDescriptor Interface { get; private set; }

        public override void Load(IResourceCache cache, ResourcePath path) {
            string dmfSource = cache.ContentFileReadAllText(path);
            dmfSource = dmfSource.Replace("\r\n", "\n");
            DMFParser dmfParser = new DMFParser(new DMFLexer(path.Filename, dmfSource));

            Interface = dmfParser.Interface();

            foreach (CompilerWarning warning in dmfParser.Warnings) {
                Logger.Warning(warning.ToString());
            }

            if (dmfParser.Errors.Count > 0) {
                Interface = null;

                foreach (CompilerError error in dmfParser.Errors) {
                    Logger.Error(error.ToString());
                }
            }
        }
    }
}
