using Simulation.Components;
using System;
using System.Threading;
using Worlds;

namespace Simulation.Tests
{
    public class ProgramTests : SimulationTests
    {
        protected override void SetUp()
        {
            base.SetUp();
            ComponentType.Register<bool>();
        }

        [Test]
        [CancelAfter(1000)]
        public void SimpleProgram(CancellationToken token)
        {
            Program program = Program.Create<Calculator>(World);
            Assert.That(program.State, Is.EqualTo(IsProgram.State.Uninitialized));

            StatusCode statusCode;
            do
            {
                Simulator.Update();

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
            Assert.That(statusCode, Is.EqualTo(StatusCode.Success(100)));
        }

        [Test]
        public void ExitEarly()
        {
            Program program = Program.Create<Calculator>(World);

            Assert.That(program.State, Is.EqualTo(IsProgram.State.Uninitialized));

            Simulator.Update(); //to invoke the initializer and update
            ref Calculator calculator = ref program.Read<Calculator>();

            Assert.That(calculator.text.ToString(), Is.EqualTo("Running2"));
            program.Dispose();
            Simulator.Update(); //to invoke the finisher

            Assert.That(calculator.value, Is.EqualTo(calculator.additive));
            Assert.That(calculator.text.ToString(), Is.EqualTo("Finished 0"));
        }

        [Test]
        [CancelAfter(1000)]
        public void ReRunProgram(CancellationToken token)
        {
            Program program = Program.Create<Calculator>(World);

            while (!program.IsFinished(out StatusCode statusCode))
            {
                Simulator.Update();
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
                Simulator.Update();
                if (token.IsCancellationRequested)
                {
                    Assert.Fail("Test took too long");
                }
            }

            Assert.That(program.State, Is.EqualTo(IsProgram.State.Finished));
            Assert.That(calculator.value, Is.EqualTo(calculator.limit * calculator.additive));
        }
    }
}