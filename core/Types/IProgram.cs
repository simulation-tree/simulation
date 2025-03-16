using Simulation.Functions;
using System;
using Worlds;

namespace Simulation
{
    /// <summary>
    /// Managed by a <see cref="Simulator"/> after all 
    /// <see cref="ISystem"/>s performed their behaviour.
    /// </summary>
    public interface IProgram
    {
        public (StartProgram start, UpdateProgram update, FinishProgram finish) Functions
        {
            get
            {
                return default;
            }
        }
    }

    /// <summary>
    /// Managed by a <see cref="Simulator"/> after all 
    /// <see cref="ISystem"/>s performed their behaviour.
    /// </summary>
    public interface IProgram<T> : IProgram where T : unmanaged, IProgram
    {
        void Start(ref T program, in Simulator simulator, in World world);
        StatusCode Update(in TimeSpan delta);
        void Finish(in StatusCode statusCode);
    }
}