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
    public struct ProgramContainer : IDisposable, IEquatable<ProgramContainer>
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

        public IsProgram.State state;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgramContainer"/> struct.
        /// </summary>
        public ProgramContainer(uint entity, IsProgram.State state, IsProgram component, World world, Allocation allocation)
        {
            this.entity = entity;
            this.state = state;
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

        public readonly override bool Equals(object? obj)
        {
            return obj is ProgramContainer container && Equals(container);
        }

        public readonly bool Equals(ProgramContainer other)
        {
            return world == other.world;
        }

        public readonly override int GetHashCode()
        {
            return world.GetHashCode();
        }

        public static bool operator ==(ProgramContainer left, ProgramContainer right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ProgramContainer left, ProgramContainer right)
        {
            return !(left == right);
        }
    }
}