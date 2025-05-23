using Types;

namespace Simulation
{
    internal struct MessageReceiverLocator
    {
        public TypeMetadata type;
        public int index;

        public MessageReceiverLocator(TypeMetadata type, int index)
        {
            this.type = type;
            this.index = index;
        }
    }
}