using Collections.Generic;
using Unmanaged;
using Worlds;

namespace Simulation.Tests
{
    public class SimulatorTests : SimulationTests
    {
        [Test]
        public void VerifyOrderOfOperations()
        {
            using World simulatorWorld = CreateWorld();
            using (Simulator simulator = new(simulatorWorld))
            {
                using List<World> systemStartedWorlds = new();
                using List<World> programStartedWorlds = new();
                using List<World> systemUpdatedWorlds = new();
                using List<World> systemFinishedWorlds = new();
                using MemoryAddress disposed = MemoryAddress.AllocateValue(false);
                simulator.AddSystem(new DummySystem(systemStartedWorlds, systemUpdatedWorlds, systemFinishedWorlds, disposed));
                World programWorld;
                using (Program program = Program.Create(simulatorWorld, new DummyProgram(4, programStartedWorlds)))
                {
                    programWorld = program.ProgramWorld;
                    simulator.Update();

                    Assert.That(systemStartedWorlds.Count, Is.EqualTo(2));
                    Assert.That(systemStartedWorlds[0], Is.EqualTo(simulatorWorld));
                    Assert.That(systemStartedWorlds[1], Is.EqualTo(programWorld));

                    Assert.That(programStartedWorlds.Count, Is.EqualTo(1));
                    Assert.That(programStartedWorlds[0], Is.EqualTo(programWorld));

                    Assert.That(systemUpdatedWorlds.Count, Is.EqualTo(2));
                    Assert.That(systemUpdatedWorlds[0], Is.EqualTo(simulatorWorld));
                    Assert.That(systemUpdatedWorlds[1], Is.EqualTo(programWorld));

                    simulator.Update();

                    Assert.That(systemUpdatedWorlds.Count, Is.EqualTo(4));
                    Assert.That(systemUpdatedWorlds[2], Is.EqualTo(simulatorWorld));
                    Assert.That(systemUpdatedWorlds[3], Is.EqualTo(programWorld));

                    simulator.Update();

                    Assert.That(systemUpdatedWorlds.Count, Is.EqualTo(6));
                    Assert.That(systemUpdatedWorlds[4], Is.EqualTo(simulatorWorld));
                    Assert.That(systemUpdatedWorlds[5], Is.EqualTo(programWorld));

                    simulator.Update();

                    Assert.That(systemUpdatedWorlds.Count, Is.EqualTo(8));
                    Assert.That(systemUpdatedWorlds[6], Is.EqualTo(simulatorWorld));
                    Assert.That(systemUpdatedWorlds[7], Is.EqualTo(programWorld));
                }

                simulator.RemoveSystem<DummySystem>();

                Assert.That(disposed.Read<bool>(), Is.True);

                Assert.That(systemFinishedWorlds.Count, Is.EqualTo(2));
                Assert.That(systemFinishedWorlds[0], Is.EqualTo(programWorld));
                Assert.That(systemFinishedWorlds[1], Is.EqualTo(simulatorWorld));
            }
        }
    }
}