using Collections.Generic;
using System;
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
    public struct Simulator : IDisposable
    {
        /// <summary>
        /// The world this simulator is created for.
        /// </summary>
        public readonly World world;

        private Dictionary<TypeMetadata, List<MessageReceiver>> receiversMap;
        private List<SystemContainer> systems;
        private DateTime lastUpdateTime;
        private double runTime;

        /// <summary>
        /// Checks if the simulator has been disposed.
        /// </summary>
        public readonly bool IsDisposed => systems.IsDisposed;

        /// <summary>
        /// The total amount of time the simulator has progressed.
        /// </summary>
        public readonly double Time => runTime;

        /// <summary>
        /// Amount of systems added.
        /// </summary>
        public readonly int Count => systems.Count;

        /// <summary>
        /// All systems added.
        /// </summary>
        public readonly System.Collections.Generic.IEnumerable<ISystem> Systems
        {
            get
            {
                int count = systems.Count;
                for (int i = 0; i < count; i++)
                {
                    yield return GetSystem(i);
                }
            }
        }

#if NET
        [Obsolete("Default constructor not supported", true)]
        public Simulator()
        {
        }
#endif
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
            Span<SystemContainer> systemsSpan = systems.AsSpan();
            foreach (SystemContainer system in systemsSpan)
            {
                system.Dispose();
            }

            systems.Dispose();
            foreach (List<MessageReceiver> receivers in receiversMap.Values)
            {
                receivers.Dispose();
            }

            receiversMap.Dispose();
        }

        /// <summary>
        /// Adds the given <paramref name="system"/>.
        /// </summary>
        public readonly void Add<T>(T system) where T : ISystem
        {
            SystemContainer container = SystemContainer.Create(system);
            if (system is IListener listener)
            {
                int count = listener.Count;
                Span<MessageHandler> handlers = stackalloc MessageHandler[count];
                listener.CollectMessageHandlers(handlers);
                foreach (MessageHandler handler in handlers)
                {
                    ref List<MessageReceiver> receivers = ref receiversMap.TryGetValue(handler.type, out bool contains);
                    if (!contains)
                    {
                        receivers = ref receiversMap.Add(handler.type);
                        receivers = new(4);
                    }

                    container.receiverLocators.Add(new(handler.type, receivers.Count));
                    receivers.Add(handler.receiver);
                }
            }

            systems.Add(container);
        }

        /// <summary>
        /// Removes the first system found of type <typeparamref name="T"/>,
        /// and disposes it by default.
        /// </summary>
        /// <returns>The removed system.</returns>
        public readonly T Remove<T>(bool dispose = true) where T : ISystem
        {
            ThrowIfSystemIsMissing<T>();

            ReadOnlySpan<SystemContainer> systemsSpan = systems.AsSpan();
            for (int i = 0; i < systemsSpan.Length; i++)
            {
                SystemContainer container = systemsSpan[i];
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
        public readonly void Remove(object system)
        {
            ThrowIfSystemIsMissing(system);

            ReadOnlySpan<SystemContainer> systemsSpan = systems.AsSpan();
            for (int i = 0; i < systemsSpan.Length; i++)
            {
                SystemContainer container = systemsSpan[i];
                if (container.System == system)
                {
                    Remove(container, i);
                    return;
                }
            }

            throw new InvalidOperationException($"System instance {system} not found");
        }

        private readonly void Remove(SystemContainer container, int index)
        {
            ReadOnlySpan<MessageReceiverLocator> receiverLocators = container.receiverLocators.AsSpan();
            for (int i = 0; i < receiverLocators.Length; i++)
            {
                MessageReceiverLocator locator = receiverLocators[i];
                ref List<MessageReceiver> receivers = ref receiversMap[locator.type];
                receivers.RemoveAt(locator.index);

                if (receivers.Count != locator.index)
                {
                    //the removed listener wasnt last, so need to shift the rest of the list
                    Span<SystemContainer> systemsSpan = systems.AsSpan();
                    for (int c = 0; c < systemsSpan.Length; c++)
                    {
                        SystemContainer system = systemsSpan[c];
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
        public readonly bool Contains<T>() where T : ISystem
        {
            ReadOnlySpan<SystemContainer> systemsSpan = systems.AsSpan();
            for (int i = 0; i < systemsSpan.Length; i++)
            {
                if (systemsSpan[i].System is T)
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
        public readonly T GetFirst<T>() where T : ISystem
        {
            ThrowIfSystemIsMissing<T>();

            ReadOnlySpan<SystemContainer> systemsSpan = systems.AsSpan();
            for (int i = 0; i < systemsSpan.Length; i++)
            {
                if (systemsSpan[i].System is T system)
                {
                    return system;
                }
            }

            throw new InvalidOperationException($"System of type {typeof(T)} not found");
        }

        /// <summary>
        /// Tries to retrieve the first system of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool TryGetFirst<T>([NotNullWhen(true)] out T? system) where T : notnull, ISystem
        {
            ReadOnlySpan<SystemContainer> systemsSpan = systems.AsSpan();
            for (int i = 0; i < systemsSpan.Length; i++)
            {
                if (systemsSpan[i].System is T foundSystem)
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
        public readonly ISystem GetSystem(int index)
        {
            ThrowIfIndexIsOutOfBounds(index);

            return systems[index].System;
        }

        /// <summary>
        /// Updates all systems forward.
        /// </summary>
        public void Update()
        {
            DateTime timeNow = DateTime.UtcNow;
            double deltaTime = (timeNow - lastUpdateTime).TotalSeconds;
            lastUpdateTime = timeNow;
            runTime += deltaTime;

            ReadOnlySpan<SystemContainer> systemsSpan = systems.AsSpan();
            for (int i = 0; i < systemsSpan.Length; i++)
            {
                systemsSpan[i].Update(this, deltaTime);
            }
        }

        /// <summary>
        /// Updates all systems forward and retreives the delta time.
        /// </summary>
        public void Update(out double deltaTime)
        {
            DateTime timeNow = DateTime.UtcNow;
            deltaTime = (timeNow - lastUpdateTime).TotalSeconds;
            lastUpdateTime = timeNow;
            runTime += deltaTime;

            ReadOnlySpan<SystemContainer> systemsSpan = systems.AsSpan();
            for (int i = 0; i < systemsSpan.Length; i++)
            {
                systemsSpan[i].Update(this, deltaTime);
            }
        }

        /// <summary>
        /// Updates all systems forward with the specified <paramref name="deltaTime"/>.
        /// </summary>
        public void Update(double deltaTime)
        {
            lastUpdateTime = DateTime.UtcNow;
            runTime += deltaTime;

            ReadOnlySpan<SystemContainer> systemsSpan = systems.AsSpan();
            for (int i = 0; i < systemsSpan.Length; i++)
            {
                systemsSpan[i].Update(this, deltaTime);
            }
        }

        /// <summary>
        /// Broadcasts the given <paramref name="message"/> to all systems.
        /// </summary>
        public readonly void Broadcast<T>(T message) where T : unmanaged
        {
            TypeMetadata messageType = TypeMetadata.GetOrRegister<T>();
            if (receiversMap.TryGetValue(messageType, out List<MessageReceiver> receivers))
            {
                using Message container = Message.Create(message);
                Span<MessageReceiver> receiversSpan = receivers.AsSpan();
                for (int i = 0; i < receiversSpan.Length; i++)
                {
                    receiversSpan[i].Invoke(container);
                }
            }
        }

        /// <summary>
        /// Broadcasts the given <paramref name="message"/> to all systems.
        /// </summary>
        public readonly void Broadcast<T>(ref T message) where T : unmanaged
        {
            TypeMetadata messageType = TypeMetadata.GetOrRegister<T>();
            if (receiversMap.TryGetValue(messageType, out List<MessageReceiver> receivers))
            {
                using Message container = Message.Create(message);
                Span<MessageReceiver> receiversSpan = receivers.AsSpan();
                for (int i = 0; i < receiversSpan.Length; i++)
                {
                    receiversSpan[i].Invoke(container);
                }

                message = container.data.Read<T>();
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfSystemIsMissing<T>() where T : ISystem
        {
            ReadOnlySpan<SystemContainer> systemsSpan = systems.AsSpan();
            for (int i = 0; i < systemsSpan.Length; i++)
            {
                if (systemsSpan[i].System is T)
                {
                    return;
                }
            }

            throw new InvalidOperationException($"System of type {typeof(T)} not found");
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfSystemIsMissing(object system)
        {
            ReadOnlySpan<SystemContainer> systemsSpan = systems.AsSpan();
            for (int i = 0; i < systemsSpan.Length; i++)
            {
                if (systemsSpan[i].System == system)
                {
                    return;
                }
            }

            throw new InvalidOperationException($"System instance {system} not found");
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfIndexIsOutOfBounds(int index)
        {
            if (index < 0 || index >= systems.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of bounds. Count: {systems.Count}");
            }
        }
    }
}