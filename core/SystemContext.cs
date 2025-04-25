using Simulation.Exceptions;
using System;
using System.Diagnostics;
using Worlds;

namespace Simulation
{
    /// <summary>
    /// Represents a piece of the <see cref="Simulation.Simulator"/>.
    /// </summary>
    public readonly struct SystemContext : IEquatable<SystemContext>
    {
        private readonly SystemContainer systemContainer;

        /// <summary>
        /// The world that the <see cref="Simulation.Simulator"/> was created with.
        /// </summary>
        public readonly World SimulatorWorld => systemContainer.World;

        /// <summary>
        /// The simulator that the context originates from.
        /// </summary>
        public readonly Simulator Simulator => systemContainer.simulator;

        internal SystemContext(SystemContainer systemContainer)
        {
            this.systemContainer = systemContainer;
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            return systemContainer.ToString();
        }

        /// <summary>
        /// Checks if the given <paramref name="world"/> is the world that the
        /// simulator was created with.
        /// </summary>
        public readonly bool IsSimulatorWorld(World world)
        {
            return systemContainer.IsSimulatorWorld(world);
        }

        /// <summary>
        /// Adds the given <paramref name="system"/> to the simulator.
        /// </summary>
        public readonly SystemContainer<T> AddSystem<T>(T system) where T : unmanaged, ISystem
        {
            return systemContainer.simulator.AddSystem(system, systemContainer.index);
        }

        /// <summary>
        /// Removes the system of type <typeparamref name="T"/> from the simulator.
        /// </summary>
        public readonly void RemoveSystem<T>() where T : unmanaged, ISystem
        {
            systemContainer.simulator.RemoveSystem<T>();
        }

        /// <summary>
        /// Submits a <paramref name="message"/> for a potential system to handle.
        /// </summary>
        /// <returns><see langword="default"/> if no handler was found.</returns>
        public readonly StatusCode TryHandleMessage<T>(T message) where T : unmanaged
        {
            return systemContainer.simulator.TryHandleMessage(message);
        }

        /// <summary>
        /// Submits a <paramref name="message"/> for a potential system to handle.
        /// </summary>
        /// <returns><see langword="default"/> if no handler was found.</returns>
        public readonly StatusCode TryHandleMessage<T>(ref T message) where T : unmanaged
        {
            return systemContainer.simulator.TryHandleMessage(ref message);
        }

        /// <summary>
        /// Only updates the systems forward with the simulator world first, then program worlds.
        /// </summary>
        public readonly void UpdateSystems(TimeSpan delta)
        {
            systemContainer.simulator.UpdateSystems(delta);
        }

        /// <summary>
        /// Updates all systems only with the given <paramref name="world"/>.
        /// </summary>
        public readonly void UpdateSystems(TimeSpan delta, World world)
        {
            systemContainer.simulator.UpdateSystems(delta, world);
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is SystemContext context && Equals(context);
        }

        /// <inheritdoc/>
        public readonly bool Equals(SystemContext other)
        {
            return systemContainer == other.systemContainer;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return systemContainer.GetHashCode();
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfSystemTypeMismatch<T>() where T : unmanaged
        {
            if (!systemContainer.type.Is<T>())
            {
                throw new SystemTypeMismatchException(typeof(T), systemContainer.type);
            }
        }

        /// <summary>
        /// Overwrites the system value.
        /// </summary>
        public readonly void Write<T>(T newSystem) where T : unmanaged, ISystem
        {
            ThrowIfSystemTypeMismatch<T>();

            systemContainer.Write(newSystem);
        }

        /// <inheritdoc/>
        public static bool operator ==(SystemContext left, SystemContext right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(SystemContext left, SystemContext right)
        {
            return !(left == right);
        }
    }
}