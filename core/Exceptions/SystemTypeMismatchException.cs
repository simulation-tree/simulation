using System;

namespace Simulation.Exceptions
{
    /// <summary>
    /// Exception thrown when a system is access with a type, but not with the expected type.
    /// </summary>
    public class SystemTypeMismatchException : Exception
    {
        /// <inheritdoc/>
        public SystemTypeMismatchException(Type actualType, Type? expectedType)
            : base(GetMessage(actualType, expectedType))
        {
        }

        private static string GetMessage(Type actualType, Type? expectedType)
        {
            return $"The system `{actualType.Name}` is not of the expected type `{expectedType}`";
        }
    }
}