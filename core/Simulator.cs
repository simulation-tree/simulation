using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Types;
using Unmanaged;
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
        private readonly List<ISystem> systems;
        private readonly List<MessageReceiverLocator[]> receiverLocators;
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
        public IReadOnlyList<ISystem> Systems
        {
            get
            {
                ThrowIfDisposed();

                return systems;
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
            receiverLocators = new(4);
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
        }

        /// <summary>
        /// Adds the given <paramref name="system"/>.
        /// </summary>
        public void Add<T>(T system) where T : ISystem
        {
            ThrowIfDisposed();

            if (system is IListener listener)
            {
                int count = listener.Count;
                MessageReceiverLocator[] locators = new MessageReceiverLocator[count];
                Span<MessageHandler> handlers = stackalloc MessageHandler[count];
                listener.CollectMessageHandlers(handlers);
                for (int i = 0; i < count; i++)
                {
                    MessageHandler handler = handlers[i];
                    if (!receiversMap.TryGetValue(handler.type, out List<Receive>? receivers))
                    {
                        receivers = new(4);
                        receiversMap.Add(handler.type, receivers);
                    }

                    locators[i] = new(handler.type, receivers.Count);
                    receivers.Add((Receive)handler.receiver.Target!);
                    handler.receiver.Free();
                }

                receiverLocators.Add(locators);
            }
            else
            {
                receiverLocators.Add(Array.Empty<MessageReceiverLocator>());
            }

            systems.Add(system);
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
                if (systems[i] is T system)
                {
                    Remove(i);
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
        public void Remove(ISystem system)
        {
            ThrowIfDisposed();
            ThrowIfSystemIsMissing(system);

            for (int i = 0; i < systems.Count; i++)
            {
                if (systems[i] == system)
                {
                    Remove(i);
                    return;
                }
            }

            throw new InvalidOperationException($"System instance {system} not found");
        }

        private void Remove(int index)
        {
            ThrowIfDisposed();

            MessageReceiverLocator[] locators = receiverLocators[index];
            for (int i = 0; i < locators.Length; i++)
            {
                MessageReceiverLocator locator = locators[i];
                List<Receive> receivers = receiversMap[locator.type];
                receivers.RemoveAt(locator.index);

                if (receivers.Count != locator.index)
                {
                    //the removed listener wasnt last, so need to shift the rest of the list
                    for (int c = 0; c < systems.Count; c++)
                    {
                        MessageReceiverLocator[] otherLocators = receiverLocators[c];
                        for (int j = 0; j < otherLocators.Length; j++)
                        {
                            MessageReceiverLocator otherLocator = otherLocators[j];
                            if (otherLocator.type == locator.type && otherLocator.index > locator.index)
                            {
                                otherLocator.index--;
                                otherLocators[j] = otherLocator;
                            }
                        }
                    }
                }
            }

            systems.RemoveAt(index);
        }

        /// <summary>
        /// Checks if the simulator contains a system of type <typeparamref name="T"/>.
        /// </summary>
        public bool Contains<T>() where T : ISystem
        {
            ThrowIfDisposed();

            for (int i = 0; i < systems.Count; i++)
            {
                if (systems[i] is T)
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
                if (systems[i] is T system)
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
                if (systems[i] is T foundSystem)
                {
                    system = foundSystem;
                    return true;
                }
            }

            system = default;
            return false;
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
                using MemoryAddress container = MemoryAddress.AllocateValue(message);
                for (int i = 0; i < receivers.Count; i++)
                {
                    receivers[i].Invoke(container);
                }
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
                using MemoryAddress container = MemoryAddress.AllocateValue(message);
                for (int i = 0; i < receivers.Count; i++)
                {
                    receivers[i].Invoke(container);
                }

                message = container.Read<T>();
            }
        }

        [Conditional("DEBUG")]
        private void ThrowIfSystemIsMissing<T>() where T : ISystem
        {
            for (int i = 0; i < systems.Count; i++)
            {
                if (systems[i] is T)
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
                if (systems[i] == system)
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