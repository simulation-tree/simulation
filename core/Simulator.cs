using Simulation.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Types;
using Unmanaged;

namespace Simulation
{
    /// <summary>
    /// Contains systems for updating and broadcasting messages to.
    /// </summary>
    [SkipLocalsInit]
    public class Simulator : IDisposable
    {
        private readonly Dictionary<TypeMetadata, List<Receive>> receiversMap;
        private readonly List<object> systems;
        private readonly List<Type> systemTypes;
        private readonly List<MessageReceiverLocator[]> receiverLocators;
        private readonly Collections.Generic.Stack<Message> messageContainers;
        private bool disposed;

        /// <summary>
        /// Amount of systems added.
        /// </summary>
        public int Count => systems.Count;

        /// <summary>
        /// All systems added.
        /// </summary>
        public IReadOnlyList<object> Systems => systems;

        /// <summary>
        /// Checks if this instance has been disposed.
        /// </summary>
        public bool IsDisposed => disposed;

        /// <summary>
        /// Creates a new simulator.
        /// </summary>
        public Simulator()
        {
            receiversMap = new(4);
            systems = new(4);
            systemTypes = new(4);
            receiverLocators = new(4);
            messageContainers = new(4);
        }

        /// <summary>
        /// Disposes the simulator.
        /// <para>
        /// When overriding, call this function last.
        /// </para>
        /// </summary>
        public virtual void Dispose()
        {
            ThrowIfDisposed();

            disposed = true;
            while (messageContainers.TryPop(out Message message))
            {
                message.Dispose();
            }

            messageContainers.Dispose();
        }

        /// <summary>
        /// Adds the given <paramref name="system"/>.
        /// </summary>
        public void Add<T>(T system) where T : notnull
        {
            ThrowIfDisposed();
            AddListeners(system);
            systems.Add(system);

            Type systemType = typeof(T);
            if (!systemTypes.Contains(systemType))
            {
                systemTypes.Add(typeof(T));
            }

            OnAdded(system);
        }

        /// <summary>
        /// Removes the first system found of type <typeparamref name="T"/>,
        /// and disposes it by default.
        /// </summary>
        /// <returns>The removed system.</returns>
        public T Remove<T>(bool dispose = true) where T : notnull
        {
            ThrowIfDisposed();

            int count = systems.Count;
            for (int i = 0; i < count; i++)
            {
                if (systems[i] is T system)
                {
                    Remove(i, dispose);
                    return system;
                }
            }

            throw new MissingSystemTypeException(typeof(T));
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

            int count = systems.Count;
            for (int i = 0; i < count; i++)
            {
                if (systems[i] == system)
                {
                    Remove(i, false);
                    return;
                }
            }

            throw new NullReferenceException($"System instance `{system}` not found");
        }

        private void Remove(int index, bool dispose)
        {
            object system = systems[index];
            systems.RemoveAt(index);
            RemoveListeners(index);
            Type type = system.GetType();
            int count = 0;
            for (int i = 0; i < systems.Count; i++)
            {
                if (systems[i].GetType() == type)
                {
                    count++;
                }
            }

            if (count == 0)
            {
                systemTypes.Remove(type);
            }

            OnRemoved(system);
            if (dispose && system is IDisposable disposableSystem)
            {
                disposableSystem.Dispose();
            }
        }

        /// <summary>
        /// Callback after a system has been added.
        /// </summary>
        protected virtual void OnAdded(object system)
        {
        }

        /// <summary>
        /// Callback after a system has been removed.
        /// </summary>
        protected virtual void OnRemoved(object system)
        {
        }

        private void AddListeners<T>(T system) where T : notnull
        {
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
                    receivers.Add(handler.Callback);
                    handler.Dispose();
                }

