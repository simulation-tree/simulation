using Simulation.Functions;
using System;
using Unmanaged;
using Worlds;

namespace Simulation
{
    public interface IProgram
    {
        public (StartProgram start, UpdateProgram update, FinishProgram finish) Functions
        {
            get
            {
                return default;
            }
        }

        void Start(in Simulator simulator, in Allocation allocation, in World world);
        StatusCode Update(in TimeSpan delta);
        void Finish(in StatusCode statusCode);
    }
}