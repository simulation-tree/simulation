using System;
using Worlds;

namespace Simulation.Functions
{
    /// <summary>
    /// Describes a function that initializes a system.
    /// </summary>
    public unsafe readonly struct StartSystem : IEquatable<StartSystem>
    {
#if NET
        private readonly delegate* unmanaged<SystemContainer, World, void> value;

        /// <summary>
        /// Creates a new <see cref="StartSystem"/> with the given <paramref name="value"/>.
        /// </summary>
        public StartSystem(delegate* unmanaged<SystemContainer, World, void> value)
        {
            this.value = value;
        }

#else
        private readonly delegate*<SystemContainer, World, void> value;

        public StartSystem(delegate*<SystemContainer, World, void> value)
        {
            this.value = value;
        }
#endif
        /// <inheritdoc/>
        public override string ToString()
        {
            if ((nint)value == default)
            {
                return "Default";
            }
            else
            {
                return nameof(StartSystem);
            }
        }

        /// <summary>
        /// Invokes the function.
        /// </summary>
        public readonly void Invoke(SystemContainer container, World world)
        {
            value(container, world);
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is StartSystem system && Equals(system);
        }

        /// <inheritdoc/>
        public readonly bool Equals(StartSystem other)
        {
            return (nint)value == (nint)other.value;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return ((nint)value).GetHashCode();
        }

        /// <inheritdoc/>
        public static bool operator ==(StartSystem left, StartSystem right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(StartSystem left, StartSystem right)
        {
            return !(left == right);
        }
    }
}