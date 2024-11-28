# Simulation
Library implementing the concept of programs executing within a simulator.

### Systems
Systems are defined by implementing the `ISystem` interface, and contain
functions for initializing, iterating, and finalizing the system with all
worlds that the simulator is aware of:
```cs
public unsafe readonly struct ExampleSystem : ISystem
{
    readonly InitializeFunction ISystem.Initialize => new(&Initialize);
    readonly IterateFunction ISystem.Iterate => new(&Iterate);
    readonly FinalizeFunction ISystem.Finalize => new(&Finalize);

    [UnmanagedCallersOnly]
    private static void Initialize(SystemContainer container, World world)
    {
        if (container.World == world)
        {
            Entity firstEntity = new(world);
            firstEntity.AddComponent(0u);
        }
    }

    [UnmanagedCallersOnly]
    private static void Iterate(SystemContainer container, World world, TimeSpan delta)
    {
        if (container.World == world)
        {
            ref uint firstEntityValue = ref world.GetComponentRef<uint>(1);
            firstEntityValue++;
        }
    }

    [UnmanagedCallersOnly]
    private static void Finalize(SystemContainer container, World world)
    {
        if (container.World == world)
        {
            ref uint firstEntityValue = ref world.GetComponentRef<uint>(1);
            firstEntityValue *= 10;
        }
    }
}
```
> A simulator may iterate over a system with multiple worlds, so the `World == world` check is necessary
to ensure that the code only runs for the simulator's world.

### Simulators
Simulators are objects that contain and update systems as well as programs found
within the simulator's world:
```cs
using (World world = new())
{
    using (Simulator simulator = new(world))
    {
        simulator.AddSystem<ExampleSystem>();
        simulator.Update(TimeSpan.FromSeconds(1));
        simulator.RemoveSystem<ExampleSystem>();
    }
}
```

### Programs
Program entities are defined by implementing the `IProgram` interface, and
expose functions that are called by the simulator to start, update, and finish.

They are operated by a simulator, and have their own world to interact with separate
from the simulator's world:
```cs
public unsafe readonly struct ExampleProgram : IProgram
{
    readonly StartFunction IProgram.Start => new(&Start);
    readonly UpdateFunction IProgram.Update => new(&Update);
    readonly FinishFunction IProgram.Finish => new(&Finish);

    private ExampleProgram() { }

    [UnmanagedCallersOnly]
    private static void Start(Simulator simulator, Allocation allocation, World world)
    {
        allocation.Write(new ExampleProgram());
    }

    [UnmanagedCallersOnly]
    private static uint Update(Simulator simulator, Allocation allocation, World world, TimeSpan delta)
    {
        return 0;
    }

    [UnmanagedCallersOnly]
    private static void Finish(Simulator simulator, Allocation allocation, World world, uint returnCode)
    {
    }
}

public static int Main()
{
    uint returnCode;
    using (World world = new())
    {
        using (Simulator simulator = new(world))
        {
            simulator.AddSystem<ExampleSystem>();
            using (Program program = Program.Create<ExampleProgram>(world))
            {
                while (!program.IsFinished(out returnCode))
                {
                    simulator.Update();
                }
            }

            simulator.RemoveSystem<ExampleSystem>();
        }
    }

    return (int)returnCode;
}
```