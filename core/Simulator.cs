using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Types;
using Worlds;

namespace Simulation
{
    /// <summary>
    /// Contains systems for updating and broadcasting messages to.
    /// </summary>
    [SkipLocalsInit]
    public class Simulator : IDisposable
    {
        /// <summary>
        /// The world this simulator is created for.
        /// </summary>
        public readonly World world;

        private readonly Dictionary<TypeMetadata, List<Receive>> receiversMap;
        private readonly List<SystemContainer> systems;
        private DateTime lastUpdateTime;
        private double runTime;
        private bool disposed;

        /// <summary>
        /// Checks if the simulator has been disposed.
        /// </summary>
        public bool IsDisposed => disposed;

        /// <summary>
        /// The total amount of time the simulator has progressed.
        /// </summary>
        public double Time
        {
            get
            {
                ThrowIfDisposed();

                return runTime;
            }
        }

        /// <summary>
        /// Amount of systems added.
        /// </summary>
        public int Count
        {
            get
            {
                ThrowIfDisposed();

                return systems.Count;
            }
        }

        /// <summary>
        /// All systems added.
        /// </summary>
        public IEnumerable<ISystem> Systems
        {
            get
            {
                ThrowIfDisposed();

                int count = systems.Count;
                for (int i = 0; i < count; i++)
                {
                    yield return GetSystem(i);
                }
            }
        }

