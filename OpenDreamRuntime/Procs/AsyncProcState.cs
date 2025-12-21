namespace OpenDreamRuntime.Procs;
public abstract class AsyncProcState : ProcState {
    #if TOOLS
            public override (string SourceFile, int Line) TracyLocationId => ("Async Native Proc", 0);
    #endif
    public abstract void SafeResume();
}
