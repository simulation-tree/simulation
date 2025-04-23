using Simulation.Functions;
using System;

namespace Simulation
{
    /// <summary>
    /// Collector of message handlers.
    /// </summary>
    public readonly struct MessageHandlerCollector
    {
        private readonly RuntimeTypeHandle systemType;
        private readonly MessageHandlers messageHandlers;

        internal MessageHandlerCollector(RuntimeTypeHandle systemType, MessageHandlers messageHandlers)
        {
            this.systemType = systemType;
            this.messageHandlers = messageHandlers;
        }

        /// <summary>
        /// Adds the given <paramref name="function"/> to the list of message handlers for the given type <typeparamref name="T"/>.
        /// </summary>
        public readonly void Add<T>(HandleMessage function) where T : unmanaged
        {
            RuntimeTypeHandle messageType = RuntimeTypeTable.GetHandle<T>();
            messageHandlers.Add(systemType, messageType, function);
        }

#if NET
        /// <summary>
        /// Adds the given <paramref name="function"/> to the list of message handlers for the given type <typeparamref name="T"/>.
        /// </summary>
        public unsafe readonly void Add<T>(delegate* unmanaged<HandleMessage.Input, StatusCode> function) where T : unmanaged
        {
            RuntimeTypeHandle messageType = RuntimeTypeTable.GetHandle<T>();
            messageHandlers.Add(systemType, messageType, new(function));
        }
#else
        /// <summary>
        /// Adds the given <paramref name="function"/> to the list of message handlers for the given type <typeparamref name="T"/>.
        /// </summary>
        public unsafe readonly void Add<T>(delegate*<HandleMessage.Input, StatusCode> function) where T : unmanaged
        {
            RuntimeTypeHandle messageType = RuntimeTypeTable.GetHandle<T>();
            messageHandlers.Add(systemType, messageType, new(function));
        }
#endif
    }
}