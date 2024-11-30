using Simulation.Functions;
using Unmanaged;
using Worlds;

namespace Simulation
{
    /// <summary>
    /// Describes a system type added to <see cref="Simulator"/> instances.
    /// </summary>
    public interface ISystem
    {
        /// <summary>
        /// Initializes the system for <see cref="World"/>s of each program and
        /// the <see cref="Simulator"/>.
        /// </summary>
        StartSystem Start { get; }

        /// <summary>
        /// Updates the system for <see cref="World"/>s of each program and
        /// the <see cref="Simulator"/>.
        /// </summary>
        UpdateSimulator Update { get; }

        /// <summary>
        /// Finalizes the system for <see cref="World"/>s of each program and
        /// the <see cref="Simulator"/>.
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