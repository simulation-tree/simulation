using System;
using System.Diagnostics;
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
        private readonly delegate* unmanaged<Simulator, MemoryAddress, World, void> function;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartProgram"/> struct.
        /// </summary>
        public StartProgram(delegate* unmanaged<Simulator, MemoryAddress, World, void> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<Simulator, Allocation, World, void> function;

        public StartProgram(delegate*<Simulator, Allocation, World, void> function)
        {
            this.function = function;
        }
#endif
        /// <summary>
        /// Invokes the function.
        /// </summary>
        public readonly void Invoke(Simulator simulator, MemoryAddress allocation, World world)
        {
            ThrowIfDefault();

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

        [Conditional("DEBUG")]
        private readonly void ThrowIfDefault()
        {
            if (function == default)
            {
                throw new InvalidOperationException("Start program function is not initialized");
            }
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