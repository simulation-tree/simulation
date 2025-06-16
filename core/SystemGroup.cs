using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Simulation
{
    /// <summary>
    /// A group of systems that all run on a separate foreground thread.
    /// </summary>
    public sealed class SystemGroup<T> : IDisposable, IListener<T> where T : unmanaged
    {
        private readonly CancellationTokenSource cts;
        private readonly Thread thread;
        private IListener<T>[] systems;
        private bool disposed;
        private volatile int messageVersion;
        private volatile bool finishedSignal;
        private T message;

        int IListener.Count => 1;

        /// <summary>
        /// Creates a new system group.
        /// </summary>
        public SystemGroup(ReadOnlySpan<char> threadName)
        {
            systems = [];
            cts = new();
            thread = new Thread(Run);
            thread.Name = threadName.ToString();
            thread.IsBackground = false;
            thread.Start();
        }

        void IListener.CollectMessageHandlers(Span<MessageHandler> messageHandlers)
        {
            messageHandlers[0] = MessageHandler.Get<IListener<T>, T>(this);
        }

        private void Run()
        {
            SpinWait waiter = new();
            int lastMessageVersion = 0;
            while (true)
            {
                while (lastMessageVersion == messageVersion && !cts.IsCancellationRequested)
                {
                    waiter.SpinOnce();
                }

                if (cts.IsCancellationRequested)
                {
                    break;
                }

                Thread.MemoryBarrier();
                T currentMessage = message;
                lastMessageVersion = messageVersion;

                foreach (IListener<T> system in systems)
                {
                    system.Receive(ref currentMessage);
                }

                waiter.Reset();
                finishedSignal = true;
            }
        }

        /// <summary>
        /// Disposes the system group and finishes work on its thread.
        /// </summary>
        public void Dispose()
        {
            ThrowIfDisposed();

            disposed = true;
            cts.Cancel();
            if (thread.IsAlive)
            {
                thread.Join();
            }

            cts.Dispose();
        }

        /// <summary>
        /// Adds the given <paramref name="system"/> to the group.
        /// </summary>
        public void Add<S>(S system) where S : IListener<T>
        {
            ThrowIfDisposed();

            Array.Resize(ref systems, systems.Length + 1);
            systems[^1] = system;
        }

        /// <summary>
        /// Removes the system of type <typeparamref name="S"/> from the group.
        /// </summary>
        public void Remove<S>() where S : IListener<T>
        {
            ThrowIfDisposed();

            List<IListener<T>> systemList = new(systems);
            for (int i = 0; i < systemList.Count; i++)
            {
                if (systemList[i] is S system)
                {
                    systemList.RemoveAt(i);
                    if (system is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }

                    break;
                }
            }

            systems = systemList.ToArray();
        }

        /// <summary>
        /// Signals the thread to process the given <paramref name="message"/>.
        /// </summary>
        public void Receive(ref T message)
        {
            ThrowIfDisposed();

            finishedSignal = false;
            this.message = message;
            Thread.MemoryBarrier();
            Interlocked.Increment(ref messageVersion);

            SpinWait waiter = new();
            while (!finishedSignal)
            {
                waiter.SpinOnce();
            }
        }

        [Conditional("DEBUG")]
        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(SystemGroup<T>), "This system group has already been disposed");
            }
        }
    }
}