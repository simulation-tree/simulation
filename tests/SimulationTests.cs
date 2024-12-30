using Simulation.Components;
using System;
using System.Threading;
using System.Threading.Tasks;
using Unmanaged;
using Unmanaged.Tests;
using Worlds;

namespace Simulation.Tests
{
    public abstract class SimulationTests : UnmanagedTests
    {
        protected World world;
        protected Simulator simulator;

        static SimulationTests()
        {
            TypeLayout.Register<IsProgram>();
            TypeLayout.Register<bool>();
            TypeLayout.Register<FixedString>();
            TypeLayout.Register<uint>();
            TypeLayout.Register<float>();
            TypeLayout.Register<ulong>();
            TypeLayout.Register<byte>();
            TypeLayout.Register<int>();
        }

        protected override void SetUp()
        {
            base.SetUp();
            world = CreateWorld();
            simulator = new(world);
        }

        protected override void TearDown()
        {
            simulator.Dispose();
            world.Dispose();
            base.TearDown();
        }

        protected async Task Simulate(World world, CancellationToken cancellation)
        {
            TimeSpan delta = simulator.Update();
            await Task.Delay(delta, cancellation).ConfigureAwait(false);
        }

        protected static World CreateWorld()
        {
            World world = new();
            world.Schema.RegisterComponent<IsProgram>();
            world.Schema.RegisterComponent<bool>();
            world.Schema.RegisterComponent<FixedString>();
            world.Schema.RegisterComponent<uint>();
            world.Schema.RegisterComponent<float>();
            world.Schema.RegisterComponent<ulong>();
            world.Schema.RegisterComponent<byte>();
            world.Schema.RegisterComponent<int>();
            return world;
        }
    }
}
