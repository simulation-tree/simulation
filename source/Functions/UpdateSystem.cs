using System;
using Worlds;

namespace Simulation.Functions
{
    /// <summary>
    /// A function that iterates over a system.
    /// </summary>
    public unsafe readonly struct UpdateSystem
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
        /// <summary>
        /// Invokes the function.
        /// </summary>
        public readonly void Invoke(SystemContainer container, World world, TimeSpan delta)
        {
            value(container, world, delta);
        }
    }
}