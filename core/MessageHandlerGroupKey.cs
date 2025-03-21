using Collections.Generic;
using Simulation.Functions;
using System;
using Types;

namespace Simulation
{
    internal readonly struct MessageHandlerGroupKey : IDisposable, IEquatable<MessageHandlerGroupKey>
    {
        public readonly Types.Type messageType;
        public readonly Array<MessageHandler> handlers;

        public MessageHandlerGroupKey(Types.Type messageType)
        {
            this.messageType = messageType;
            this.handlers = default;
        }

        public MessageHandlerGroupKey(Types.Type messageType, Array<MessageHandler> handlers)
        {
            this.messageType = messageType;
            this.handlers = handlers;
        }

        public readonly void Dispose()
        {
            handlers.Dispose();
        }

        public readonly void Add(Types.Type systemType, HandleMessage function)
        {
            int length = handlers.Length;
            handlers.Length = length + 1;
            handlers[length] = new(systemType, function);
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is MessageHandlerGroupKey key && Equals(key);
        }

        public readonly bool Equals(MessageHandlerGroupKey other)
        {
            return messageType.Equals(other.messageType);
        }

        public readonly override int GetHashCode()
        {
            return messageType.GetHashCode();
        }

        public static bool operator ==(MessageHandlerGroupKey left, MessageHandlerGroupKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MessageHandlerGroupKey left, MessageHandlerGroupKey right)
        {
            return !(left == right);
        }
    }
}