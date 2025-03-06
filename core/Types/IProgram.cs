using Simulation.Functions;
using System;
using Unmanaged;
using Worlds;

namespace Simulation
{
    /// <summary>
    /// Managed by a <see cref="Simulator"/> after all <see cref="ISystem"/> performed
    /// their behaviour.
    /// </summary>
    public interface IProgram
    {
        public (StartProgram start, UpdateProgram update, FinishProgram finish) Functions
        {
            get
            {
                return default;
            }
        }

        /// <summary>
        /// Initializes the program with its own program <see cref="World"/> instance.
        /// After all systems have been started.
        /// </summary>
        void Start(in Simulator simulator, in MemoryAddress allocation, in World world);

        /// <summary>
        /// Updates the program forward with the <see cref="World"/> that it was initialized with.
        /// After systems have updated.
        /// </summary>
        StatusCode Update(in TimeSpan delta);

        /// <summary>
        /// Finishes the program with the <see cref="World"/> that it was initialized with.
        /// <para>
        /// The <paramref name="statusCode"/> will be <see cref="StatusCode.Termination"/> if the 
        /// program exited without its control.
        /// </para>
        /// </summary>
        void Finish(in StatusCode statusCode);
    }
}