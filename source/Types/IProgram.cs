using Simulation.Functions;
using System;
using Unmanaged;
using Worlds;

namespace Simulation
{
    public interface IProgram : IDisposable
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
        /// </summary>
        void Initialize(in Simulator simulator, in Allocation allocation, in World world);

        /// <summary>
        /// Updates the program forward with the <see cref="World"/> that it was initialized with.
        /// </summary>
        StatusCode Update(in TimeSpan delta);
    }
}