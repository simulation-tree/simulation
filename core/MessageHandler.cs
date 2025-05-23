using System.Runtime.InteropServices;
using Types;
using Unmanaged;

namespace Simulation
{
    /// <summary>
    /// Container of a message receiving callback.
    /// </summary>
    public readonly struct MessageHandler
    {
        /// <summary>
        /// Type of message this handler can receive.
        /// </summary>
        public readonly TypeMetadata type;

        internal readonly GCHandle receiver;

        /// <summary>
        /// Initializes a new container instance.
        /// </summary>
        public MessageHandler(TypeMetadata type, Receive receiver)
        {
            this.type = type;
            this.receiver = GCHandle.Alloc(receiver, GCHandleType.Normal);
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