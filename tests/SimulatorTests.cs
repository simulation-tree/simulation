using System.Threading;
using Unmanaged.Tests;
using Worlds;

namespace Simulation.Tests
{
    public class SimulatorTests : UnmanagedTests
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
            using World world = new();
            using Simulator simulator = new(world);

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
        public void UpdatingSimulatorForward()
        {
            using World world = new();
            using Simulator simulator = new(world);

            TimeSystem system = new();
            simulator.Add(system);

            simulator.Update(20);
            Assert.That(simulator.Time, Is.EqualTo(20));
            Assert.That(system.time, Is.EqualTo(20));

            simulator.Update(10);
            Assert.That(simulator.Time, Is.EqualTo(30));
            Assert.That(system.time, Is.EqualTo(30));

            simulator.Update();
            Thread.Sleep(1000);
            simulator.Update();
            Assert.That(simulator.Time, Is.EqualTo(31).Within(0.05));
            Assert.That(system.time, Is.EqualTo(31).Within(0.05));

            simulator.Update(5);
            Assert.That(simulator.Time, Is.EqualTo(36).Within(0.05));
            Assert.That(system.time, Is.EqualTo(36).Within(0.05));

            simulator.Update(0);
            Assert.That(simulator.Time, Is.EqualTo(36).Within(0.05));
            Assert.That(system.time, Is.EqualTo(36).Within(0.05));

            simulator.Remove(system);
            simulator.Update(10);
            Assert.That(simulator.Time, Is.EqualTo(46).Within(0.05));
            Assert.That(system.time, Is.EqualTo(36).Within(0.05));
        }
    }
}