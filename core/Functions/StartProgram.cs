using System;
using Unmanaged;
using Worlds;

namespace Simulation.Functions
{
    /// <summary>
    /// A function that starts a program.
    /// </summary>
    public unsafe readonly struct StartProgram : IEquatable<StartProgram>
    {
#if NET
        private readonly delegate* unmanaged<Simulator, Allocation, World, void> function;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartProgram"/> struct.
        /// </summary>
        public StartProgram(delegate* unmanaged<Simulator, Allocation, World, void> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<Simulator, Allocation, World, void> function;

        public StartProgramFunction(delegate*<Simulator, Allocation, World, void> function)
        {
            this.function = function;
        }
#endif
        /// <summary>
        /// Invokes the function.
        /// </summary>
        public readonly void Invoke(Simulator simulator, Allocation allocation, World world)
        {
            function(simulator, allocation, world);
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is StartProgram function && Equals(function);
        }

        /// <inheritdoc/>
        public readonly bool Equals(StartProgram other)
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
        public static bool operator ==(StartProgram left, StartProgram right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(StartProgram left, StartProgram right)
        {
            return !(left == right);
        }
    }
}