using Unmanaged;
using Worlds;

namespace Simulation.Components
{
    /// <summary>
    /// Stores the allocation of a program.
    /// </summary>
    [Component]
    public readonly struct ProgramAllocation
    {
        /// <summary>
        /// The allocation of the program.
        /// </summary>
        public readonly Allocation allocation;

        /// <summary>
        /// The world that was created for and belongs to the program.
        /// </summary>
        public readonly World world;

        public ProgramAllocation(Allocation allocation, World world)
        {
            this.allocation = allocation;
            this.world = world;
        }
    }
}