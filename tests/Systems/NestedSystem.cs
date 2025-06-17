using System.Diagnostics;

namespace Simulation.Tests
{
    public partial class NestedSystem : IListener<AppendCharacter>
    {
        private readonly Simulator simulator;

        public NestedSystem(Simulator simulator)
        {
            this.simulator = simulator;
        }

        void IListener<AppendCharacter>.Receive(ref AppendCharacter message)
        {
            Trace.WriteLine($"received a {message.character} in a nested system");
            simulator.Broadcast(new UpdateMessage(5));
        }
    }
}