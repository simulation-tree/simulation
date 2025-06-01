using System;

namespace Simulation.Exceptions
{
    /// <summary>
    /// Exception thrown when a system type is missing from the <see cref="Simulator"/>.
    /// </summary>
    public class MissingSystemTypeException : Exception
    {
        /// <summary>
        /// Creates an instance.
        /// </summary>
        public MissingSystemTypeException(Type systemType) : base(GetMessage(systemType))
        {
        }

        private static string GetMessage(Type systemType)
        {
            return $"The system type `{systemType.FullName}` is missing from the simulator";
        }
    }
}