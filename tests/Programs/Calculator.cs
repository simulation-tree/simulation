using System;
using Unmanaged;
using Worlds;

namespace Simulation.Tests
{
    public partial struct Calculator : IProgram<Calculator>
    {
        public int value;
        public ASCIIText256 text;

        public readonly int limit;
        public readonly int additive;
        private readonly World world;

        private Calculator(World world, int limit, int additive)
        {
            this.limit = limit;
            this.additive = additive;
            this.world = world;
        }

        public Calculator(int limit, int additive)
        {
            this.limit = limit;
            this.additive = additive;
        }

        readonly void IProgram<Calculator>.Start(ref Calculator calculator, in Simulator simulator, in World world)
        {
            calculator = new(world, calculator.limit, calculator.additive);
            calculator.text = "Not running";
        }

        StatusCode IProgram<Calculator>.Update(in TimeSpan delta)
        {
            value += additive;
            text = "Running";
            text.Append(value);

            uint newEntity = world.CreateEntity();
            world.AddComponent(newEntity, true);
            if (world.Count >= limit)
            {
                return StatusCode.Success(0);
            }
            else
            {
                return StatusCode.Continue;
            }
        }

        void IProgram<Calculator>.Finish(in StatusCode statusCode)
        {
            Span<char> buffer = stackalloc char[64];
            int length = statusCode.ToString(buffer);
            text = new(buffer.Slice(0, length));
        }
    }
}