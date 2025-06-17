namespace Simulation.Tests
{
    public class BroadcastingTests : SimulationTests
    {
        [Test]
        public void BroadcastingMessages()
        {
            TimeSystem system = new();
            Simulator.Broadcast(new UpdateMessage(1));
            Assert.That(system.time, Is.EqualTo(0));
            Simulator.Add(system);
            Simulator.Broadcast(new UpdateMessage(1));
            Assert.That(system.time, Is.EqualTo(1));
            Simulator.Broadcast(new UpdateMessage(1));
            Assert.That(system.time, Is.EqualTo(2));
            Simulator.Remove<TimeSystem>();
            Simulator.Broadcast(new UpdateMessage(1));
            Assert.That(system.time, Is.EqualTo(2));
        }

        [Test]
        public void BroadcastingToGlobalSimulator()
        {
            GlobalSimulatorLoader.Load();
            Assert.That(GlobalTimeSystem.time, Is.EqualTo(0));
            GlobalSimulator.Broadcast(new UpdateMessage(1));
            Assert.That(GlobalTimeSystem.time, Is.EqualTo(1));
            GlobalSimulator.Reset();
            GlobalSimulator.Broadcast(new UpdateMessage(1));
            Assert.That(GlobalTimeSystem.time, Is.EqualTo(1));
        }

        [Test]
        public void SystemsOutOfOrder()
        {
            TimeSystem system = new();
            using TextSystem text = new(Simulator);
            Simulator.Broadcast(new UpdateMessage(1));
            Assert.That(system.time, Is.EqualTo(0));
            Simulator.Add(system);
            Simulator.Add(text);
            Simulator.Broadcast(new UpdateMessage(1));
            Assert.That(system.time, Is.EqualTo(1));
            Assert.That(text.Text.ToString(), Is.EqualTo("1"));
            Simulator.Broadcast(new UpdateMessage(1));
            Assert.That(system.time, Is.EqualTo(2));
            Assert.That(text.Text.ToString(), Is.EqualTo("11"));
            Simulator.Remove<TimeSystem>();
            Simulator.Broadcast(new UpdateMessage(1));
            Assert.That(system.time, Is.EqualTo(2));
            Assert.That(text.Text.ToString(), Is.EqualTo("111"));
            Simulator.Remove<TextSystem>(false);
            Simulator.Broadcast(new UpdateMessage(1));
            Assert.That(system.time, Is.EqualTo(2));
            Assert.That(text.Text.ToString(), Is.EqualTo("111"));
        }

        [Test]
        public void DuplicateSystems()
        {
            TimeSystem a = new();
            TimeSystem b = new();
            Simulator.Add(a);
            Simulator.Add(b);
            Assert.That(a.time, Is.EqualTo(0));
            Assert.That(b.time, Is.EqualTo(0));
            Simulator.Broadcast(new UpdateMessage(1));
            Assert.That(a.time, Is.EqualTo(1));
            Assert.That(b.time, Is.EqualTo(1));
            Simulator.Broadcast(new UpdateMessage(9));
            Assert.That(a.time, Is.EqualTo(10));
            Assert.That(b.time, Is.EqualTo(10));
            Simulator.Remove<TimeSystem>(false);
            Assert.That(a.time, Is.EqualTo(10));
            Assert.That(b.time, Is.EqualTo(10));
            Simulator.Broadcast(new UpdateMessage(1));
            Assert.That(a.time, Is.EqualTo(10));
            Assert.That(b.time, Is.EqualTo(11));
            Simulator.Remove<TimeSystem>(false);
            Assert.That(a.time, Is.EqualTo(10));
            Assert.That(b.time, Is.EqualTo(11));
            Simulator.Broadcast(new UpdateMessage(1));
            Assert.That(a.time, Is.EqualTo(10));
            Assert.That(b.time, Is.EqualTo(11));
        }

        [Test]
        public void NestedEvents()
        {
            NestedSystem nested = new(Simulator);
            TimeSystem a = new();
            TimeSystem b = new();
            using TextSystem text = new(Simulator);
            Simulator.Add(a);
            Simulator.Add(nested);
            Simulator.Add(b);
            Simulator.Add(text);
            Assert.That(a.time, Is.EqualTo(0));
            Assert.That(b.time, Is.EqualTo(0));
            Assert.That(text.Text.ToString(), Is.EqualTo(string.Empty));
            Simulator.Broadcast(new AppendCharacter('a'));
            Assert.That(a.time, Is.EqualTo(5));
            Assert.That(b.time, Is.EqualTo(5));
            Assert.That(text.Text.ToString(), Is.EqualTo("5a"));
        }
    }
}