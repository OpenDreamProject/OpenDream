using System;
using Robust.Shared.Configuration;

namespace OpenDreamShared {
    [CVarDefs]
    public abstract class OpenDreamCVars {
        public static readonly CVarDef<string> JsonPath =
            CVarDef.Create("opendream.json_path", String.Empty, CVar.SERVERONLY);


        public static readonly CVarDef<int> DownloadTimeout =
            CVarDef.Create("opendream.download_timeout", 30, CVar.CLIENTONLY);
    }
}
