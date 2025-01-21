using Simulation.Components;
using Simulation.Functions;
using System;
using Unmanaged;
using Worlds;

namespace Simulation
{
    /// <summary>
    /// Container for a program running in a <see cref="World"/>,
    /// operated by a <see cref="Simulator"/>.
    /// </summary>
    public struct ProgramContainer : IDisposable
    {
        /// <summary>
        /// The function to start the program.
        /// </summary>
        public readonly StartProgram start;

        /// <summary>
        /// The function to finish the program.
        /// </summary>
        public readonly FinishProgram finish;

        /// <summary>
        /// The function to update the program.
        /// </summary>
        public readonly UpdateProgram update;

        /// <summary>
        /// The <see cref="World"/> that belongs to this program.
        /// </summary>
        public readonly World world;

        /// <summary>
        /// The entity in the <see cref="Simulator"/> world that initialized this program.
        /// </summary>
        public readonly uint entity;

        /// <summary>
        /// Native memory containing the program's data.
        /// </summary>
        public readonly Allocation allocation;

        public bool didFinish;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramContainer"/> struct.
        /// </summary>
        public ProgramContainer(uint entity, IsProgram component, World world, Allocation allocation)
        {
            this.entity = entity;
            start = component.start;
            finish = component.finish;
            update = component.update;
            this.world = world;
            this.allocation = allocation;
        }

        public readonly void Dispose()
        {
            world.Dispose();
            allocation.Dispose();
        }
    }
}