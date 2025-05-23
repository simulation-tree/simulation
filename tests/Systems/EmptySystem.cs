using System.Diagnostics;

namespace Simulation.Tests
{
    public partial class EmptySystem : ISystem, IListener<char>
    {
        void IListener<char>.Receive(ref char message)
        {
            Trace.WriteLine($"received a {message}");
        }

        void ISystem.Update(Simulator simulator, double deltaTime)
        {
        }
    }
}