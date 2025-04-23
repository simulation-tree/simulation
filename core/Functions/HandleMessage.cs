using Simulation.Exceptions;
using System;
using System.Diagnostics;
using Types;
using Unmanaged;
using Worlds;

namespace Simulation.Functions
{
    /// <summary>
    /// A function that handles a sent message.
    /// </summary>
    public unsafe readonly struct HandleMessage : IEquatable<HandleMessage>
    {
#if NET
        private readonly delegate* unmanaged<Input, StatusCode> value;

        /// <summary>
        /// Creates a new <see cref="HandleMessage"/> instance.
        /// </summary>
        public HandleMessage(delegate* unmanaged<Input, StatusCode> value)
        {
            this.value = value;
        }
#else
        private readonly delegate*<Input, StatusCode> value;

        public HandleMessage(delegate*<Input, StatusCode> value)
        {
            this.value = value;
        }
#endif
        /// <summary>
        /// Invokes the function.
        /// </summary>
        public readonly StatusCode Invoke(SystemContainer container, World world, MemoryAddress message, TypeMetadata messageType)
        {
            return value(new(container, world, message, messageType));
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is HandleMessage function && Equals(function);
        }

        /// <inheritdoc/>
        public readonly bool Equals(HandleMessage other)
        {
            return ((nint)value) == ((nint)other.value);
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return ((nint)value).GetHashCode();
        }

        /// <inheritdoc/>
        public static bool operator ==(HandleMessage left, HandleMessage right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(HandleMessage left, HandleMessage right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Input for the <see cref="HandleMessage"/> function.
        /// </summary>
        public readonly struct Input
        {
            /// <summary>
            /// The system that is handling the message, and registered
            /// the handler.
            /// </summary>
            public readonly SystemContainer system;

            /// <summary>
            /// The world the message is being handled with.
            /// </summary>
            public readonly World world;

            private readonly MemoryAddress data;
            private readonly TypeMetadata messageType;

            /// <summary>
            /// The simulator where the message is being handled from.
            /// </summary>
            public readonly Simulator Simulator => system.simulator;

            /// <inheritdoc/>
            public Input(SystemContainer system, World world, MemoryAddress data, TypeMetadata messageType)
            {
                this.system = system;
                this.world = world;
                this.data = data;
                this.messageType = messageType;
            }

            [Conditional("DEBUG")]
            private readonly void ThrowIfMessageTypeMismatch<T>() where T : unmanaged
            {
                if (!messageType.Is<T>())
                {
                    throw new InvalidOperationException($"The message type {typeof(T)} does not match the expected type {messageType}");
                }
            }

            [Conditional("DEBUG")]
            private readonly void ThrowIfSystemTypeMismatch<T>() where T : unmanaged
            {
                if (!system.type.Is<T>())
                {
                    throw new SystemTypeMismatchException(typeof(T), system.type);
                }
            }

            /// <summary>
            /// Reads the message being handled.
            /// </summary>
            public readonly ref T ReadMessage<T>() where T : unmanaged
            {
                ThrowIfMessageTypeMismatch<T>();

                return ref data.Read<T>();
            }

            /// <summary>
            /// Reads the system instance that is handling the message.
            /// </summary>
            public readonly ref T ReadSystem<T>() where T : unmanaged, ISystem
            {
                ThrowIfSystemTypeMismatch<T>();

                return ref system.Read<T>();
            }
        }
    }
}