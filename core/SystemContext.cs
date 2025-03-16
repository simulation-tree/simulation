using Simulation.Exceptions;
using System;
using System.Diagnostics;
using Worlds;

namespace Simulation
{
    public readonly struct SystemContext : IEquatable<SystemContext>
    {
        private readonly SystemContainer systemContainer;

        public readonly World World => systemContainer.World;

        internal SystemContext(SystemContainer systemContainer)
        {
            this.systemContainer = systemContainer;
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

        public readonly override bool Equals(object? obj)
        {
            return obj is SystemContext context && Equals(context);
        }

        public readonly bool Equals(SystemContext other)
        {
            return systemContainer == other.systemContainer;
        }

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
        /// Asks the simulator to handle the given <paramref name="message"/>.
        /// </summary>
        /// <returns><see langword="default"/> if no system handled it.</returns>
        public readonly StatusCode TryHandleMessage<T>(ref T message) where T : unmanaged
        {
            return systemContainer.simulator.TryHandleMessage(ref message);
        }

        /// <summary>
        /// Overwrites the system value.
        /// </summary>
        public readonly void Write<T>(T newSystem) where T : unmanaged, ISystem
        {
            ThrowIfSystemTypeMismatch<T>();

            systemContainer.Write(newSystem);
        }

        public static bool operator ==(SystemContext left, SystemContext right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SystemContext left, SystemContext right)
        {
            return !(left == right);
        }
    }
}