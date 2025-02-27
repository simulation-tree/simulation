using Collections.Generic;
using Simulation.Components;
using System;
using Worlds;

namespace Simulation.Pointers
{
    internal struct Simulator
    {
        public DateTime lastUpdateTime;
        public readonly uint programComponent;
        public readonly World world;
        public readonly List<SystemContainer> systems;
        public readonly List<ProgramContainer> programs;
        public readonly List<ProgramContainer> activePrograms;
        public readonly Dictionary<uint, ProgramContainer> programsMap;

        internal Simulator(World world)
        {
            this.world = world;
            programComponent = world.Schema.GetComponentTypeIndex<IsProgram>();
            lastUpdateTime = DateTime.MinValue;
            systems = new(4);
            programs = new(4);
            activePrograms = new(4);
            programsMap = new(4);
        }
    }
}
