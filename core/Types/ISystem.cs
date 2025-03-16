using Simulation.Functions;
using System;
using Worlds;

namespace Simulation
{
    /// <summary>
    /// Describes a system type added to <see cref="Simulator"/> instances.
    /// <para>
    /// Always initialized with <see langword="default"/> state.
    /// Use the <see cref="Start(in SystemContainer, in World)"/>
    /// function to initialize with a custom state.
    /// </para>
    /// </summary>
    public interface ISystem : IDisposable
    {
        /// <summary>
        /// Exposes function pointers for a <see cref="Simulator"/> to use.
        /// </summary>
        public (StartSystem start, UpdateSystem update, FinishSystem finish, DisposeSystem dispose) Functions
        {
            get
            {
                return default;
            }
        }

        /// <summary>
        /// Collects all message handling functions.
        /// </summary>
        public void CollectMessageHandlers(MessageHandlerCollector collector)
        {
        }

        /// <summary>
        /// Called to notify that the system has been started.
        /// <para>
        /// The <paramref name="world"/> will be the <paramref name="collector"/>'s world first,
        /// and then with each program in the order they were added.
        /// </para>
        /// </summary>
        void Start(in SystemContext context, in World world);

        /// <summary>
        /// Called when the simulator updates the simulation forward.
        /// <para>
        /// The <paramref name="world"/> will be the <paramref name="collector"/>'s world first,
        /// and then with each program in the order they were added.
        /// </para>
        /// </summary>
        void Update(in SystemContext context, in World world, in TimeSpan delta);

        /// <summary>
        /// Called after the system has been removed from the simulation, or when
        /// the <see cref="Simulator"/> has been disposed.
        /// <para>
        /// The <paramref name="world"/> will be with each program in the reverse added
        /// order, and then with the <paramref name="collector"/>'s world last.
        /// </para>
        /// </summary>
        void Finish(in SystemContext context, in World world);
    }
}