namespace Simulation.Tests
{
    public readonly struct UpdateMessage
    {
        public readonly double deltaTime;

        public UpdateMessage(double deltaTime)
        {
            this.deltaTime = deltaTime;
        }
    }
}