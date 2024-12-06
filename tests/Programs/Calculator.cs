using System;
using Unmanaged;
using Worlds;

namespace Simulation.Tests
{
    public partial struct Calculator : IProgram
    {
        public byte value;
        public byte limit;
        public byte additive;
        public FixedString text;

        private readonly World world;

        private Calculator(World world)
        {
            this.world = world;
        }

        readonly void IProgram.Start(in Simulator simulator, in Allocation allocation, in World world)
        {
            Calculator calculator = new(world);
            calculator.limit = 4;
            calculator.additive = 2;
            calculator.text = "Not Running";
            allocation.Write(calculator);
        }

        StatusCode IProgram.Update(in TimeSpan delta)
        {
            value += additive;
            text = "Running";
            text.Append(value);

            uint newEntity = world.CreateEntity();
            world.AddComponent(newEntity, true);
            if (world.Count >= limit)
            {
                return StatusCode.Success(100);
            }
            else
            {
                return StatusCode.Continue;
            }
        }

        void IProgram.Finish(in StatusCode statusCode)
        {
            USpan<char> buffer = stackalloc char[64];
            uint length = statusCode.ToString(buffer);
            text = new(buffer.Slice(0, length));
        }
    }
}
