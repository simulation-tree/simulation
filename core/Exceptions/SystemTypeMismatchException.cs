using System;

namespace Simulation.Exceptions
{
    /// <summary>
    /// Exception thrown when a system is access with a type, but not with the expected type.
    /// </summary>
    public class SystemTypeMismatchException : Exception
    {
        /// <inheritdoc/>
        public SystemTypeMismatchException(System.Type actualType, Types.Type expectedType)
            : base(GetMessage(actualType, expectedType))
        {
        }

        private static string GetMessage(System.Type actualType, Types.Type expectedType)
        {
            return $"The system `{actualType.Name}` is not of the expected type `{expectedType.SystemType.Name}`";
        }
    }
}