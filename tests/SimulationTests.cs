using Simulation.Components;
using Unmanaged.Tests;
using Worlds;

namespace Simulation.Tests
{
    public abstract class SimulationTests : UnmanagedTests
    {
        private World world;
        private Simulator simulator;

        public World World => world;
        public Simulator Simulator => simulator;

        protected override void SetUp()
        {
            base.SetUp();
            ComponentType.Register<IsProgram>();
            ComponentType.Register<ProgramAllocation>();
            ComponentType.Register<ReturnCode>();
            world = new();
            simulator = new(world);
        }

        protected override void CleanUp()
        {
            simulator.Dispose();
            world.Dispose();
            base.CleanUp();
        }
    }
}
