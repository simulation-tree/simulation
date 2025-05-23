using Unmanaged.Tests;
using Worlds;

namespace Simulation.Tests
{
    public abstract class SimulationTests : UnmanagedTests
    {
        private Simulator? simulator;
        public World world;

        public Simulator Simulator => simulator ?? throw new("Simulator is not initialized");

        protected override void SetUp()
        {
            base.SetUp();
            world = new(CreateSchema());
            simulator = new(world);
        }

        protected override void TearDown()
        {
            simulator?.Dispose();
            simulator = default;
            world.Dispose();
            world = default;
            base.TearDown();
        }

        protected virtual Schema CreateSchema()
        {
            return new();
        }
    }
}