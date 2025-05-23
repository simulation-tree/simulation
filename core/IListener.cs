using System;

namespace Simulation
{
    /// <summary>
    /// Top level type for all listeners.
    /// </summary>
    public interface IListener
    {
        /// <summary>
        /// Amount of listener interfaces.
        /// </summary>
        public int Count => 0;

        /// <summary>
        /// Collects all listener callbacks.
        /// </summary>
        public void CollectMessageHandlers(Span<MessageHandler> receivers)
        {
        }
    }

    /// <summary>
    /// Indicates that the instance can receive messages of type <typeparamref name="T"/>
    /// when added to a <see cref="Simulator"/>.
    /// </summary>
    public interface IListener<T> : IListener where T : unmanaged
    {
        /// <summary>
        /// Handler callback.
        /// </summary>
        void Receive(ref T message);
    }
}