using Worlds;

namespace Simulation.Functions
{
    /// <summary>
    /// Finalize function for a system.
    /// </summary>
    public unsafe readonly struct FinishSystem
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

        public FinalizeFunction(delegate*<SystemContainer, World, void> value)
        {
            this.value = value;
        }
#endif

        /// <summary>
        /// Calls this function.
        /// </summary>
        public readonly void Invoke(SystemContainer container, World world)
        {
            value(container, world);
        }
    }
}