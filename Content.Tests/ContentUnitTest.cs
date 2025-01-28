using System;
using System.Collections.Generic;
using System.Reflection;
using OpenDreamClient;
using OpenDreamRuntime;
using OpenDreamRuntime.Map;
using OpenDreamRuntime.Rendering;
using OpenDreamShared;
using OpenDreamShared.Rendering;
using Robust.Shared.Analyzers;
using Robust.Shared.IoC;
using Robust.UnitTesting;
using EntryPoint = OpenDreamRuntime.EntryPoint;

namespace Content.Tests;

[Virtual]
public class ContentUnitTest : RobustUnitTest {
    protected override Type[] ExtraComponents { get; } = {
        typeof(DMISpriteComponent),
        typeof(DreamMobSightComponent)
    };

    protected override void OverrideIoC() {
        base.OverrideIoC();

        SharedOpenDreamIoC.Register();

        if (Project == UnitTestProject.Server) {
            ServerContentIoC.Register(unitTests: true);
            IoCManager.Register<IDreamMapManager, DummyDreamMapManager>();
        } else if (Project == UnitTestProject.Client) {
            ClientContentIoC.Register();
        }
    }

    protected override Assembly[] GetContentAssemblies() {
        var l = new List<Assembly> {
            typeof(OpenDreamShared.EntryPoint).Assembly
        };

        if (Project == UnitTestProject.Server) {
            l.Add(typeof(EntryPoint).Assembly);
        } else if (Project == UnitTestProject.Client) {
            l.Add(typeof(OpenDreamClient.EntryPoint).Assembly);
        }

        l.Add(typeof(ContentUnitTest).Assembly);

        return l.ToArray();
    }
}
