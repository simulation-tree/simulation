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
        private readonly delegate* unmanaged<SystemContainer, World, Allocation, void> value;

        /// <summary>
        /// Creates a new <see cref="HandleMessage"/> instance.
        /// </summary>
        public HandleMessage(delegate* unmanaged<SystemContainer, World, Allocation, void> value)
        {
            this.value = value;
        }
#else
        private readonly delegate*<SystemContainer, World, Allocation, void> value;

        public HandleFunction(delegate*<SystemContainer, World, Allocation, void> value)
        {
            this.value = value;
        }
#endif
        /// <summary>
        /// Invokes the function.
        /// </summary>
        public readonly void Invoke(SystemContainer container, World programWorld, Allocation message)
        {
            value(container, programWorld, message);
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
    }
}