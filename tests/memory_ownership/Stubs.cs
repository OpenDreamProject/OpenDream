using System.Text;
using OpenDreamRuntime.Objects;
namespace JetBrains.Annotations {
    [AttributeUsage(AttributeTargets.All)] public sealed class HandlesResourceDisposalAttribute: Attribute {}
}
namespace DMCompiler.DM { public enum ProcAttributes { None } }
namespace Robust.Shared.IoC {
    [AttributeUsage(AttributeTargets.All)] public sealed class DependencyAttribute: Attribute {}
}
namespace OpenDreamRuntime.Resources { public sealed class DreamResourceManager {} }
namespace OpenDreamRuntime.Objects {
    public class TreeEntry {}
    public class DreamObject {
        public int RefCount = 1;
        public bool Deleted;
        public bool Matches = true;
        public void IncRef() { if (!Deleted) RefCount++; }
        public void DecRef() { if (!Deleted && --RefCount < 1) throw new Exception("unexpected ownership release"); }
        public bool IsSubtypeOf(TreeEntry type) => !Deleted && Matches;
    }
    public sealed class DreamObjectTree {}
}
namespace OpenDreamRuntime.Objects.Types { public sealed class DreamObjectAtom: DreamObject {} }
namespace OpenDreamRuntime {
    public sealed class DreamManager {}
    public sealed class AtomManager {
        public IEnumerable<Objects.Types.DreamObjectAtom> EnumerateAtoms(TreeEntry? type) => [];
    }
    public readonly record struct DreamValue(DreamObject? Object): IDisposable {
        public static DreamValue Null => default;
        public DreamValue(float value): this((DreamObject?)null) {}
        public void IncRef() => Object?.IncRef();
        public void Dispose() => DecRef();
        public void DecRef() => Object?.DecRef();
        public bool TryGetValueAsDreamObject(out DreamObject? obj) { obj = Object; return obj != null && !obj.Deleted; }
    }
    public sealed class DreamThread {
        public Func<DreamValue>? OnResume;
        public DreamValue Resume() => OnResume?.Invoke() ?? default;
        public Procs.ProcStatus HandleDefer() => Procs.ProcStatus.Deferred;
        public void PushProcState(Procs.ProcState state) {}
    }
}
namespace OpenDreamRuntime.Procs {
    public enum DreamReference { NoRef, Value, Association }
    public enum ProcStatus { Returned, Called, Deferred }
    public sealed class DMProcState: IDisposable {
        public bool ThrowAssignment;
        private DreamValue _value, _association;
        public void AssignReference(DreamReference reference, DreamValue value) {
            if (ThrowAssignment) throw new InvalidOperationException("assignment control");
            if (reference == DreamReference.NoRef) return;
            value.IncRef();
            if (reference == DreamReference.Value) { _value.Dispose(); _value = value; }
            else { _association.Dispose(); _association = value; }
        }
        public void Dispose() { _value.Dispose(); _association.Dispose(); _value = default; _association = default; }
    }
    public static class IoCManager { public static void InjectDependencies(object value) {} }
    public abstract class DreamProc {
        public DreamProc(params object?[] parameters) {}
        public virtual ProcState CreateState(DreamThread thread, DreamObject? src, DreamObject? usr, DreamProcArguments arguments) => throw new NotImplementedException();
    }
    public abstract class ProcState: IDisposable {
        public DreamThread Thread = null!;
        public DreamValue Result;
        public DreamObject? Instance, Usr;
        public int ArgumentCount;
        public bool WaitFor;
        public abstract DreamProc? Proc { get; }
        protected void Initialize(DreamThread thread, bool waitFor) { Thread = thread; WaitFor = waitFor; }
        public virtual void Dispose() { Result.Dispose(); Result = default; Thread = null!; }
        public abstract ProcStatus Resume();
        public virtual void ReturnedInto(DreamValue value) {}
        public virtual void Cancel() {}
        public abstract void AppendStackFrame(StringBuilder builder);
        public abstract ReadOnlySpan<DreamValue> GetArguments();
        public abstract void SetArgument(int id, DreamValue value);
    }
    public readonly struct DreamProcArguments: IDisposable {
        public readonly DreamValue[] Values;
        public int Count => Values?.Length ?? 0;
        public DreamProcArguments() { Values = []; }
        public DreamProcArguments(DreamValue[] values) { Values = values; foreach (var value in values) value.IncRef(); }
        public void Dispose() { foreach (var value in Values) value.Dispose(); }
    }
    public sealed partial class ProcScheduler {
        private readonly Queue<TaskCompletionSource> _deferredTasks = new();
        private void UpdateDelays() {}
    }
}


