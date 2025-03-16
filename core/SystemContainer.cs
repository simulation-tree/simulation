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
    public readonly unsafe struct SystemContainer : IDisposable, IEquatable<SystemContainer>
    {
        /// <summary>
        /// The type of this system.
        /// </summary>
        public readonly TypeLayout type;

        /// <summary>
        /// The simulator that this system was created in.
        /// </summary>
        public readonly Simulator simulator;

        private readonly MemoryAddress allocation;
        private readonly List<World> worlds;
        private readonly StartSystem start;
        private readonly UpdateSystem update;
        private readonly FinishSystem finish;
        private readonly DisposeSystem dispose;
        internal readonly int index;
        internal readonly int parent;

        /// <summary>
        /// The world of the simulator that this system was created in.
        /// </summary>
        public readonly World World => simulator.World;

        /// <summary>
        /// Creates a new <see cref="SystemContainer"/> instance.
        /// </summary>
        internal SystemContainer(int index, int parent, Simulator simulator, MemoryAddress allocation, TypeLayout type, StartSystem start, UpdateSystem update, FinishSystem finish, DisposeSystem dispose)
        {
            this.index = index;
            this.parent = parent;
            this.simulator = simulator;
            this.allocation = allocation;
            this.type = type;
            this.start = start;
            this.update = update;
            this.finish = finish;
            this.dispose = dispose;
            worlds = new();
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            return type.ToString();
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

            dispose.Invoke(this, simulator.World);
            allocation.Dispose();
            worlds.Dispose();
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
            if (!type.Is<T>())
            {
                throw new InvalidOperationException($"System `{this}` is not of type `{typeof(T)}`");
            }
        }

        internal readonly SystemContainer<T> As<T>() where T : unmanaged, ISystem
        {
            return new(simulator, index, type);
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is SystemContainer container && Equals(container);
        }

        public readonly bool Equals(SystemContainer other)
        {
            return allocation.Equals(other.allocation);
        }

        public readonly override int GetHashCode()
        {
            return allocation.GetHashCode();
        }

        public static bool operator ==(SystemContainer left, SystemContainer right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SystemContainer left, SystemContainer right)
        {
            return !(left == right);
        }
    }

    /// <summary>
    /// Generic container for of a <typeparamref name="T"/> system added to a <see cref="Simulator"/>.
    /// </summary>
    public unsafe readonly struct SystemContainer<T> where T : unmanaged, ISystem
    {
        public readonly Simulator simulator;
        public readonly TypeLayout type;

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

        private readonly SystemContainer Container => simulator.Systems[index];

        /// <summary>
        /// Initializes a new <see cref="SystemContainer{T}"/> instance with an
        /// existing system index.
        /// </summary>
        internal SystemContainer(Simulator simulator, int index, TypeLayout type)
        {
            this.simulator = simulator;
            this.index = index;
            this.type = type;
        }

        public readonly void RemoveSelf()
        {
            simulator.RemoveSystem<T>();
        }

        [Conditional("DEBUG")]
        public readonly void ThrowIfSystemIsDifferent()
        {
            if (Container.type != type)
            {
                throw new InvalidOperationException($"System at index `{index}` is not of expected type `{typeof(T)}`");
            }
        }

        public static implicit operator SystemContainer(SystemContainer<T> container)
        {
            return container.Container;
        }

        public static implicit operator T(SystemContainer<T> container)
        {
            return container.Value;
        }
    }
}