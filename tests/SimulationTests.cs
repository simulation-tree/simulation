using Simulation.Components;
using Unmanaged;
using Unmanaged.Tests;
using Worlds;

namespace Simulation.Tests
{
    public abstract class SimulationTests : UnmanagedTests
    {
        protected override void SetUp()
        {
            base.SetUp();
            ComponentType.Register<float>();
            ComponentType.Register<int>();
            ComponentType.Register<double>();
            ComponentType.Register<char>();
            ComponentType.Register<World>();
            ComponentType.Register<IsProgram>();
            ComponentType.Register<ProgramState>();
            ComponentType.Register<byte>();
            ComponentType.Register<bool>();
            ComponentType.Register<uint>();
            ComponentType.Register<FixedString>();
            ComponentType.Register<short>();
            ComponentType.Register<ushort>();
            ComponentType.Register<ProgramAllocation>();
            ArrayType.Register<byte>();
            ArrayType.Register<float>();
            ArrayType.Register<double>();
            ArrayType.Register<char>();
            ArrayType.Register<uint>();
        }
    }
}
