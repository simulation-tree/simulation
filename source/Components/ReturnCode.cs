using Worlds;

namespace Simulation.Components
{
    [Component]
    public struct ReturnCode
    {
        public static readonly ReturnCode Continue = new(0);

        public uint value;

        public ReturnCode(uint value)
        {
            this.value = value;
        }
    }
}