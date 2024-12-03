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

        void Start(in SystemContainer systemContainer, in World world);
        void Update(in SystemContainer systemContainer, in World world, in TimeSpan delta);
        void Finish(in SystemContainer systemContainer, in World world);
    }
}