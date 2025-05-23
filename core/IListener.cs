using System;

namespace Simulation
{
    public interface IListener
    {
        public int Count => 0;

        public void CollectMessageHandlers(Span<MessageHandler> receivers)
        {
        }
    }

    public interface IListener<T> : IListener where T : unmanaged
    {
        void Receive(ref T message);
    }
}