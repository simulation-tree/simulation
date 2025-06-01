using Simulation.Exceptions;

namespace Simulation.Tests
{
    public class SystemTests : SimulationTests
    {
        [Test]
        public void AddingAndRemovingSystems()
        {
            Assert.That(Simulator.Count, Is.EqualTo(0));
            EmptySystem system = new();
            Simulator.Add(system);
            Assert.That(Simulator.Count, Is.EqualTo(1));
            Assert.That(Simulator.Contains<EmptySystem>(), Is.True);
            Assert.That(Simulator.Systems, Has.Exactly(1).EqualTo(system));
            EmptySystem removedSystem = Simulator.Remove<EmptySystem>();
            Assert.That(Simulator.Count, Is.EqualTo(0));
            Assert.That(Simulator.Contains<EmptySystem>(), Is.False);
            Assert.That(removedSystem, Is.SameAs(system));
        }

        [Test]
        public void AddingDuplicateSystems()
        {
            EmptySystem a = new();
            Simulator.Add(a);
            EmptySystem b = new();
            Simulator.Add(b);
            Assert.That(Simulator.Count, Is.EqualTo(2));
            Assert.That(Simulator.Contains<EmptySystem>(), Is.True);
            Simulator.Remove<EmptySystem>();
            Assert.That(Simulator.Count, Is.EqualTo(1));
            Assert.That(Simulator.Contains<EmptySystem>(), Is.True);
            EmptySystem c = new();
            Simulator.Add(c);
            Assert.That(Simulator.Count, Is.EqualTo(2));
            Assert.That(Simulator.Contains<EmptySystem>(), Is.True);
            Simulator.Remove<EmptySystem>();
            Simulator.Remove<EmptySystem>();
            Assert.That(Simulator.Count, Is.EqualTo(0));
            Assert.That(Simulator.Contains<EmptySystem>(), Is.False);
            Assert.Throws<MissingSystemTypeException>(() => Simulator.Remove<EmptySystem>());
        }
    }
}