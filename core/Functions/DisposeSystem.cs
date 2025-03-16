using System;
using Worlds;

namespace Simulation.Functions
{
    public unsafe readonly struct DisposeSystem : IEquatable<DisposeSystem>
    {
#if NET
        private readonly delegate* unmanaged<SystemContainer, World, void> value;

        public DisposeSystem(delegate* unmanaged<SystemContainer, World, void> value)
        {
            this.value = value;
        }
#else
        private readonly delegate*<SystemContainer, World, void> value;

        public DisposeSystem(delegate* unmanaged<SystemContainer, World, void> value)
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
                return nameof(DisposeSystem);
            }
        }

        public readonly void Invoke(SystemContainer container, World world)
        {
            value(container, world);
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is DisposeSystem system && Equals(system);
        }

        public readonly bool Equals(DisposeSystem other)
        {
            return (nint)value == (nint)other.value;
        }

        public readonly override int GetHashCode()
        {
            return ((nint)value).GetHashCode();
        }

        public static bool operator ==(DisposeSystem left, DisposeSystem right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DisposeSystem left, DisposeSystem right)
        {
            return !(left == right);
        }
    }
}