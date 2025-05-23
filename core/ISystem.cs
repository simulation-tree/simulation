namespace Simulation
{
    /// <summary>
    /// Callback for <see cref="Simulator.Update()"/> function.
    /// </summary>
    public interface ISystem
    {
        /// <summary>
        /// Updates the system forward with <paramref name="deltaTime"/>.
        /// </summary>
        void Update(Simulator simulator, double deltaTime);
    }
}