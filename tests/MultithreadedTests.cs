namespace Simulation.Tests
{
    public class MultithreadedTests : SimulationTests
    {
        [Test]
        public void SystemGroupCreation()
        {
            SystemGroup<UpdateMessage> group = new("update group");
            TimeSystem a = new();
            group.Add(a);
            Simulator.Add(group);
            Simulator.Broadcast(new UpdateMessage(0.1));
            Assert.That(a.time, Is.EqualTo(0.1));
            Simulator.Remove(group);
            Simulator.Broadcast(new UpdateMessage(0.1));
            Assert.That(a.time, Is.EqualTo(0.1));
            group.Dispose();
        }

        [Test]
        public void MultipleSystems()
        {
            SystemGroup<UpdateMessage> group = new("update group");
            TimeSystem a = new();
            TimeSystem b = new();
            group.Add(a);
            group.Add(b);
            Simulator.Add(group);
            Simulator.Broadcast(new UpdateMessage(0.1));
            Assert.That(a.time, Is.EqualTo(0.1));
            Assert.That(b.time, Is.EqualTo(0.1));
            Simulator.Remove(group);
            group.Dispose();
        }
    }
}