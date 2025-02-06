using Collections;
using Simulation.Functions;
using System;
using System.Diagnostics;
using Unmanaged;
using Worlds;

namespace Simulation
{
    /// <summary>
    /// Contains a system added to a <see cref="Simulation.Simulator"/>.
    /// </summary>
    public readonly unsafe struct SystemContainer : IDisposable
    {
        /// <summary>
        /// The <see cref="RuntimeTypeHandle"/> of this system.
        /// </summary>
        public readonly nint systemType;

        /// <summary>
        /// The simulator that this system was created in.
        /// </summary>
        public readonly Simulator simulator;

        private readonly Allocation allocation;
        private readonly Allocation input;
        private readonly Dictionary<nint, HandleMessage> handlers;
        private readonly List<World> programWorlds;
        private readonly StartSystem start;
        private readonly UpdateSystem update;
        private readonly FinishSystem finish;

        /// <summary>
        /// The world that this system was created in.
        /// </summary>
        public readonly World World => simulator.World;

        /// <summary>
        /// The <see cref="Type"/> of this system.
        /// </summary>
        public readonly Type Type
        {
            get
            {
                RuntimeTypeHandle handle = RuntimeTypeTable.GetHandle(systemType);
                return Type.GetTypeFromHandle(handle) ?? throw new();
            }
        }

        /// <summary>
        /// The input data that this system was created with.
        /// </summary>
        public readonly Allocation Input => input;

        /// <summary>
        /// Creates a new <see cref="SystemContainer"/> instance.
        /// </summary>
        public SystemContainer(Simulator simulator, Allocation allocation, Allocation input, nint systemType, Dictionary<nint, HandleMessage> handlers, StartSystem start, UpdateSystem update, FinishSystem finish)
        {
            this.simulator = simulator;
            this.allocation = allocation;
            this.input = input;
            this.systemType = systemType;
            this.handlers = handlers;
            programWorlds = new();
            this.start = start;
            this.update = update;
            this.finish = finish;
        }

        /// <summary>
        /// Builds a string representation of the system.
        /// </summary>
        public readonly uint ToString(USpan<char> buffer)
        {
            string name = Type.Name;
            for (uint i = 0; i < name.Length; i++)
            {
                buffer[i] = name[(int)i];
            }

            return (uint)name.Length;
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[256];
            uint length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfDisposed()
        {
            if (programWorlds.IsDisposed)
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

            for (uint i = programWorlds.Count - 1; i != uint.MaxValue; i--)
            {
                Finalize(programWorlds[i]);
            }

            input.Dispose();
            allocation.Dispose();
            programWorlds.Dispose();
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
        /// Checks if this system is initialized with the given <paramref name="programWorld"/>.
        /// </summary>
        public readonly bool IsInitializedWith(World programWorld)
        {
            return programWorlds.Contains(programWorld);
        }

        /// <summary>
        /// Initializes this system with the given world as its context.
        /// </summary>
        public readonly void Start(World programWorld)
        {
            ThrowIfAlreadyInitializedWith(programWorld);

            programWorlds.Add(programWorld);
            start.Invoke(this, programWorld);
        }

        /// <summary>
        /// Updates this system with the given world as its context.
        /// </summary>
        public readonly void Update(World programWorld, TimeSpan delta)
        {
            ThrowIfNotInitializedWith(programWorld);

            update.Invoke(this, programWorld, delta);
        }

        /// <summary>
        /// Finalizes this system with the given world as its context.
        /// </summary>
        public readonly void Finalize(World programWorld)
        {
            ThrowIfNotInitializedWith(programWorld);

            finish.Invoke(this, programWorld);
            programWorlds.TryRemoveBySwapping(programWorld);
        }

        /// <summary>
        /// Tries to handle the given <paramref name="message"/>.
        /// </summary>
        public readonly bool TryHandleMessage(World programWorld, nint messageType, Allocation message)
        {
            if (handlers.TryGetValue(messageType, out HandleMessage handler))
            {
                return handler.Invoke(this, programWorld, message);
            }

            return false;
        }

        /// <summary>
        /// Tries to handle the given <paramref name="message"/>.
        /// </summary>
        public readonly bool TryHandleMessage<T>(World programWorld, Allocation message) where T : unmanaged
        {
            nint messageType = RuntimeTypeTable.GetAddress<T>();
            return TryHandleMessage(programWorld, messageType, message);
        }

        /// <summary>
        /// Tries to handle the given <paramref name="message"/>.
        /// </summary>
        public readonly bool TryHandleMessage<T>(World programWorld, ref T message) where T : unmanaged
        {
            nint messageType = RuntimeTypeTable.GetAddress<T>();
            using Allocation allocation = Allocation.Create(message);
            if (TryHandleMessage(programWorld, messageType, allocation))
            {
                message = allocation.Read<T>();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to handle the given <paramref name="message"/>.
        /// </summary>
        public readonly bool TryHandleMessage<T>(World programWorld, T message) where T : unmanaged
        {
            nint messageType = RuntimeTypeTable.GetAddress<T>();
            using Allocation allocation = Allocation.Create(message);
            if (TryHandleMessage(programWorld, messageType, allocation))
            {
                message = allocation.Read<T>();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if this system is not initialized with the given <paramref name="programWorld"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        [Conditional("DEBUG")]
        public readonly void ThrowIfNotInitializedWith(World programWorld)
        {
            if (!IsInitializedWith(programWorld))
            {
                throw new InvalidOperationException($"System `{this}` is not initialized with world `{programWorld}`");
            }
        }

        [Conditional("DEBUG")]
        public readonly void ThrowIfAlreadyInitializedWith(World programWorld)
        {
            if (IsInitializedWith(programWorld))
            {
                throw new InvalidOperationException($"System `{this}` is already initialized with world `{programWorld}`");
            }
        }

        [Conditional("DEBUG")]
        public readonly void ThrowIfNotSameType<T>() where T : unmanaged, ISystem
        {
            nint systemType = RuntimeTypeTable.GetAddress<T>();
            if (this.systemType != systemType)
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

        private readonly uint index;
        private readonly nint systemType;

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
        internal SystemContainer(Simulator simulator, uint index, nint systemType)
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
