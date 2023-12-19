using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using OpenDreamShared;

using Robust.Server;
using Robust.Shared.Configuration;

namespace OpenDreamRuntime {
    /// <summary>
    /// Handles commands from the <see cref="OpenDreamCVars.CommandPipe"/>.
    /// </summary>
    /// <remarks>Not reentrant.</remarks>
    internal sealed class CommandPipeManager {
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IBaseServer _baseServer = default!;

        private readonly ISawmill _sawmill;

        private Task? activeTask;
        private CancellationTokenSource? activeCts;

        public CommandPipeManager() {
            _sawmill = Logger.GetSawmill("opendream");
        }

        public void Start() {
            if (activeTask != null)
                throw new InvalidOperationException("Already running!");

            activeCts = new CancellationTokenSource();
            activeTask = RunAsync(activeCts.Token);
        }

        public async ValueTask Shutdown() {
            if (activeTask == null)
                return;

            activeCts!.Cancel();
            activeCts.Dispose();
            activeCts = null;
            await activeTask;
            activeTask = null;
        }

        private async Task RunAsync(CancellationToken cancellationToken) {

            var commandPipeName = _configManager.GetCVar(OpenDreamCVars.CommandPipe);
            if (string.IsNullOrWhiteSpace(commandPipeName)) {
                _sawmill.Debug("No command pipe present");
                return;
            }

            _sawmill.Debug("Starting CommandPipeManager...");

            // grab both pipes asap so we can close them on error
            await using var commandPipeClient = new AnonymousPipeClientStream(
                PipeDirection.In,
                commandPipeName);

            try {
                using var streamReader = new StreamReader(commandPipeClient, Encoding.UTF8, leaveOpen: true);
                while (!cancellationToken.IsCancellationRequested) {
                    _sawmill.Debug("Waiting to read command...");
                    var line = await streamReader.ReadLineAsync(cancellationToken);

                    _sawmill.Info("Received pipe command: {0}", line);
                    switch (line) {
                        case "shutdown":
                            _baseServer.Shutdown("Received shutdown pipe command");
                            break;
                        default:
                            _sawmill.Warning("Unrecognized pipe command: {command}", line);
                            break;
                    }
                }
            } catch (OperationCanceledException) {
                _sawmill.Debug("Command read task cancelled!");
            } catch (Exception ex) {
                _sawmill.Error("Command read task errored! Exception: {0}", ex);
            }
            finally {
                _sawmill.Debug("Command read task exiting...");
            }
        }
    }
}
