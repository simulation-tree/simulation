using System.Runtime.InteropServices;
using Types;

namespace Simulation
{
    public readonly struct MessageHandler
    {
        public readonly TypeMetadata type;
        internal readonly GCHandle receiver;

        public MessageHandler(TypeMetadata type, Receive receiver)
        {
            this.type = type;
            this.receiver = GCHandle.Alloc(receiver, GCHandleType.Normal);
        }

        public static MessageHandler Get<L, M>(L listener) where L : IListener<M> where M : unmanaged
        {
            return new(TypeMetadata.GetOrRegister<M>(), Receive);

            void Receive(ref Message message)
            {
                listener.Receive(ref message.data.Read<M>());
            }
        }
    }
}