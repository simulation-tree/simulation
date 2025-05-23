using Unmanaged.Tests;
using Worlds;

namespace Simulation.Tests
{
    public abstract class SimulationTests : UnmanagedTests
    {
        public Simulator simulator;
        public World world;

        protected override void SetUp()
        {
            base.SetUp();
            world = new(CreateSchema());
            simulator = new(world);
        }

        protected override void TearDown()
        {
            simulator.Dispose();
            simulator = default;
            world.Dispose();
            world = default;
            base.TearDown();
        }

        protected void Update()
        {
            simulator.Update();
        }

        protected void Update(double deltaTime)
        {
            simulator.Update(deltaTime);
        }

        protected void Broadcast<T>(T message) where T : unmanaged
        {
            simulator.Broadcast(message);
        }

        protected void Broadcast<T>(ref T message) where T : unmanaged
        {
            simulator.Broadcast(ref message);
        }

        protected virtual Schema CreateSchema()
        {
            return new();
        }
    }
}