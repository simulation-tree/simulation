#if !NET
using Worlds;

namespace Simulation
{
    /// <summary>
    /// An entity that represents a program running in a <see cref="World"/>,
    /// operated by a <see cref="Simulator"/>.
    /// </summary>
    public readonly partial struct Program : IProgramEntity
    {
        public readonly World world;
        public readonly uint value;

        public readonly void Dispose()
        {
            world.DestroyEntity(value);
        }

        public readonly ref T GetComponent<T>() where T : unmanaged
        {
            return ref world.GetComponent<T>(value);
        }

        public static implicit operator Entity(Program program)
        {
            return program.AsEntity();
        }
    }
}
#endif