                receiverLocators.Add(locators);
            }
            else
            {
                receiverLocators.Add(Array.Empty<MessageReceiverLocator>());
            }
        }

        private void RemoveListeners(int index)
        {
            ReadOnlySpan<MessageReceiverLocator> locators = receiverLocators[index].AsSpan();
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
                        Span<MessageReceiverLocator> otherLocators = receiverLocators[c].AsSpan();
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
        }

        /// <summary>
        /// Checks if the simulator contains a system of type <typeparamref name="T"/>.
        /// </summary>
        public bool Contains<T>() where T : notnull
        {
            ThrowIfDisposed();

            return systemTypes.Contains(typeof(T));
        }

        /// <summary>
        /// Retrieves the first system of type <typeparamref name="T"/>.
        /// <para>
        /// An exception will be thrown if a system is not found.
        /// </para>
        /// </summary>
        public T GetFirst<T>() where T : notnull
        {
            ThrowIfDisposed();

            int count = systems.Count;
            for (int i = 0; i < count; i++)
            {
                if (systems[i] is T system)
                {
                    return system;
                }
            }

            throw new MissingSystemTypeException(typeof(T));
        }

        /// <summary>
        /// Tries to retrieve the first system of type <typeparamref name="T"/>.
        /// </summary>
        public bool TryGetFirst<T>([NotNullWhen(true)] out T? system) where T : notnull
        {
            ThrowIfDisposed();

            int count = systems.Count;
            for (int i = 0; i < count; i++)
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
        /// Broadcasts the given <paramref name="message"/> to all systems.
        /// </summary>
        public unsafe void Broadcast<T>(T message) where T : unmanaged
        {
            ThrowIfDisposed();

            TypeMetadata messageType = TypeMetadata.GetOrRegister<T>();
            if (receiversMap.TryGetValue(messageType, out List<Receive>? receivers))
            {
                //get or create message container
                int messageLength = sizeof(T);
                if (messageContainers.TryPop(out Message messageContainer))
                {
                    if (messageLength > messageContainer.byteCapacity)
                    {
                        messageContainer.byteCapacity = messageLength.GetNextPowerOf2();
                        MemoryAddress.Resize(ref messageContainer.memory, messageContainer.byteCapacity);
                    }
                }
                else
                {
                    messageContainer = new(messageLength);
                }

                //broadcast
                messageContainer.memory.Write(message);
                int count = receivers.Count;
                for (int i = 0; i < count; i++)
                {
                    receivers[i].Invoke(messageContainer.memory);
                }

                //return message container
                messageContainers.Push(messageContainer);
            }
        }

        /// <summary>
        /// Broadcasts the given <paramref name="message"/> to all systems.
        /// </summary>
        public unsafe void Broadcast<T>(ref T message) where T : unmanaged
        {
            ThrowIfDisposed();

            TypeMetadata messageType = TypeMetadata.GetOrRegister<T>();
            if (receiversMap.TryGetValue(messageType, out List<Receive>? receivers))
            {
                //get or create message container
                int messageLength = sizeof(T);
                if (messageContainers.TryPop(out Message messageContainer))
                {
                    if (messageLength > messageContainer.byteCapacity)
                    {
                        messageContainer.byteCapacity = messageLength.GetNextPowerOf2();
                        MemoryAddress.Resize(ref messageContainer.memory, messageContainer.byteCapacity);
                    }
                }
                else
                {
                    messageContainer = new(messageLength);
                }

                //broadcast
                messageContainer.memory.Write(message);
                int count = receivers.Count;
                for (int i = 0; i < count; i++)
                {
                    receivers[i].Invoke(messageContainer.memory);
                }

                //return message container
                message = messageContainer.memory.Read<T>();
                messageContainers.Push(messageContainer);
            }
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

            throw new NullReferenceException($"System instance `{system}` not found in the simulator");
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
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Simulator), "Simulator has already been disposed");
            }
        }

        private struct Message : IDisposable
        {
            public int byteCapacity;
            public MemoryAddress memory;

            public Message(int byteCapacity)
            {
                this.byteCapacity = byteCapacity;
                memory = MemoryAddress.Allocate(byteCapacity);
            }

            public void Dispose()
            {
                memory.Dispose();
            }
        }
    }
}