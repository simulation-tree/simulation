using Simulation.Functions;

namespace Simulation
{
    /// <summary>
    /// Describes a program.
    /// </summary>
    public interface IProgram
    {
        /// <summary>
        /// Called when this program has started.
        /// </summary>
        StartProgram Start { get; }

        /// <summary>
        /// Called when the simulator iterates over this program.
        /// </summary>
        UpdateProgram Update { get; }

        /// <summary>
        /// Called when this program is finished, and just before it's disposed.
        /// </summary>
        FinishProgram Finish { get; }
    }
}