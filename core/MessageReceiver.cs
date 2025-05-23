using System;
using System.Runtime.InteropServices;

namespace Simulation
{
    public struct MessageReceiver : IDisposable
    {
        private GCHandle function;

        public readonly bool IsDisposed => !function.IsAllocated;

        public MessageReceiver(Action<Message> function)
        {
            this.function = GCHandle.Alloc(function, GCHandleType.Normal);
        }

        public void Dispose()
        {
            function.Free();
        }

        public readonly void Invoke(Message message)
        {
            Action<Message> function = (Action<Message>)this.function.Target!;
            function.Invoke(message);
        }

        public static MessageReceiver Get<L, M>(L listener) where L : IListener<M> where M : unmanaged
        {
            return new MessageReceiver(Handle);

            void Handle(Message message)
            {
                listener.Receive(ref message.data.Read<M>());
            }
        }
    }
}