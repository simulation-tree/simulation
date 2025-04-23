using Unmanaged;
using Worlds;

namespace Simulation.Tests
{
    public class SystemTests : SimulationTests
    {
        [Test]
        public void SimpleTest()
        {
            using (World simulatorWorld = CreateWorld())
            {
                using (Simulator simulator = new(simulatorWorld))
                {
                    simulator.AddSystem(new SimpleSystem(4));

                    Assert.That(simulator.ContainsSystem<SimpleSystem>(), Is.True);
                    Assert.That(simulator.Systems.Length, Is.EqualTo(1));

                    simulator.Update();

                    Assert.That(simulatorWorld.Count, Is.EqualTo(2));

                    Entity firstEntity = new(simulatorWorld, 1);
                    Assert.That(firstEntity.GetComponent<int>(), Is.EqualTo(4));

                    Entity beforeFinalizeEntity = new(simulatorWorld, 2);
                    Assert.That(beforeFinalizeEntity.GetComponent<bool>(), Is.EqualTo(false));

                    Assert.That(simulator.ContainsSystem<SimpleSystem>(), Is.True);
                    simulator.RemoveSystem<SimpleSystem>();
                }

                Entity secondEntity = new(simulatorWorld, 2);
                Assert.That(secondEntity.GetComponent<bool>(), Is.EqualTo(true));
            }
        }

        [Test]
        public void ReceiveMessages()
        {
            using (World world = CreateWorld())
            {
                using (Simulator simulator = new(world))
                {
                    simulator.AddSystem(new MessageHandlerSystem());

                    Assert.That(simulator.Systems.Length, Is.EqualTo(1));

                    StatusCode statusCode = simulator.TryHandleMessage(new ASCIIText256("test message"));
                    Assert.That(statusCode, Is.Not.EqualTo(default(StatusCode)));

                    statusCode = simulator.TryHandleMessage(new ASCIIText256("and another one"));
                    Assert.That(statusCode, Is.Not.EqualTo(default(StatusCode)));

                    Assert.That(world.Count, Is.EqualTo(2));

                    Entity firstEntity = new(world, 1);
                    Entity secondEntity = new(world, 2);
                    Assert.That(firstEntity.GetComponent<ASCIIText256>(), Is.EqualTo(new ASCIIText256("test message")));
                    Assert.That(secondEntity.GetComponent<ASCIIText256>(), Is.EqualTo(new ASCIIText256("and another one")));
                }
            }
        }

        [Test, CancelAfter(1000)]
        public void SystemInsideSystem()
        {
            using (World world = CreateWorld())
            {
                using (Simulator simulator = new(world))
                {
                    simulator.AddSystem(new StackedSystem());

                    Assert.That(simulator.Systems.Length, Is.EqualTo(2));

                    simulator.Update();

                    Assert.That(simulator.Systems.Length, Is.EqualTo(2));
                    Assert.That(world.Count, Is.EqualTo(2));

                    Entity firstEntity = new(world, 1);
                    Assert.That(firstEntity.GetComponent<int>(), Is.EqualTo(4));

                    Entity beforeFinalizeEntity = new(world, 2);
                    Assert.That(beforeFinalizeEntity.GetComponent<bool>(), Is.EqualTo(false));

                    simulator.RemoveSystem<StackedSystem>();

                    Assert.That(simulator.Systems.Length, Is.EqualTo(0));
                }

                Entity secondEntity = new(world, 2);
                Assert.That(secondEntity.GetComponent<bool>(), Is.EqualTo(true));
            }
        }

        [Test, CancelAfter(1000)]
        public void SystemDisposesWhenSimulatorIsInCorrectOrder()
        {
            using (World world = CreateWorld())
            {
                using (Simulator simulator = new(world))
                {
                    simulator.AddSystem(new StackedSystem());

                    Assert.That(simulator.Systems.Length, Is.EqualTo(2));

                    simulator.Update();
                }

                Entity secondEntity = new(world, 2);
                Assert.That(secondEntity.GetComponent<bool>(), Is.EqualTo(true));
            }
        }
    }
}