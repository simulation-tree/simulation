using Collections.Generic;
using Simulation.Functions;
using System;
using Types;

namespace Simulation
{
    internal readonly struct MessageHandlers : IDisposable
    {
        private readonly Dictionary<TypeMetadata, Array<MessageHandler>> map;

        public MessageHandlers(int initialCapacity)
        {
            map = new(initialCapacity);
        }

        public readonly void Dispose()
        {
            foreach (Array<MessageHandler> handlers in map.Values)
            {
                handlers.Dispose();
            }

            map.Dispose();
        }

        public readonly void Add(TypeMetadata systemType, TypeMetadata messageType, HandleMessage function)
        {
            ref Array<MessageHandler> handlers = ref map.TryGetValue(messageType, out bool contains);
            if (!contains)
            {
                handlers = ref map.Add(messageType);
                handlers = new(0);
            }

            int length = handlers.Length;
            handlers.Length = length + 1;
            handlers[length] = new(systemType, function);
        }

        public readonly bool TryGetValue(TypeMetadata messageType, out Array<MessageHandler> handlers)
        {
            return map.TryGetValue(messageType, out handlers);
        }
    }
}