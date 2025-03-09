using Simulation.Functions;
using System;
using Worlds;

namespace Simulation
{
    /// <summary>
    /// Describes a system type added to <see cref="Simulator"/> instances.
    /// <para>
    /// Initialized to a default state. Use the <c>Start</c> function to initialize.
    /// </para>
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
        public int GetMessageHandlers(Span<MessageHandler> buffer)
        {
            return 0;
        }

        /// <summary>
        /// Called to notify that the system has been initialized. First with the
        /// simulator <see cref="World"/>, and then with each program <see cref="World"/>.
        /// </summary>
        void Start(in SystemContainer systemContainer, in World world);

        /// <summary>
        /// Called when the simulator updates the simulation forward. 
        /// First with the simulator <see cref="World"/>, and then with each program <see cref="World"/>.
        /// </summary>
        void Update(in SystemContainer systemContainer, in World world, in TimeSpan delta);

        /// <summary>
        /// Called after the system has been removed from the <see cref="Simulator"/>,
        /// or when it was disposed. In the reverse order that the system started.
        /// </summary>
        void Finish(in SystemContainer systemContainer, in World world);
    }
}