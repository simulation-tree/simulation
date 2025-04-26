using Types;
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
            MetadataRegistry.Load<SimulationMetadataBank>();
            MetadataRegistry.Load<SimulationTestsMetadataBank>();
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

        protected void Update()
        {
            simulator.Update();
        }

        protected virtual Schema CreateSchema()
        {
            Schema schema = new();
            schema.Load<SimulationSchemaBank>();
            schema.Load<SimulationTestsSchemaBank>();
            return schema;
        }

        protected World CreateWorld()
        {
            World world = new(CreateSchema());
            return world;
        }
    }
}