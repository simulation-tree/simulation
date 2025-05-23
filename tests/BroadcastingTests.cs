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
        public void BroadcastingWithSystemsOutOfOrder()
        {
            TimeSystem system = new();
            using TextSystem text = new();
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
    }
}