using System.Threading;
using Worlds;

namespace Simulation.Tests
{
    public class SimulatorTests : SimulationTests
    {
        [Test]
        public void CreatingAndDisposing()
        {
            using World world = new();
            Simulator simulator = new(world);
            Assert.That(simulator.IsDisposed, Is.False);
            simulator.Dispose();
            Assert.That(simulator.IsDisposed, Is.True);

            simulator = new(world);
            Assert.That(simulator.IsDisposed, Is.False);
            simulator.Dispose();
            Assert.That(simulator.IsDisposed, Is.True);
        }

        [Test]
        public void AddingAndRemovingSystems()
        {
            Assert.That(simulator.Count, Is.EqualTo(0));
            EmptySystem system = new();
            simulator.Add(system);
            Assert.That(simulator.Count, Is.EqualTo(1));
            Assert.That(simulator.Contains<EmptySystem>(), Is.True);
            Assert.That(simulator.Systems, Has.Exactly(1).EqualTo(system));
            EmptySystem removedSystem = simulator.Remove<EmptySystem>();
            Assert.That(simulator.Count, Is.EqualTo(0));
            Assert.That(removedSystem, Is.SameAs(system));
        }

        [Test]
        public void BroadcastingMessages()
        {
            TimeSystem system = new();
            Broadcast(new UpdateMessage(1f));
            Assert.That(system.time, Is.EqualTo(0f));
            simulator.Add(system);
            Broadcast(new UpdateMessage(1f));
            Assert.That(system.time, Is.EqualTo(1f));
            Broadcast(new UpdateMessage(1f));
            Assert.That(system.time, Is.EqualTo(2f));
            simulator.Remove<TimeSystem>();
            Broadcast(new UpdateMessage(1f));
            Assert.That(system.time, Is.EqualTo(2f));
        }

        [Test]
        public void BroadcastingWithSystemsOutOfOrder()
        {
            TimeSystem system = new();
            using TextSystem text = new();
            Broadcast(new UpdateMessage(1f));
            Assert.That(system.time, Is.EqualTo(0f));
            simulator.Add(system);
            simulator.Add(text);
            Broadcast(new UpdateMessage(1f));
            Assert.That(system.time, Is.EqualTo(1f));
            Assert.That(text.Text.ToString(), Is.EqualTo("1"));
            Broadcast(new UpdateMessage(1f));
            Assert.That(system.time, Is.EqualTo(2f));
            Assert.That(text.Text.ToString(), Is.EqualTo("11"));
            simulator.Remove<TimeSystem>();
            Broadcast(new UpdateMessage(1f));
            Assert.That(system.time, Is.EqualTo(2f));
            Assert.That(text.Text.ToString(), Is.EqualTo("111"));
            simulator.Remove<TextSystem>(false);
            Broadcast(new UpdateMessage(1f));
            Assert.That(system.time, Is.EqualTo(2f));
            Assert.That(text.Text.ToString(), Is.EqualTo("111"));
        }

        [Test]
        public void UpdatingSimulatorForward()
        {
            TimeSystem system = new();
            simulator.Add(system);

            Update(20);
            Assert.That(simulator.Time, Is.EqualTo(20));
            Assert.That(system.time, Is.EqualTo(20));

            Update(10);
            Assert.That(simulator.Time, Is.EqualTo(30));
            Assert.That(system.time, Is.EqualTo(30));

            Update();
            Thread.Sleep(1000);
            Update();
            Assert.That(simulator.Time, Is.EqualTo(31).Within(0.01));
            Assert.That(system.time, Is.EqualTo(31).Within(0.01));

            Update(5);
            Assert.That(simulator.Time, Is.EqualTo(36).Within(0.01));
            Assert.That(system.time, Is.EqualTo(36).Within(0.01));

            Update(0);
            Assert.That(simulator.Time, Is.EqualTo(36).Within(0.01));
            Assert.That(system.time, Is.EqualTo(36).Within(0.01));

            simulator.Remove(system);
            Update(10);
            Assert.That(simulator.Time, Is.EqualTo(46).Within(0.01));
            Assert.That(system.time, Is.EqualTo(36).Within(0.01));
        }
    }
}