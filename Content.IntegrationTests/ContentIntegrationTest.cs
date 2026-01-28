using System;
using System.Threading.Tasks;
using OpenDreamClient;
using OpenDreamShared;
using NUnit.Framework;
using OpenDreamClient.Interface;
using OpenDreamRuntime;
using Robust.Client;
using Robust.Server;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.UnitTesting;

namespace Content.IntegrationTests;
[Parallelizable(ParallelScope.All)]
public abstract class ContentIntegrationTest : RobustIntegrationTest {
    protected sealed override ClientIntegrationInstance StartClient(ClientIntegrationOptions options = null) {
        options ??= new ClientContentIntegrationOption();

        // Load content resources, but not config and user data.
        options.Options = new GameControllerOptions() {
            LoadContentResources = true,
            LoadConfigAndUserData = false,
        };

        options.ContentStart = true;

        options.LoadTestAssembly = false;
        options.ContentAssemblies = new[] {
            typeof(OpenDreamShared.EntryPoint).Assembly,
            typeof(OpenDreamClient.EntryPoint).Assembly,
            typeof(ContentIntegrationTest).Assembly
        };

        options.BeforeStart += () => {
            IoCManager.Resolve<IModLoader>().SetModuleBaseCallbacks(new ClientModuleTestingCallbacks {
                ClientBeforeIoC = () => {
                    IoCManager.Register<IDreamInterfaceManager, DummyDreamInterfaceManager>(true);
                    if (options is ClientContentIntegrationOption contentOptions) {
                        contentOptions.ContentBeforeIoC?.Invoke();
                    }
                }
            });
        };

        return base.StartClient(options);
    }

    protected override ServerIntegrationInstance StartServer(ServerIntegrationOptions options = null) {
        options ??= new ServerContentIntegrationOption();

        // Load content resources, but not config and user data.
        options.Options = new ServerOptions() {
            LoadConfigAndUserData = false,
            LoadContentResources = true,
        };

        // Set compiled json path by default.
        if (!options.CVarOverrides.ContainsKey(OpenDreamCVars.JsonPath.Name))
            options.CVarOverrides[OpenDreamCVars.JsonPath.Name] = SetupCompileDm.CompiledProject;

        options.ContentStart = true;

        options.LoadTestAssembly = false;
        options.ContentAssemblies = new[] {
            typeof(OpenDreamShared.EntryPoint).Assembly,
            typeof(OpenDreamRuntime.EntryPoint).Assembly,
            typeof(ContentIntegrationTest).Assembly
        };

        options.BeforeStart += () => {
            IoCManager.Resolve<IModLoader>().SetModuleBaseCallbacks(new ServerModuleTestingCallbacks {
                ServerBeforeIoC = () => {
                    if (options is ServerContentIntegrationOption contentOptions) {
                        contentOptions.ContentBeforeIoC?.Invoke();
                    }
                }
            });

            IoCManager.Resolve<ILogManager>().GetSawmill("loc").Level = LogLevel.Error;
        };

        return base.StartServer(options);
    }

    protected async Task<(ClientIntegrationInstance client, ServerIntegrationInstance server)>
        StartConnectedServerClientPair(ClientIntegrationOptions clientOptions = null,
            ServerIntegrationOptions serverOptions = null) {
        var client = StartClient(clientOptions);
        var server = StartServer(serverOptions);

        await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

        client.SetConnectTarget(server);

        client.Post(() => IoCManager.Resolve<IClientNetManager>().ClientConnect(null!, 0, null!));

        await RunTicksSync(client, server, 10);

        return (client, server);
    }

    protected async Task WaitUntil(IntegrationInstance instance, Func<bool> func, int maxTicks = 600,
        int tickStep = 1) {
        var ticksAwaited = 0;
        bool passed;

        await instance.WaitIdleAsync();

        while (!(passed = func()) && ticksAwaited < maxTicks) {
            var ticksToRun = tickStep;

            if (ticksAwaited + tickStep > maxTicks) {
                ticksToRun = maxTicks - ticksAwaited;
            }

            await instance.WaitRunTicks(ticksToRun);

            ticksAwaited += ticksToRun;
        }

        Assert.That(passed);
    }

    /// <summary>
    ///     Runs <paramref name="ticks"/> ticks on both server and client while keeping their main loop in sync.
    /// </summary>
    protected static async Task RunTicksSync(ClientIntegrationInstance client, ServerIntegrationInstance server,
        int ticks) {
        for (var i = 0; i < ticks; i++) {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }
    }

    protected sealed class ClientContentIntegrationOption : ClientIntegrationOptions {
        public override GameControllerOptions Options { get; set; } = new() {
            LoadContentResources = true,
            LoadConfigAndUserData = false,
        };

        public Action ContentBeforeIoC { get; set; }
    }

    protected sealed class ServerContentIntegrationOption : ServerIntegrationOptions {
        public override ServerOptions Options { get; set; } = new() {
            LoadContentResources = true,
            LoadConfigAndUserData = false,
        };

        public Action ContentBeforeIoC { get; set; }
    }
}
