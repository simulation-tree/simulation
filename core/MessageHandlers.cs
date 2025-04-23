using Collections.Generic;
using Simulation.Functions;
using System;

namespace Simulation
{
    internal readonly struct MessageHandlers : IDisposable
    {
        private readonly Dictionary<nint, Array<MessageHandler>> map;

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

        public readonly void Add(RuntimeTypeHandle systemType, RuntimeTypeHandle messageType, HandleMessage function)
        {
            ref Array<MessageHandler> handlers = ref map.TryGetValue(RuntimeTypeTable.GetAddress(messageType), out bool contains);
            if (!contains)
            {
                handlers = ref map.Add(RuntimeTypeTable.GetAddress(messageType));
                handlers = new(0);
            }

            int length = handlers.Length;
            handlers.Length = length + 1;
            handlers[length] = new(systemType, function);
        }

        public readonly bool TryGetValue(RuntimeTypeHandle messageType, out Array<MessageHandler> handlers)
        {
            return map.TryGetValue(RuntimeTypeTable.GetAddress(messageType), out handlers);
        }
    }
}