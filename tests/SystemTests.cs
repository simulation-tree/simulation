using Unmanaged;
using Worlds;

namespace Simulation.Tests
{
    public class SystemTests : SimulationTests
    {
        [Test]
        public void SimpleTest()
        {
            using (World hostWorld = CreateWorld())
            {
                using (Simulator simulator = new(hostWorld))
                {
                    simulator.AddSystem<SimpleSystem>();

                    Assert.That(simulator.Systems.Length, Is.EqualTo(1));

                    simulator.Update();

                    Assert.That(hostWorld.Count, Is.EqualTo(2));

                    Entity firstEntity = new(hostWorld, 1);
                    Assert.That(firstEntity.GetComponent<uint>(), Is.EqualTo(4));

                    Entity beforeFinalizeEntity = new(hostWorld, 2);
                    Assert.That(beforeFinalizeEntity.GetComponent<bool>(), Is.EqualTo(false));

                    simulator.RemoveSystem<SimpleSystem>();
                }

                Entity secondEntity = new(hostWorld, 2);
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
                    simulator.AddSystem<MessageHandlerSystem>();

                    Assert.That(simulator.Systems.Length, Is.EqualTo(1));

                    bool handled = simulator.TryHandleMessage(new FixedString("test message"));
                    Assert.That(handled, Is.True);

                    handled = simulator.TryHandleMessage(new FixedString("and another one"));
                    Assert.That(handled, Is.True);

                    Assert.That(world.Count, Is.EqualTo(2));

                    Entity firstEntity = new(world, 1);
                    Entity secondEntity = new(world, 2);
                    Assert.That(firstEntity.GetComponent<FixedString>(), Is.EqualTo(new FixedString("test message")));
                    Assert.That(secondEntity.GetComponent<FixedString>(), Is.EqualTo(new FixedString("and another one")));
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
                    simulator.AddSystem<StackedSystem>();

                    Assert.That(simulator.Systems.Length, Is.EqualTo(2));

                    simulator.Update();

                    Assert.That(simulator.Systems.Length, Is.EqualTo(2));
                    Assert.That(world.Count, Is.EqualTo(2));

                    Entity firstEntity = new(world, 1);
                    Assert.That(firstEntity.GetComponent<uint>(), Is.EqualTo(4));

                    Entity beforeFinalizeEntity = new(world, 2);
                    Assert.That(beforeFinalizeEntity.GetComponent<bool>(), Is.EqualTo(false));

                    simulator.RemoveSystem<StackedSystem>();

                    Assert.That(simulator.Systems.Length, Is.EqualTo(0));
                }

                Entity secondEntity = new(world, 2);
                Assert.That(secondEntity.GetComponent<bool>(), Is.EqualTo(true));
            }
        }
    }
}