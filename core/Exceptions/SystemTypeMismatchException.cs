using System;
using Types;

namespace Simulation.Exceptions
{
    public class SystemTypeMismatchException : Exception
    {
        public SystemTypeMismatchException(Type actualType, TypeLayout expectedType)
            : base(GetMessage(actualType, expectedType))
        {
        }

        private static string GetMessage(Type actualType, TypeLayout expectedType)
        {
            return $"The system `{actualType.Name}` is not of the expected type `{expectedType.SystemType.Name}`";
        }
    }
}