using Unmanaged.Tests;

namespace Simulation.Tests
{
    public abstract class SimulationTests : UnmanagedTests
    {
        private Simulator? simulator;

        public Simulator Simulator => simulator ?? throw new("Simulator is not initialized");

        protected override void SetUp()
        {
            base.SetUp();
            simulator = new();
        }

        protected override void TearDown()
        {
            simulator?.Dispose();
            simulator = null;
            base.TearDown();
        }

        protected void Update()
        {
            Update(0);
        }

        protected virtual void Update(double deltaTime)
        {
        }
    }
}