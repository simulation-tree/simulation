using Simulation.Functions;
using Unmanaged;

namespace Simulation
{
    /// <summary>
    /// Describes a system type added to <see cref="Simulator"/> instances.
    /// </summary>
    public interface ISystem
    {
        /// <summary>
        /// The function to initialize the system.
        /// </summary>
        StartSystem Start { get; }

        /// <summary>
        /// The function to update the system.
        /// </summary>
        UpdateSimulator Update { get; }

        /// <summary>
        /// The function to finalize the system.
        /// </summary>
        FinishSystem Finish { get; }

        /// <summary>
        /// Retrieves the message handlers for this system.
        /// </summary>
        public uint GetMessageHandlers(USpan<MessageHandler> buffer)
        {
            return 0;
        }
    }
}