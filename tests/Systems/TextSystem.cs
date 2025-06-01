using System;
using Unmanaged;

namespace Simulation.Tests
{
    public partial class TextSystem : SystemBase, IListener<AppendCharacter>, IListener<UpdateMessage>
    {
        private readonly Text text;

        public ReadOnlySpan<char> Text => text.AsSpan();

        public TextSystem(Simulator simulator) : base(simulator)
        {
            text = new();
        }

        public override void Dispose()
        {
            text.Dispose();
        }

        void IListener<AppendCharacter>.Receive(ref AppendCharacter message)
        {
            text.Append(message.character);
        }

        void IListener<UpdateMessage>.Receive(ref UpdateMessage message)
        {
            text.Append(message.deltaTime);
        }
    }
}