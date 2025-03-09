using Collections;
using Collections.Generic;
using Simulation.Functions;
using System;
using System.Diagnostics;
using Types;
using Unmanaged;
using Worlds;

namespace Simulation
{
    /// <summary>
    /// Contains a system added to a <see cref="Simulator"/>.
    /// </summary>
    public readonly unsafe struct SystemContainer : IDisposable
    {
        /// <summary>
        /// The type of this system.
        /// </summary>
        public readonly TypeLayout systemType;

        /// <summary>
        /// The simulator that this system was created in.
        /// </summary>
        public readonly Simulator simulator;

        private readonly MemoryAddress allocation;
        private readonly MemoryAddress input;
        private readonly Dictionary<TypeLayout, HandleMessage> handlers;
        private readonly List<World> worlds;
        private readonly StartSystem start;
        private readonly UpdateSystem update;
        private readonly FinishSystem finish;

        /// <summary>
        /// The world of the simulator that this system was created in.
        /// </summary>
        public readonly World World => simulator.World;

        /// <summary>
        /// The <see cref="System.Type"/> of this system.
        /// </summary>
        public readonly Type Type => systemType.SystemType;

        /// <summary>
        /// The input data that this system was created with.
        /// </summary>
        public readonly MemoryAddress Input => input;

        /// <summary>
        /// Creates a new <see cref="SystemContainer"/> instance.
        /// </summary>
        public SystemContainer(Simulator simulator, MemoryAddress allocation, MemoryAddress input, TypeLayout systemType, Dictionary<TypeLayout, HandleMessage> handlers, StartSystem start, UpdateSystem update, FinishSystem finish)
        {
            this.simulator = simulator;
            this.allocation = allocation;
            this.input = input;
            this.systemType = systemType;
            this.handlers = handlers;
            worlds = new();
            this.start = start;
            this.update = update;
            this.finish = finish;
        }

        /// <summary>
        /// Builds a string representation of the system.
        /// </summary>
        public readonly int ToString(Span<char> destination)
        {
            string name = Type.Name;
            for (int i = 0; i < name.Length; i++)
            {
                destination[i] = name[i];
            }

            return name.Length;
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            Span<char> buffer = stackalloc char[256];
            int length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfDisposed()
        {
            if (worlds.IsDisposed)
            {
                throw new ObjectDisposedException($"System `{this}` has been disposed");
            }
        }

        /// <summary>
        /// Finalizes the system and disposes of its resources.
        /// </summary>
        public readonly void Dispose()
        {
            ThrowIfDisposed();

            for (int i = worlds.Count - 1; i >= 0; i--)
            {
                Finalize(worlds[i]);
            }

            input.Dispose();
            allocation.Dispose();
            worlds.Dispose();
            handlers.Dispose();
        }

        /// <summary>
        /// Reads the system data of the given type.
        /// </summary>
        public readonly ref T Read<T>() where T : unmanaged, ISystem
        {
            ThrowIfNotSameType<T>();

            return ref allocation.Read<T>();
        }

        /// <summary>
        /// Writes the system data.
        /// </summary>
        public readonly void Write<T>(T value) where T : unmanaged, ISystem
        {
            ThrowIfNotSameType<T>();

            allocation.Write(value);
        }

        /// <summary>
        /// Checks if this system is initialized with the given <paramref name="world"/>.
        /// </summary>
        public readonly bool IsInitializedWith(World world)
        {
            return worlds.Contains(world);
        }

        /// <summary>
        /// Initializes this system with the given world as its context.
        /// </summary>
        public readonly void Start(World world)
        {
            ThrowIfAlreadyInitializedWith(world);

            worlds.Add(world);
            start.Invoke(this, world);
        }

        /// <summary>
        /// Updates this system with the given world as its context.
        /// </summary>
        public readonly void Update(World world, TimeSpan delta)
        {
            ThrowIfNotInitializedWith(world);

            update.Invoke(this, world, delta);
        }

        /// <summary>
        /// Finalizes this system with the given world as its context.
        /// </summary>
        public readonly void Finalize(World world)
        {
            ThrowIfNotInitializedWith(world);

            finish.Invoke(this, world);
            worlds.TryRemoveBySwapping(world);
        }

        /// <summary>
        /// Tries to handle the given <paramref name="message"/>.
        /// </summary>
        /// <returns><see langword="default"/> if no handler was found.</returns>
        public readonly StatusCode TryHandleMessage(World world, TypeLayout messageType, MemoryAddress message)
        {
            if (handlers.TryGetValue(messageType, out HandleMessage handler))
            {
                return handler.Invoke(this, world, message);
            }

            return default;
        }

        /// <summary>
        /// Tries to handle the given <paramref name="message"/>.
        /// </summary>
        /// <returns><see langword="default"/> if no handler was found.</returns>
        public readonly StatusCode TryHandleMessage<T>(World world, MemoryAddress message) where T : unmanaged
        {
            TypeLayout messageType = TypeRegistry.GetOrRegister<T>();
            return TryHandleMessage(world, messageType, message);
        }

        /// <summary>
        /// Tries to handle the given <paramref name="message"/>.
        /// </summary>
        /// <returns><see langword="default"/> if no handler was found.</returns>
        public readonly StatusCode TryHandleMessage<T>(World world, ref T message) where T : unmanaged
        {
            TypeLayout messageType = TypeRegistry.GetOrRegister<T>();
            using MemoryAddress allocation = MemoryAddress.AllocateValue(message);
            StatusCode statusCode = TryHandleMessage(world, messageType, allocation);
            if (statusCode != default)
            {
                message = allocation.Read<T>();
            }

            return statusCode;
        }

        /// <summary>
        /// Tries to handle the given <paramref name="message"/>.
        /// </summary>
        /// <returns><see langword="default"/> if no handler was found.</returns>
        public readonly StatusCode TryHandleMessage<T>(World world, T message) where T : unmanaged
        {
            TypeLayout messageType = TypeRegistry.GetOrRegister<T>();
            using MemoryAddress allocation = MemoryAddress.AllocateValue(message);
            StatusCode statusCode = TryHandleMessage(world, messageType, allocation);
            if (statusCode != default)
            {
                message = allocation.Read<T>();
            }

            return statusCode;
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if this system is not initialized with the given <paramref name="world"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        [Conditional("DEBUG")]
        public readonly void ThrowIfNotInitializedWith(World world)
        {
            if (!IsInitializedWith(world))
            {
                throw new InvalidOperationException($"System `{this}` is not initialized with world `{world}`");
            }
        }

        [Conditional("DEBUG")]
        public readonly void ThrowIfAlreadyInitializedWith(World world)
        {
            if (IsInitializedWith(world))
            {
                throw new InvalidOperationException($"System `{this}` is already initialized with world `{world}`");
            }
        }

        [Conditional("DEBUG")]
        public readonly void ThrowIfNotSameType<T>() where T : unmanaged, ISystem
        {
            TypeLayout type = TypeRegistry.GetOrRegister<T>();
            if (systemType != type)
            {
                throw new InvalidOperationException($"System `{this}` is not of type `{typeof(T)}`");
            }
        }
    }

    /// <summary>
    /// Generic container for of a <typeparamref name="T"/> system added to a <see cref="Simulation.Simulator"/>.
    /// </summary>
    public unsafe readonly struct SystemContainer<T> where T : unmanaged, ISystem
    {
        public readonly Simulator simulator;
        public readonly TypeLayout systemType;

        private readonly int index;

        /// <summary>
        /// The system's data.
        /// </summary>
        public readonly ref T Value
        {
            get
            {
                ThrowIfSystemIsDifferent();

                return ref Container.Read<T>();
            }
        }

        /// <summary>
        /// The world that this system was created in.
        /// </summary>
        public readonly World World => simulator.World;

        private unsafe readonly ref SystemContainer Container => ref simulator.Systems[index];

        /// <summary>
        /// Initializes a new <see cref="SystemContainer{T}"/> instance with an
        /// existing system index.
        /// </summary>
        internal SystemContainer(Simulator simulator, int index, TypeLayout systemType)
        {
            this.simulator = simulator;
            this.index = index;
            this.systemType = systemType;
        }

        public readonly void RemoveSelf()
        {
            simulator.RemoveSystem<T>();
        }

        [Conditional("DEBUG")]
        public readonly void ThrowIfSystemIsDifferent()
        {
            if (Container.systemType != systemType)
            {
                throw new InvalidOperationException($"System at index `{index}` is not of expected type `{typeof(T)}`");
            }
        }

        public static implicit operator SystemContainer(SystemContainer<T> container)
        {
            return container.Container;
        }
    }
}
