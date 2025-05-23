using Types;

namespace Simulation
{
    public readonly struct MessageHandler
    {
        public readonly TypeMetadata type;
        public readonly MessageReceiver receiver;

        public MessageHandler(TypeMetadata type, MessageReceiver receiver)
        {
            this.type = type;
            this.receiver = receiver;
        }

        public static MessageHandler Get<L, M>(L listener) where L : IListener<M> where M : unmanaged
        {
            return new(TypeMetadata.GetOrRegister<M>(), MessageReceiver.Get<L, M>(listener));
        }
    }
}