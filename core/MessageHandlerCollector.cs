using Collections.Generic;
using Simulation.Functions;
using Types;

namespace Simulation
{
    /// <summary>
    /// Collector of message handlers.
    /// </summary>
    public readonly struct MessageHandlerCollector
    {
        private readonly Type systemType;
        private readonly HashSet<MessageHandlerGroupKey> messageHandlerGroups;

        internal MessageHandlerCollector(Type systemType, HashSet<MessageHandlerGroupKey> messageHandlerGroups)
        {
            this.systemType = systemType;
            this.messageHandlerGroups = messageHandlerGroups;
        }

        /// <summary>
        /// Adds the given <paramref name="function"/> to the list of message handlers for the given type <typeparamref name="T"/>.
        /// </summary>
        public readonly void Add<T>(HandleMessage function) where T : unmanaged
        {
            Type messageType = TypeRegistry.GetOrRegisterType<T>();
            MessageHandlerGroupKey key = new(messageType);
            if (!messageHandlerGroups.TryGetValue(key, out MessageHandlerGroupKey existing))
            {
                existing = new(messageType, new(0));
                messageHandlerGroups.Add(existing);
            }

            existing.Add(systemType, function);
        }

#if NET
        /// <summary>
        /// Adds the given <paramref name="function"/> to the list of message handlers for the given type <typeparamref name="T"/>.
        /// </summary>
        public unsafe readonly void Add<T>(delegate* unmanaged<HandleMessage.Input, StatusCode> function) where T : unmanaged
        {
            Type messageType = TypeRegistry.GetOrRegisterType<T>();
            MessageHandlerGroupKey key = new(messageType);
            if (!messageHandlerGroups.TryGetValue(key, out MessageHandlerGroupKey existing))
            {
                existing = new(messageType, new(0));
                messageHandlerGroups.Add(existing);
            }

            existing.Add(systemType, new(function));
        }
#else
        /// <summary>
        /// Adds the given <paramref name="function"/> to the list of message handlers for the given type <typeparamref name="T"/>.
        /// </summary>
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