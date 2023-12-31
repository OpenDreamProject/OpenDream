using System.Text.Json.Nodes;
using OpenDreamShared;
using Robust.Server.ServerStatus;
using Robust.Shared.Configuration;

namespace OpenDreamRuntime;

/// <summary>
/// Adds additional data like info links to the server info endpoint
/// </summary>
public sealed class ServerInfoManager
{
    private static readonly (CVarDef<string> cVar, string icon, string name)[] Vars =
    {
        // @formatter:off
        (OpenDreamCVars.InfoLinksDiscord, "discord", "Discord"),
        (OpenDreamCVars.InfoLinksForum,   "forum",   "Forum"),
        (OpenDreamCVars.InfoLinksGithub,  "github",  "GitHub"),
        (OpenDreamCVars.InfoLinksWebsite, "web",     "Website"),
        (OpenDreamCVars.InfoLinksWiki,    "wiki",    "Wiki")
        // @formatter:on
    };

    [Dependency] private readonly IStatusHost _statusHost = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public void Initialize()
    {
        _statusHost.OnInfoRequest += OnInfoRequest;
    }

    private void OnInfoRequest(JsonNode json)
    {
        foreach (var (cVar, icon, name) in Vars)
        {
            var url = _cfg.GetCVar(cVar);
            if (string.IsNullOrEmpty(url))
                continue;

            StatusHostHelpers.AddLink(json, name, url, icon);
        }
    }
}
