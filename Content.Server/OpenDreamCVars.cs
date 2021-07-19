using Robust.Shared.Configuration;
using System;

namespace Content.Server {
    [CVarDefs]
    public abstract class OpenDreamCVars {
        public static readonly CVarDef<string> File =
            CVarDef.Create("opendream.json", String.Empty, CVar.SERVERONLY);
    }
}
