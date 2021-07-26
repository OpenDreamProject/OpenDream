using Robust.Shared.Configuration;
using System;

namespace Content.Server {
    [CVarDefs]
    public abstract class OpenDreamCVars {
        public static readonly CVarDef<string> JsonPath =
            CVarDef.Create("opendream.json_path", String.Empty, CVar.SERVERONLY);
    }
}
