using System.Diagnostics;

namespace Simulation.Tests
{
    public partial class EmptySystem : IListener<char>
    {
        void IListener<char>.Receive(ref char message)
        {
            Trace.WriteLine($"received a {message}");
        }
    }
}