using System;
using Worlds;

namespace Simulation.Functions
{
    /// <summary>
    /// Finalize function for a system.
    /// </summary>
    public unsafe readonly struct FinishSystem : IEquatable<FinishSystem>
    {
#if NET
        private readonly delegate* unmanaged<SystemContainer, World, void> value;

        /// <summary>
        /// Creates a new <see cref="FinishSystem"/>.
        /// </summary>
        public FinishSystem(delegate* unmanaged<SystemContainer, World, void> value)
        {
            this.value = value;
        }
#else
        private readonly delegate*<SystemContainer, World, void> value;

        public FinishSystem(delegate*<SystemContainer, World, void> value)
        {
            this.value = value;
        }
#endif
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

        public readonly override bool Equals(object? obj)
        {
            return obj is FinishSystem system && Equals(system);
        }

        public readonly bool Equals(FinishSystem other)
        {
            return (nint)value == (nint)other.value;
        }

        public readonly override int GetHashCode()
        {
            return ((nint)value).GetHashCode();
        }

        public static bool operator ==(FinishSystem left, FinishSystem right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FinishSystem left, FinishSystem right)
        {
            return !(left == right);
        }
    }
}