using System.Linq;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Procs;
using Robust.Shared.Console;

namespace OpenDreamRuntime.Commands;

public sealed class FastProcCallCountCommand : IConsoleCommand {
    public string Command => "fastproccallcount";
    public string Description => "Sorts native procs by call count and prints them out.";
    public string Help => Command;
    public void Execute(IConsoleShell shell, string argStr, string[] args) {
        var tree = IoCManager.Resolve<DreamObjectTree>();

        var allProcs = new List<FastNativeProc>();

        foreach (var proc in tree.Procs) {
            if (proc is FastNativeProc t)
                allProcs.Add(t);
        }

        foreach (var ty in tree.Types) {
            var def = ty.ObjectDefinition;
            foreach (var procName in def.Procs.Keys.Concat(def.OverridingProcs.Keys).Distinct()) {
                if (def.TryGetProc(procName, out var proc) && proc is FastNativeProc t)
                    allProcs.Add(t);
            }
        }

        foreach (var proc in allProcs.DistinctBy(x => x.Id).OrderBy(x => x.CallCount)) {
            shell.WriteLine($"{proc}: {proc.CallCount}");
        }
    }
}
