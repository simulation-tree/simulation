using Simulation.Components;
using Unmanaged.Tests;
using Worlds;

namespace Simulation.Tests
{
    public abstract class SimulationTests : UnmanagedTests
    {
        protected override void SetUp()
        {
            base.SetUp();
            ComponentType.Register<IsProgram>();
            ComponentType.Register<ProgramAllocation>();
            ComponentType.Register<ReturnCode>();
        }
    }
}