        /// <summary>
        /// Creates a new simulator.
        /// </summary>
        public Simulator(World world)
        {
            this.world = world;
            receiversMap = new(4);
            systems = new(4);
            runTime = 0;
            lastUpdateTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Disposes the simulator.
        /// </summary>
        public void Dispose()
        {
            ThrowIfDisposed();

            disposed = true;
            foreach (SystemContainer system in systems)
            {
                system.Dispose();
            }
        }

        /// <summary>
        /// Adds the given <paramref name="system"/>.
        /// </summary>
        public void Add<T>(T system) where T : ISystem
        {
            ThrowIfDisposed();

            SystemContainer container = SystemContainer.Create(system);
            if (system is IListener listener)
            {
                int count = listener.Count;
                Span<MessageHandler> handlers = stackalloc MessageHandler[count];
                listener.CollectMessageHandlers(handlers);
                foreach (MessageHandler handler in handlers)
                {
                    if (!receiversMap.TryGetValue(handler.type, out List<Receive>? receivers))
                    {
                        receivers = new(4);
                        receiversMap.Add(handler.type, receivers);
                    }

                    container.receiverLocators.Add(new(handler.type, receivers.Count));
                    receivers.Add((Receive)handler.receiver.Target!);
                    handler.receiver.Free();
                }
            }

            systems.Add(container);
        }

        /// <summary>
        /// Removes the first system found of type <typeparamref name="T"/>,
        /// and disposes it by default.
        /// </summary>
        /// <returns>The removed system.</returns>
        public T Remove<T>(bool dispose = true) where T : ISystem
        {
            ThrowIfDisposed();
            ThrowIfSystemIsMissing<T>();

            for (int i = 0; i < systems.Count; i++)
            {
                SystemContainer container = systems[i];
                if (container.System is T system)
                {
                    Remove(container, i);
                    if (dispose && system is IDisposable disposableSystem)
                    {
                        disposableSystem.Dispose();
                    }

                    return system;
                }
            }

            throw new InvalidOperationException($"System of type {typeof(T)} not found");
        }

        /// <summary>
        /// Removes the given <paramref name="system"/> without disposing it.
        /// <para>
        /// Throws an exception if the system is not found.
        /// </para>
        /// </summary>
        public void Remove(object system)
        {
            ThrowIfDisposed();
            ThrowIfSystemIsMissing(system);

            for (int i = 0; i < systems.Count; i++)
            {
                SystemContainer container = systems[i];
                if (container.System == system)
                {
                    Remove(container, i);
                    return;
                }
            }

            throw new InvalidOperationException($"System instance {system} not found");
        }

        private void Remove(SystemContainer container, int index)
        {
            ThrowIfDisposed();

            ReadOnlySpan<MessageReceiverLocator> receiverLocators = container.receiverLocators.AsSpan();
            for (int i = 0; i < receiverLocators.Length; i++)
            {
                MessageReceiverLocator locator = receiverLocators[i];
                List<Receive> receivers = receiversMap[locator.type];
                receivers.RemoveAt(locator.index);

                if (receivers.Count != locator.index)
                {
                    //the removed listener wasnt last, so need to shift the rest of the list
                    for (int c = 0; c < systems.Count; c++)
                    {
                        SystemContainer system = systems[c];
                        Span<MessageReceiverLocator> locators = system.receiverLocators.AsSpan();
                        for (int j = 0; j < locators.Length; j++)
                        {
                            ref MessageReceiverLocator otherLocator = ref locators[j];
                            if (otherLocator.type == locator.type && otherLocator.index > locator.index)
                            {
                                otherLocator.index--;
                            }
                        }
                    }
                }
            }

            systems.RemoveAt(index);
            container.Dispose();
        }

        /// <summary>
        /// Checks if the simulator contains a system of type <typeparamref name="T"/>.
        /// </summary>
        public bool Contains<T>() where T : ISystem
        {
            ThrowIfDisposed();

            for (int i = 0; i < systems.Count; i++)
            {
                if (systems[i].System is T)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Retrieves the first system of type <typeparamref name="T"/>.
        /// <para>
        /// An exception will be thrown if a system is not found.
        /// </para>
        /// </summary>
        public T GetFirst<T>() where T : ISystem
        {
            ThrowIfDisposed();
            ThrowIfSystemIsMissing<T>();

            for (int i = 0; i < systems.Count; i++)
            {
                if (systems[i].System is T system)
                {
                    return system;
                }
            }

            throw new InvalidOperationException($"System of type {typeof(T)} not found");
        }

        /// <summary>
        /// Tries to retrieve the first system of type <typeparamref name="T"/>.
        /// </summary>
        public bool TryGetFirst<T>([NotNullWhen(true)] out T? system) where T : notnull, ISystem
        {
            ThrowIfDisposed();

            for (int i = 0; i < systems.Count; i++)
            {
                if (systems[i].System is T foundSystem)
                {
                    system = foundSystem;
                    return true;
                }
            }

            system = default;
            return false;
        }

        /// <summary>
        /// Retrieves the system at the given <paramref name="index"/>.
        /// </summary>
        public ISystem GetSystem(int index)
        {
            ThrowIfDisposed();
            ThrowIfIndexIsOutOfBounds(index);

            return systems[index].System;
        }

        /// <summary>
        /// Updates all systems forward.
        /// </summary>
        public void Update()
        {
            ThrowIfDisposed();

            DateTime timeNow = DateTime.UtcNow;
            double deltaTime = (timeNow - lastUpdateTime).TotalSeconds;
            lastUpdateTime = timeNow;
            runTime += deltaTime;

            for (int i = 0; i < systems.Count; i++)
            {
                systems[i].Update(this, deltaTime);
            }
        }

        /// <summary>
        /// Updates all systems forward and retreives the delta time.
        /// </summary>
        public void Update(out double deltaTime)
        {
            ThrowIfDisposed();

            DateTime timeNow = DateTime.UtcNow;
            deltaTime = (timeNow - lastUpdateTime).TotalSeconds;
            lastUpdateTime = timeNow;
            runTime += deltaTime;

            for (int i = 0; i < systems.Count; i++)
            {
                systems[i].Update(this, deltaTime);
            }
        }

        /// <summary>
        /// Updates all systems forward with the specified <paramref name="deltaTime"/>.
        /// </summary>
        public void Update(double deltaTime)
        {
            ThrowIfDisposed();

            lastUpdateTime = DateTime.UtcNow;
            runTime += deltaTime;

            for (int i = 0; i < systems.Count; i++)
            {
                systems[i].Update(this, deltaTime);
            }
        }

        /// <summary>
        /// Broadcasts the given <paramref name="message"/> to all systems.
        /// </summary>
        public void Broadcast<T>(T message) where T : unmanaged
        {
            ThrowIfDisposed();

            TypeMetadata messageType = TypeMetadata.GetOrRegister<T>();
            if (receiversMap.TryGetValue(messageType, out List<Receive>? receivers))
            {
                Message container = Message.Create(message);
                for (int i = 0; i < receivers.Count; i++)
                {
                    receivers[i].Invoke(ref container);
                }

                container.Dispose();
            }
        }

        /// <summary>
        /// Broadcasts the given <paramref name="message"/> to all systems.
        /// </summary>
        public void Broadcast<T>(ref T message) where T : unmanaged
        {
            ThrowIfDisposed();

            TypeMetadata messageType = TypeMetadata.GetOrRegister<T>();
            if (receiversMap.TryGetValue(messageType, out List<Receive>? receivers))
            {
                Message container = Message.Create(message);
                for (int i = 0; i < receivers.Count; i++)
                {
                    receivers[i].Invoke(ref container);
                }

                message = container.data.Read<T>();
                container.Dispose();
            }
        }

        [Conditional("DEBUG")]
        private void ThrowIfSystemIsMissing<T>() where T : ISystem
        {
            for (int i = 0; i < systems.Count; i++)
            {
                if (systems[i].System is T)
                {
                    return;
                }
            }

            throw new InvalidOperationException($"System of type {typeof(T)} not found");
        }

        [Conditional("DEBUG")]
        private void ThrowIfSystemIsMissing(object system)
        {
            for (int i = 0; i < systems.Count; i++)
            {
                if (systems[i].System == system)
                {
                    return;
                }
            }

            throw new InvalidOperationException($"System instance {system} not found");
        }

        [Conditional("DEBUG")]
        private void ThrowIfIndexIsOutOfBounds(int index)
        {
            if (index < 0 || index >= systems.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of bounds. Count: {systems.Count}");
            }
        }

        [Conditional("DEBUG")]
        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(Simulator), "Simulator has been disposed");
            }
        }
    }
}