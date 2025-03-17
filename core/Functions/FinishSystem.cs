using System;
using Worlds;

namespace Simulation.Functions
{
    /// <summary>
    /// The <see cref="ISystem.Finish"/> function pointer.
    /// </summary>
    public unsafe readonly struct FinishSystem : IEquatable<FinishSystem>
    {
#if NET
        private readonly delegate* unmanaged<SystemContainer, World, void> value;

        /// <inheritdoc/>
        public FinishSystem(delegate* unmanaged<SystemContainer, World, void> value)
        {
            this.value = value;
        }
#else
        private readonly delegate*<SystemContainer, World, void> value;
        
        /// <inheritdoc/>
        public FinishSystem(delegate*<SystemContainer, World, void> value)
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
                return nameof(FinishSystem);
            }
        }

        /// <summary>
        /// Calls this function.
        /// </summary>
        public readonly void Invoke(SystemContainer container, World world)
        {
            value(container, world);
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is FinishSystem system && Equals(system);
        }

        /// <inheritdoc/>
        public readonly bool Equals(FinishSystem other)
        {
            return (nint)value == (nint)other.value;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return ((nint)value).GetHashCode();
        }

        /// <inheritdoc/>
        public static bool operator ==(FinishSystem left, FinishSystem right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(FinishSystem left, FinishSystem right)
        {
            return !(left == right);
        }
    }
}