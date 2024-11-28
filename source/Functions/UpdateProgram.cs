using System;
using Unmanaged;
using Worlds;

namespace Simulation.Functions
{
    /// <summary>
    /// A function that updates a program.
    /// </summary>
    public unsafe readonly struct UpdateProgram : IEquatable<UpdateProgram>
    {
#if NET
        private readonly delegate* unmanaged<Simulator, Allocation, World, TimeSpan, uint> function;

        /// <summary>
        /// Creates a new <see cref="UpdateProgram"/>.
        /// </summary>
        public UpdateProgram(delegate* unmanaged<Simulator, Allocation, World, TimeSpan, uint> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<Simulator, Allocation, World, TimeSpan, uint> function;

        public UpdateProgramFunction(delegate*<Simulator, Allocation, World, TimeSpan, uint> function)
        {
            this.function = function;
        }
#endif

        /// <summary>
        /// Invokes the function.
        /// </summary>
        public readonly uint Invoke(Simulator simulator, Allocation allocation, World world, TimeSpan delta)
        {
            return function(simulator, allocation, world, delta);
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is UpdateProgram function && Equals(function);
        }

        /// <inheritdoc/>
        public readonly bool Equals(UpdateProgram other)
        {
            nint a = (nint)function;
            nint b = (nint)other.function;
            return a == b;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return ((nint)function).GetHashCode();
        }

        /// <inheritdoc/>
        public static bool operator ==(UpdateProgram left, UpdateProgram right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(UpdateProgram left, UpdateProgram right)
        {
            return !(left == right);
        }
    }
}