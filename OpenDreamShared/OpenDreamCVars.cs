using System;
using Robust.Shared.Configuration;

namespace OpenDreamShared {
    [CVarDefs]
    public abstract class OpenDreamCVars {
        public static readonly CVarDef<string> JsonPath =
            CVarDef.Create("opendream.json_path", String.Empty, CVar.SERVERONLY);

        public static readonly CVarDef<int> DownloadTimeout =
            CVarDef.Create("opendream.download_timeout", 30, CVar.CLIENTONLY);

        public static readonly CVarDef<bool> AlwaysShowExceptions =
            CVarDef.Create("opendream.always_show_exceptions", false, CVar.SERVERONLY);

        public static readonly CVarDef<int> DebugAdapterLaunched =
            CVarDef.Create("opendream.debug_adapter_launched", 0, CVar.SERVERONLY);

        public static readonly CVarDef<bool> SpoofIEUserAgent =
            CVarDef.Create("opendream.spoof_ie_user_agent", true, CVar.CLIENTONLY);

        public static readonly CVarDef<string> WorldParams =
            CVarDef.Create("opendream.world_params", string.Empty, CVar.SERVERONLY);

        public static readonly CVarDef<int> TopicPort =
            CVarDef.Create("opendream.topic_port", 25567, CVar.SERVERONLY);
    }
}
