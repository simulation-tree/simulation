using System;
using System.Runtime.InteropServices;
using Types;
using Unmanaged;

namespace Simulation
{
    /// <summary>
    /// Container of a message receiving callback.
    /// </summary>
    public readonly struct MessageHandler : IDisposable
    {
        /// <summary>
        /// Type of message this handler can receive.
        /// </summary>
        public readonly TypeMetadata type;

        private readonly GCHandle receiver;

        /// <summary>
        /// The callback that receives messages of the specified type.
        /// </summary>
        public readonly Receive Callback => (Receive)receiver.Target!;

        /// <summary>
        /// Initializes a new container instance.
        /// </summary>
        public MessageHandler(TypeMetadata type, Receive receiver)
        {
            this.type = type;
            this.receiver = GCHandle.Alloc(receiver, GCHandleType.Normal);
        }

        /// <summary>
        /// Disposes the message handler, freeing the receiver callback.
        /// </summary>
        public readonly void Dispose()
        {
            receiver.Free();
        }

        /// <summary>
        /// Creates a new container instance.
        /// </summary>
        public static MessageHandler Get<L, M>(L listener) where L : IListener<M> where M : unmanaged
        {
            return new(TypeMetadata.GetOrRegister<M>(), Receive);

            void Receive(MemoryAddress message)
            {
                listener.Receive(ref message.Read<M>());
            }
        }
    }
}