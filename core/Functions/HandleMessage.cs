using System;
using Unmanaged;
using Worlds;

namespace Simulation.Functions
{
    /// <summary>
    /// A function that handles a sent message.
    /// </summary>
    public unsafe readonly struct HandleMessage : IEquatable<HandleMessage>
    {
#if NET
        private readonly delegate* unmanaged<Input, StatusCode> value;

        /// <summary>
        /// Creates a new <see cref="HandleMessage"/> instance.
        /// </summary>
        public HandleMessage(delegate* unmanaged<Input, StatusCode> value)
        {
            this.value = value;
        }
#else
        private readonly delegate*<Input, StatusCode> value;

        public HandleMessage(delegate*<Input, StatusCode> value)
        {
            this.value = value;
        }
#endif
        /// <summary>
        /// Invokes the function.
        /// </summary>
        public readonly StatusCode Invoke(SystemContainer container, World world, MemoryAddress message)
        {
            return value(new(container, world, message));
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is HandleMessage function && Equals(function);
        }

        /// <inheritdoc/>
        public readonly bool Equals(HandleMessage other)
        {
            return ((nint)value) == ((nint)other.value);
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return ((nint)value).GetHashCode();
        }

        /// <inheritdoc/>
        public static bool operator ==(HandleMessage left, HandleMessage right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(HandleMessage left, HandleMessage right)
        {
            return !(left == right);
        }

        public readonly struct Input
        {
            public readonly SystemContainer system;
            public readonly World world;

            private readonly MemoryAddress data;

            public readonly Simulator Simulator => system.simulator;
            public readonly World SimulatorWorld => system.World;

            public Input(SystemContainer system, World world, MemoryAddress data)
            {
                this.system = system;
                this.world = world;
                this.data = data;
            }

            public readonly ref T ReadMessage<T>() where T : unmanaged
            {
                return ref data.Read<T>();
            }

            public readonly ref T ReadSystem<T>() where T : unmanaged, ISystem
            {
                return ref system.Read<T>();
            }
        }
    }
}