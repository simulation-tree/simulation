using Unmanaged;

namespace Simulation
{
    /// <summary>
    /// Callback for receiving a <paramref name="message"/>.
    /// </summary>

    public delegate void Receive(MemoryAddress message);
}