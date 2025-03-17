using System;
using Worlds;

namespace Simulation.Functions
{
    /// <summary>
    /// The <see cref="IDisposable.Dispose"/> function pointer.
    /// </summary>
    public unsafe readonly struct DisposeSystem : IEquatable<DisposeSystem>
    {
#if NET
        private readonly delegate* unmanaged<SystemContainer, World, void> value;

        /// <inheritdoc/>
        public DisposeSystem(delegate* unmanaged<SystemContainer, World, void> value)
        {
            this.value = value;
        }
#else
        private readonly delegate*<SystemContainer, World, void> value;
        
        /// <inheritdoc/>
        public DisposeSystem(delegate* unmanaged<SystemContainer, World, void> value)
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
                return nameof(DisposeSystem);
            }
        }

        /// <inheritdoc/>
        public readonly void Invoke(SystemContainer container, World world)
        {
            value(container, world);
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is DisposeSystem system && Equals(system);
        }

        /// <inheritdoc/>
        public readonly bool Equals(DisposeSystem other)
        {
            return (nint)value == (nint)other.value;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return ((nint)value).GetHashCode();
        }

        /// <inheritdoc/>
        public static bool operator ==(DisposeSystem left, DisposeSystem right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(DisposeSystem left, DisposeSystem right)
        {
            return !(left == right);
        }
    }
}