using Simulation.Functions;
using System;

namespace Simulation
{
    internal readonly struct MessageHandler : IEquatable<MessageHandler>
    {
        public readonly nint systemType;
        public readonly HandleMessage function;

        public MessageHandler(RuntimeTypeHandle systemType, HandleMessage function)
        {
            this.systemType = RuntimeTypeTable.GetAddress(systemType);
            this.function = function;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is MessageHandler handler && Equals(handler);
        }

        public readonly bool Equals(MessageHandler other)
        {
            return systemType == other.systemType && function == other.function;
        }

        public readonly override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + systemType.GetHashCode();
                hash = hash * 23 + function.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(MessageHandler left, MessageHandler right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MessageHandler left, MessageHandler right)
        {
            return !(left == right);
        }
    }
}