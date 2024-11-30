using Simulation.Components;
using Simulation.Functions;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Unmanaged;
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

            uint returnCode;
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
            while (!program.IsFinished(out returnCode));

            Calculator finishedCalculator = program.Read<Calculator>();
            Assert.That(finishedCalculator.value, Is.EqualTo(finishedCalculator.limit * finishedCalculator.additive));
            Assert.That(program.State, Is.EqualTo(IsProgram.State.Finished));
            Assert.That(returnCode, Is.EqualTo(1337u));
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
            Assert.That(calculator.text.ToString(), Is.EqualTo("Finished0"));
        }

        [Test]
        [CancelAfter(1000)]
        public void ReRunProgram(CancellationToken token)
        {
            Program program = Program.Create<Calculator>(World);

            while (!program.IsFinished(out uint returnCode))
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

            while (!program.IsFinished(out uint returnCode))
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

        public struct Calculator : IProgram
        {
            public byte value;
            public byte limit;
            public byte additive;
            public FixedString text;

            unsafe readonly StartProgram IProgram.Start => new(&Start);
            unsafe readonly UpdateProgram IProgram.Update => new(&Update);
            unsafe readonly FinishProgram IProgram.Finish => new(&Finish);

            [UnmanagedCallersOnly]
            private static void Start(Simulator simulator, Allocation allocation, World world)
            {
                Calculator calculator = new();
                calculator.limit = 4;
                calculator.additive = 2;
                calculator.text = "Not Running";
                allocation.Write(calculator);
            }

            [UnmanagedCallersOnly]
            private static uint Update(Simulator simulator, Allocation allocation, World world, TimeSpan delta)
            {
                ref Calculator calculator = ref allocation.Read<Calculator>();
                calculator.value += calculator.additive;
                calculator.text = "Running";
                calculator.text.Append(calculator.value);

                uint newEntity = world.CreateEntity();
                world.AddComponent(newEntity, true);
                if (world.Count >= calculator.limit)
                {
                    return 1337;
                }
                else
                {
                    return default;
                }
            }

            [UnmanagedCallersOnly]
            private static void Finish(Simulator simulator, Allocation allocation, World world, uint returnCode)
            {
                ref Calculator calculator = ref allocation.Read<Calculator>();
                calculator.text = "Finished";
                calculator.text.Append(returnCode);
            }
        }
    }
}
