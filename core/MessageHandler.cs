using Simulation.Functions;
using System;
using Types;

namespace Simulation
{
    /// <summary>
    /// Container of message handler information.
    /// </summary>
    public readonly struct MessageHandler : IEquatable<MessageHandler>
    {
        /// <summary>
        /// The system that registered this handler.
        /// </summary>
        public readonly Types.Type systemType;

        /// <summary>
        /// The function for handling.
        /// </summary>
        public readonly HandleMessage function;

        /// <summary>
        /// The <see cref="System.Type"/> of message to handle.
        /// </summary>
        public readonly System.Type SystemType => systemType.SystemType;

        /// <summary>
        /// Creates a new instance of the <see cref="MessageHandler"/> struct.
        /// </summary>
        public MessageHandler(Types.Type systemType, HandleMessage function)
        {
            this.systemType = systemType;
            this.function = function;
        }

        /// <summary>
        /// Builds a string representation of the message handler.
        /// </summary>
        public readonly int ToString(Span<char> destination)
        {
            string name = SystemType.Name;
            name.AsSpan().CopyTo(destination);
            return name.Length;
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            return SystemType.ToString();
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is MessageHandler handler && Equals(handler);
        }

        /// <inheritdoc/>
        public readonly bool Equals(MessageHandler other)
        {
            return systemType.Equals(other.systemType) && function.Equals(other.function);
        }

        /// <inheritdoc/>
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

        /// <summary>
        /// Creates a new instance of the <see cref="MessageHandler"/> struct.
        /// </summary>
        public static MessageHandler Create<T>(HandleMessage function) where T : unmanaged
        {
            return new(TypeRegistry.GetOrRegisterType<T>(), function);
        }

        /// <inheritdoc/>
        public static bool operator ==(MessageHandler left, MessageHandler right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(MessageHandler left, MessageHandler right)
        {
            return !(left == right);
        }
    }
}