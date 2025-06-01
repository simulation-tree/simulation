using System;

namespace Simulation
{
    /// <summary>
    /// Base type for system classes.
    /// </summary>
    public abstract class SystemBase : IDisposable
    {
        /// <summary>
        /// The simulator this system belongs to.
        /// </summary>
        public readonly Simulator simulator;

        /// <summary>
        /// Creates the system.
        /// </summary>
        protected SystemBase(Simulator simulator)
        {
            this.simulator = simulator;
        }

        /// <summary>
        /// Disposes the system.
        /// </summary>
        public abstract void Dispose();
    }
}