using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Unearth
{
    /// <summary>
    /// Kills Synchronization Context to prevent ASP.NET Deadlocks
    /// usage: await new SynchronizationContextRemover()
    /// </summary>
    public struct SynchronizationContextRemover : INotifyCompletion
    {
        public bool IsCompleted => SynchronizationContext.Current == null;

        public void OnCompleted(Action continuation)
        {
            var prevContext = SynchronizationContext.Current;
            try
            {
                SynchronizationContext.SetSynchronizationContext(null);
                continuation();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(prevContext);
            }
        }

        public SynchronizationContextRemover GetAwaiter() => this;

        public void GetResult() {}
    }
}