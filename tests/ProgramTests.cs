using Collections;
using Simulation.Components;
using System;
using System.Threading;
using Unmanaged;

namespace Simulation.Tests
{
    public class ProgramTests : SimulationTests
    {
        [Test]
        [CancelAfter(1000)]
        public void SimpleProgram(CancellationToken token)
        {
            Program program = new Program<Calculator>(world);
            Assert.That(program.State, Is.EqualTo(IsProgram.State.Uninitialized));

            StatusCode statusCode;
            do
            {
                simulator.Update();

                ref Calculator calculator = ref program.Read<Calculator>();
                Console.WriteLine(calculator.value);
                Assert.That(program.State, Is.Not.EqualTo(IsProgram.State.Uninitialized));
                if (token.IsCancellationRequested)
                {
                    Assert.Fail("Test took too long");
                }
            }
            while (!program.IsFinished(out statusCode));

            Calculator finishedCalculator = program.Read<Calculator>();
            Assert.That(finishedCalculator.value, Is.EqualTo(finishedCalculator.limit * finishedCalculator.additive));
            Assert.That(program.State, Is.EqualTo(IsProgram.State.Finished));
            Assert.That(statusCode, Is.EqualTo(StatusCode.Success(0)));
        }

        [Test]
        public void ExitEarly()
        {
            Program program = new Program<Calculator>(world);

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
            Program program = new Program<Calculator>(world);

            while (!program.IsFinished(out StatusCode statusCode))
            {
                simulator.Update();
                if (token.IsCancellationRequested)
                {
                    Assert.Fail("Test took too long");
                }
            }

            ref Calculator calculator = ref program.Read<Calculator>();
            Assert.That(program.State, Is.EqualTo(IsProgram.State.Finished));
            Assert.That(calculator.value, Is.EqualTo(calculator.limit * calculator.additive));
            program.Restart();

            while (!program.IsFinished(out StatusCode statusCode))
            {
                simulator.Update();
                if (token.IsCancellationRequested)
                {
                    Assert.Fail("Test took too long");
                }
            }

            Assert.That(program.State, Is.EqualTo(IsProgram.State.Finished));
            Assert.That(calculator.value, Is.EqualTo(calculator.limit * calculator.additive));
        }

        [Test, CancelAfter(3000)]
        public void SystemsInitializeWithProgram(CancellationToken token)
        {
            using List<SystemContainer> startedWorlds = new();
            using List<SystemContainer> updatedWorlds = new();
            using List<SystemContainer> finishedWorlds = new();

            Allocation input = Allocation.Create((startedWorlds, updatedWorlds, finishedWorlds));
            SystemContainer<DummySystem> system = simulator.AddSystem<DummySystem>(input);
            {
                using (Program program = new Program<DummyProgram>(world, new(TimeSpan.FromSeconds(2))))
                {
                    StatusCode statusCode;
                    while (!program.IsFinished(out statusCode))
                    {
                        simulator.Update();

                        if (token.IsCancellationRequested)
                        {
                            Assert.Fail("Test took too long");
                        }
                    }

                    Assert.That(statusCode, Is.EqualTo(StatusCode.Success(0)));
                }

            }
            system.RemoveSelf();

            Assert.That(startedWorlds.Count, Is.EqualTo(2));
            Assert.That(updatedWorlds.Count, Is.GreaterThan(2));
            Assert.That(finishedWorlds.Count, Is.EqualTo(2));
        }

        [Test, CancelAfter(3000)]
        public void ProgramAskingSimulatorUpdateSystems(CancellationToken token)
        {
            using List<SystemContainer> startedWorlds = new();
            using List<SystemContainer> updatedWorlds = new();
            using List<SystemContainer> finishedWorlds = new();

            Allocation input = Allocation.Create((startedWorlds, updatedWorlds, finishedWorlds));
            SystemContainer<DummySystem> system = simulator.AddSystem<DummySystem>(input);
            {
                using (Program program = new Program<ProgramThatUpdatesSystemsOnStart>(world))
                {
                    StatusCode statusCode;
                    while (!program.IsFinished(out statusCode))
                    {
                        simulator.Update();

                        if (token.IsCancellationRequested)
                        {
                            Assert.Fail("Test took too long");
                        }
                    }

                    Assert.That(statusCode, Is.EqualTo(StatusCode.Success(0)));
                }
            }
            system.RemoveSelf();

            Assert.That(startedWorlds.Count, Is.EqualTo(2));
            Assert.That(updatedWorlds.Count, Is.EqualTo(3));
            Assert.That(finishedWorlds.Count, Is.EqualTo(2));
        }
    }
}