using Collections.Generic;
using Simulation.Components;
using System;
using System.Threading;
using Unmanaged;
using Worlds;

namespace Simulation.Tests
{
    public class ProgramTests : SimulationTests
    {
        [Test]
        [CancelAfter(1000)]
        public void SimpleProgram(CancellationToken token)
        {
            using List<int> values = new();
            Program program = Program.Create(world, new Calculator(4, 2));
            Assert.That(program.State, Is.EqualTo(IsProgram.State.Uninitialized));

            StatusCode statusCode;
            do
            {
                simulator.Update();

                ref Calculator calculator = ref program.Read<Calculator>();
                values.Add(calculator.value);
                Assert.That(program.State, Is.Not.EqualTo(IsProgram.State.Uninitialized));
                token.ThrowIfCancellationRequested();
            }
            while (!program.IsFinished(out statusCode));

            Calculator finishedCalculator = program.Read<Calculator>();
            Assert.That(finishedCalculator.value, Is.EqualTo(finishedCalculator.limit * finishedCalculator.additive));
            Assert.That(program.State, Is.EqualTo(IsProgram.State.Finished));
            Assert.That(statusCode, Is.EqualTo(StatusCode.Success(0)));
            Assert.That(values.Count, Is.EqualTo(4));
            Assert.That(values[0], Is.EqualTo(2));
            Assert.That(values[1], Is.EqualTo(4));
            Assert.That(values[2], Is.EqualTo(6));
            Assert.That(values[3], Is.EqualTo(8));
        }

        [Test]
        public void ExitEarly()
        {
            Program program = Program.Create(world, new Calculator(4, 2));

            Assert.That(program.State, Is.EqualTo(IsProgram.State.Uninitialized));

            simulator.Update(); //to invoke the initializer and update
            ref Calculator calculator = ref program.Read<Calculator>();

            Assert.That(calculator.text.ToString(), Is.EqualTo("Running2"));
            program.Dispose();

            simulator.Update(); //to invoke the finisher

            Assert.That(calculator.value, Is.EqualTo(calculator.additive));
            Assert.That(calculator.text.ToString(), Is.EqualTo(StatusCode.Termination.ToString()));
        }

        [Test]
        [CancelAfter(1000)]
        public void ReRunProgram(CancellationToken token)
        {
            Program program = Program.Create(world, new Calculator(4, 2));

            while (!program.IsFinished(out StatusCode statusCode))
            {
                simulator.Update();
                token.ThrowIfCancellationRequested();
            }

            ref Calculator calculator = ref program.Read<Calculator>();
            Assert.That(program.State, Is.EqualTo(IsProgram.State.Finished));
            Assert.That(calculator.value, Is.EqualTo(calculator.limit * calculator.additive));
            program.Restart();

            while (!program.IsFinished(out StatusCode statusCode))
            {
                simulator.Update();
                token.ThrowIfCancellationRequested();
            }

            Assert.That(program.State, Is.EqualTo(IsProgram.State.Finished));
            Assert.That(calculator.value, Is.EqualTo(calculator.limit * calculator.additive));
        }

        [Test, CancelAfter(3000)]
        public void SystemsInitializeWithProgram(CancellationToken token)
        {
            using List<World> startedWorlds = new();
            using List<World> updatedWorlds = new();
            using List<World> finishedWorlds = new();
            using MemoryAddress disposed = MemoryAddress.AllocateValue(false);

            simulator.AddSystem(new DummySystem(startedWorlds, updatedWorlds, finishedWorlds, disposed));
            using (Program program = Program.Create(world, new TimedProgram(2)))
            {
                StatusCode statusCode;
                while (!program.IsFinished(out statusCode))
                {
                    simulator.Update();
                    token.ThrowIfCancellationRequested();
                }

                Assert.That(statusCode, Is.EqualTo(StatusCode.Success(0)));
            }
            simulator.RemoveSystem<DummySystem>();

            Assert.That(startedWorlds.Count, Is.EqualTo(2));
            Assert.That(updatedWorlds.Count, Is.GreaterThan(2));
            Assert.That(finishedWorlds.Count, Is.EqualTo(2));
        }

        [Test, CancelAfter(3000)]
        public void ProgramAskingSimulatorUpdateSystems(CancellationToken token)
        {
            using List<World> startedWorlds = new();
            using List<World> updatedWorlds = new();
            using List<World> finishedWorlds = new();
            using MemoryAddress disposed = MemoryAddress.AllocateValue(false);

            simulator.AddSystem(new DummySystem(startedWorlds, updatedWorlds, finishedWorlds, disposed));
            World programWorld;
            using (Program program = new Program<ProgramThatUpdatesSystemsOnStart>(world))
            {
                programWorld = program.ProgramWorld;
                simulator.Update();
                if (program.IsFinished(out StatusCode statusCode))
                {
                    Assert.That(statusCode, Is.EqualTo(StatusCode.Success(0)));
                }
                else
                {
                    throw new InvalidOperationException("Program did not finish as expected");
                }
            }
            simulator.RemoveSystem<DummySystem>();

            Assert.That(startedWorlds.Count, Is.EqualTo(2));
            Assert.That(startedWorlds[0], Is.EqualTo(world));
            Assert.That(startedWorlds[1], Is.EqualTo(programWorld));

            Assert.That(updatedWorlds.Count, Is.EqualTo(3));
            Assert.That(updatedWorlds[0], Is.EqualTo(world));
            Assert.That(updatedWorlds[1], Is.EqualTo(world));
            Assert.That(updatedWorlds[2], Is.EqualTo(programWorld));

            Assert.That(finishedWorlds.Count, Is.EqualTo(2));
            Assert.That(finishedWorlds[0], Is.EqualTo(programWorld));
            Assert.That(finishedWorlds[1], Is.EqualTo(world));
        }
    }
}