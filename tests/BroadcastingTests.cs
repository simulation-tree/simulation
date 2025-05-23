namespace Simulation.Tests
{
    public class BroadcastingTests : SimulationTests
    {
        [Test]
        public void BroadcastingMessages()
        {
            TimeSystem system = new();
            Simulator.Broadcast(new UpdateMessage(1f));
            Assert.That(system.time, Is.EqualTo(0f));
            Simulator.Add(system);
            Simulator.Broadcast(new UpdateMessage(1f));
            Assert.That(system.time, Is.EqualTo(1f));
            Simulator.Broadcast(new UpdateMessage(1f));
            Assert.That(system.time, Is.EqualTo(2f));
            Simulator.Remove<TimeSystem>();
            Simulator.Broadcast(new UpdateMessage(1f));
            Assert.That(system.time, Is.EqualTo(2f));
        }

        [Test]
        public void BroadcastingWithSystemsOutOfOrder()
        {
            TimeSystem system = new();
            using TextSystem text = new();
            Simulator.Broadcast(new UpdateMessage(1f));
            Assert.That(system.time, Is.EqualTo(0f));
            Simulator.Add(system);
            Simulator.Add(text);
            Simulator.Broadcast(new UpdateMessage(1f));
            Assert.That(system.time, Is.EqualTo(1f));
            Assert.That(text.Text.ToString(), Is.EqualTo("1"));
            Simulator.Broadcast(new UpdateMessage(1f));
            Assert.That(system.time, Is.EqualTo(2f));
            Assert.That(text.Text.ToString(), Is.EqualTo("11"));
            Simulator.Remove<TimeSystem>();
            Simulator.Broadcast(new UpdateMessage(1f));
            Assert.That(system.time, Is.EqualTo(2f));
            Assert.That(text.Text.ToString(), Is.EqualTo("111"));
            Simulator.Remove<TextSystem>(false);
            Simulator.Broadcast(new UpdateMessage(1f));
            Assert.That(system.time, Is.EqualTo(2f));
            Assert.That(text.Text.ToString(), Is.EqualTo("111"));
        }
    }
}