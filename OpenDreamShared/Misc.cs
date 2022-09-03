using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenDreamShared
{
    /// <summary>
    /// TODO: Try to think of a better name for this (dummy) class.
    /// </summary>
    public static class Misc
    {
        /// <summary>
        /// Defers calling the given action until the block is completed. <br/>
        /// To use this properly, you must cast a magic spell against the GC, in this specific way: <br/>
        /// <see langword="using"/> <see langword="var"/> _ = Defer(b =&gt; bluh(b), maybe_captured_parameter); <br/>
        /// If everything goes well (!!), this MUST be called before the block that invokes it returns.
        /// </summary>
        public static DeferDisposable<T> Defer<T>(Action<T> action, T param1) => new DeferDisposable<T>(action, param1);

        /// <inheritdoc cref="Defer{T}(Action{T}, T)"/>
        public static DeferDisposableVoid Defer(Action action) => new DeferDisposableVoid(action);

        public readonly struct DeferDisposable<T1> : IDisposable
        {
            readonly Action<T1> _action;
            readonly T1 _param1;
            public DeferDisposable(Action<T1> action, T1 param1)
            {
                _action = action;
                _param1 = param1;
            }
            public void Dispose() => _action.Invoke(_param1);
        }
        public readonly struct DeferDisposableVoid: IDisposable
        {
            readonly Action _action;
            public DeferDisposableVoid(Action action)
            {
                _action = action;
            }
            public void Dispose() => _action.Invoke();
        }
    }
}
