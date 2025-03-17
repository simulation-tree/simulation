using Simulation.Functions;
using System;
using Worlds;

namespace Simulation
{
    /// <summary>
    /// Managed by a <see cref="Simulator"/> after all 
    /// <see cref="ISystem"/>s performed their behaviour.
    /// </summary>
    public interface IProgram
    {
        /// <summary>
        /// The underlying pointers for all control functions.
        /// </summary>
        public (StartProgram start, UpdateProgram update, FinishProgram finish) Functions
        {
            get
            {
                return default;
            }
        }
    }

    /// <summary>
    /// Managed by a <see cref="Simulator"/> after all 
    /// <see cref="ISystem"/>s performed their behaviour.
    /// </summary>
    public interface IProgram<T> : IProgram where T : unmanaged, IProgram
    {
        /// <summary>
        /// Starts a program with its own <paramref name="world"/>.
        /// </summary>
        void Start(ref T program, in Simulator simulator, in World world);

        /// <summary>
        /// Updates a program forward after all systems.
        /// </summary>
        StatusCode Update(in TimeSpan delta);

        /// <summary>
        /// Finishes the program after <see cref="Update"/> returns a non
        /// continue and default status code.
        /// </summary>
        void Finish(in StatusCode statusCode);
    }
}