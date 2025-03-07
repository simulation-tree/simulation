using Simulation.Functions;
using System;
using Types;
using Unmanaged;

namespace Simulation
{
    /// <summary>
    /// Container of message handler information.
    /// </summary>
    public readonly struct MessageHandler : IEquatable<MessageHandler>
    {
        /// <summary>
        /// The type of message to handle.
        /// </summary>
        public readonly TypeLayout messageType;

        /// <summary>
        /// The function for handling.
        /// </summary>
        public readonly HandleMessage function;

        /// <summary>
        /// The <see cref="Type"/> of message to handle.
        /// </summary>
        public readonly Type MessageType => messageType.SystemType;

        /// <summary>
        /// Creates a new instance of the <see cref="MessageHandler"/> struct.
        /// </summary>
        public MessageHandler(TypeLayout messageType, HandleMessage function)
        {
            this.messageType = messageType;
            this.function = function;
        }

        /// <summary>
        /// Builds a string representation of the message handler.
        /// </summary>
        public readonly uint ToString(USpan<char> destination)
        {
            string name = MessageType.Name;
            for (uint i = 0; i < name.Length; i++)
            {
                destination[i] = name[(int)i];
            }

            return (uint)name.Length;
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[256];
            uint length = ToString(buffer);
            return buffer.GetSpan(length).ToString();
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is MessageHandler handler && Equals(handler);
        }

        /// <inheritdoc/>
        public readonly bool Equals(MessageHandler other)
        {
            return messageType.Equals(other.messageType) && function.Equals(other.function);
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return HashCode.Combine(messageType, function);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="MessageHandler"/> struct.
        /// </summary>
        public static MessageHandler Create<T>(HandleMessage function) where T : unmanaged
        {
            return new(TypeRegistry.GetOrRegister<T>(), function);
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