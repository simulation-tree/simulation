using System;
using Worlds;

namespace Simulation.Functions
{
    /// <summary>
    /// A function that iterates over a system.
    /// </summary>
    public unsafe readonly struct UpdateSystem : IEquatable<UpdateSystem>
    {
#if NET
        private readonly delegate* unmanaged<SystemContainer, World, TimeSpan, void> value;

        /// <summary>
        /// Creates a new iterate function.
        /// </summary>
        public UpdateSystem(delegate* unmanaged<SystemContainer, World, TimeSpan, void> value)
        {
            this.value = value;
        }
#else
        private readonly delegate*<SystemContainer, World, TimeSpan, void> value;

        public UpdateSystem(delegate*<SystemContainer, World, TimeSpan, void> value)
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
                return nameof(UpdateSystem);
            }
        }

        /// <summary>
        /// Invokes the function.
        /// </summary>
        public readonly void Invoke(SystemContainer container, World world, TimeSpan delta)
        {
            value(container, world, delta);
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is UpdateSystem system && Equals(system);
        }

        /// <inheritdoc/>
        public readonly bool Equals(UpdateSystem other)
        {
            return (nint)value == (nint)other.value;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return ((nint)value).GetHashCode();
        }

        /// <inheritdoc/>
        public static bool operator ==(UpdateSystem left, UpdateSystem right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(UpdateSystem left, UpdateSystem right)
        {
            return !(left == right);
        }
    }
}