using Worlds;

namespace Simulation.Functions
{
    /// <summary>
    /// Describes a function that initializes a system.
    /// </summary>
    public unsafe readonly struct StartSystem
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

        public InitializeFunction(delegate*<SystemContainer, World, void> value)
        {
            this.value = value;
        }
#endif
        /// <summary>
        /// Invokes the function.
        /// </summary>
        public readonly void Invoke(SystemContainer container, World world)
        {
            value(container, world);
        }
    }
}