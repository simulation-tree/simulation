using Simulation.Functions;
using System;
using Unmanaged;
using Worlds;

namespace Simulation
{
    /// <summary>
    /// Describes a system type added to <see cref="Simulator"/> instances.
    /// </summary>
    public interface ISystem
    {
        public (StartSystem start, UpdateSystem update, FinishSystem finish) Functions
        {
            get
            {
                return default;
            }
        }

        /// <summary>
        /// Retrieves the message handlers for this system.
        /// </summary>
        public uint GetMessageHandlers(USpan<MessageHandler> buffer)
        {
            return 0;
        }

        /// <summary>
        /// Called when this system has been initialized for every
        /// program and simulator <see cref="World"/>.
        /// </summary>
        void Start(in SystemContainer systemContainer, in World world);

        /// <summary>
        /// Called for every program and simulator <see cref="World"/>.
        /// </summary>
        void Update(in SystemContainer systemContainer, in World world, in TimeSpan delta);

        /// <summary>
        /// Called after the system has been removed from the <see cref="Simulator"/>
        /// for every program and simulator <see cref="World"/>.
        /// </summary>
        void Finish(in SystemContainer systemContainer, in World world);
    }
}