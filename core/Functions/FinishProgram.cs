using System;
using System.Diagnostics;
using Unmanaged;
using Worlds;

namespace Simulation.Functions
{
    /// <summary>
    /// A function that finishes a program.
    /// </summary>
    public unsafe readonly struct FinishProgram : IEquatable<FinishProgram>
    {
#if NET
        private readonly delegate* unmanaged<Simulator, Allocation, World, StatusCode, void> function;

        /// <summary>
        /// Creates a new <see cref="FinishProgram"/>.
        /// </summary>
        public FinishProgram(delegate* unmanaged<Simulator, Allocation, World, StatusCode, void> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<Simulator, Allocation, World, StatusCode, void> function;

        public FinishProgram(delegate*<Simulator, Allocation, World, StatusCode, void> function)
        {
            this.function = function;
        }
#endif

        /// <summary>
        /// Invokes the function.
        /// </summary>
        public readonly void Invoke(Simulator simulator, Allocation allocation, World world, StatusCode statusCode)
        {
            ThrowIfDefault();

            function(simulator, allocation, world, statusCode);
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is FinishProgram function && Equals(function);
        }

        /// <inheritdoc/>
        public readonly bool Equals(FinishProgram other)
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
                throw new InvalidOperationException("Finish program function is not initialized");
            }
        }

        /// <inheritdoc/>
        public static bool operator ==(FinishProgram left, FinishProgram right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(FinishProgram left, FinishProgram right)
        {
            return !(left == right);
        }
    }
}