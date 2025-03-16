using Collections.Generic;
using Simulation.Functions;
using Types;

namespace Simulation
{
    public readonly struct MessageHandlerCollector
    {
        private readonly TypeLayout systemType;
        private readonly HashSet<MessageHandlerGroupKey> messageHandlerGroups;

        internal MessageHandlerCollector(TypeLayout systemType, HashSet<MessageHandlerGroupKey> messageHandlerGroups)
        {
            this.systemType = systemType;
            this.messageHandlerGroups = messageHandlerGroups;
        }

        public readonly void Add<T>(HandleMessage function) where T : unmanaged
        {
            TypeLayout messageType = TypeRegistry.GetOrRegister<T>();
            MessageHandlerGroupKey key = new(messageType);
            if (!messageHandlerGroups.TryGetValue(key, out MessageHandlerGroupKey existing))
            {
                existing = new(messageType, new(0));
                messageHandlerGroups.Add(existing);
            }

            existing.Add(systemType, function);
        }

#if NET
        public unsafe readonly void Add<T>(delegate* unmanaged<HandleMessage.Input, StatusCode> function) where T : unmanaged
        {
            TypeLayout messageType = TypeRegistry.GetOrRegister<T>();
            MessageHandlerGroupKey key = new(messageType);
            if (!messageHandlerGroups.TryGetValue(key, out MessageHandlerGroupKey existing))
            {
                existing = new(messageType, new(0));
                messageHandlerGroups.Add(existing);
            }

            existing.Add(systemType, new(function));
        }
#else
        public unsafe readonly void Add<T>(delegate*<HandleMessage.Input, StatusCode> function) where T : unmanaged
        {
            TypeLayout messageType = TypeRegistry.GetOrRegister<T>();
            MessageHandlerGroupKey key = new(messageType);
            if (!messageHandlerGroups.TryGetValue(key, out MessageHandlerGroupKey existing))
            {
                existing = new(messageType, new(0));
                messageHandlerGroups.Add(existing);
            }

            existing.Add(new(function));
        }
#endif
    }
}