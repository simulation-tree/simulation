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
        private readonly delegate* unmanaged<SystemContainer, World, Allocation, Boolean> value;

        /// <summary>
        /// Creates a new <see cref="HandleMessage"/> instance.
        /// </summary>
        public HandleMessage(delegate* unmanaged<SystemContainer, World, Allocation, Boolean> value)
        {
            this.value = value;
        }
#else
        private readonly delegate*<SystemContainer, World, Allocation, Boolean> value;

        public HandleFunction(delegate*<SystemContainer, World, Allocation, Boolean> value)
        {
            this.value = value;
        }
#endif
        /// <summary>
        /// Invokes the function.
        /// </summary>
        public readonly bool Invoke(SystemContainer container, World programWorld, Allocation message)
        {
            return value(container, programWorld, message);
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

        public readonly struct Boolean
        {
            private readonly byte value;

            public Boolean(bool value)
            {
                this.value = value ? (byte)1 : (byte)0;
            }

            public static implicit operator bool(Boolean value)
            {
                return value.value == 1;
            }

            public static implicit operator Boolean(bool value)
            {
                return new Boolean(value);
            }
        }
    }
}