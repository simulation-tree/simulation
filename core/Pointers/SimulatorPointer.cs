using Collections.Generic;
using System;
using Worlds;

namespace Simulation.Pointers
{
    internal struct SimulatorPointer
    {
        public DateTime lastUpdateTime;
        public int programComponent;
        public World world;
        public List<SystemContainer> systems;
        public List<ProgramContainer> programs;
        public List<ProgramContainer> activePrograms;
        public Dictionary<uint, ProgramContainer> programsMap;
        public MessageHandlers handlers;
    }
}