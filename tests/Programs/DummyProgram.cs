using Collections.Generic;
using System;
using Unmanaged;
using Worlds;

namespace Simulation.Tests
{
    public partial struct DummyProgram : IProgram
    {
        private readonly List<SystemContainer> systems;
        private TimeSpan time;

        [Obsolete("Default constructor not supported", true)]
        public DummyProgram()
        {
            throw new NotSupportedException();
        }

        public DummyProgram(TimeSpan duration)
        {
            systems = new();
            time = duration;
        }

        void IProgram.Finish(in StatusCode statusCode)
        {
            systems.Dispose();
        }

        void IProgram.Start(in Simulator simulator, in Allocation allocation, in World world)
        {
        }

        StatusCode IProgram.Update(in TimeSpan delta)
        {
            time -= delta;
            if (time.TotalSeconds <= 0)
            {
                return StatusCode.Success(0);
            }

            return StatusCode.Continue;
        }
    }
}
