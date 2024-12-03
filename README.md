# Simulation
Library implementing the concept of programs executing within a simulator.

### Systems
Systems are defined by implementing the `ISystem` interface, and contain
functions for initializing, iterating, and finalizing the system with all
worlds that the simulator is aware of:
```cs
public readonly partial struct ExampleSystem : ISystem
{
    void ISystem.Start(in SystemContainer systemContainer, in World world)
    {
        if (systemContainer.World == world)
        {
            Entity firstEntity = new(world);
            firstEntity.AddComponent(0u);
        }
    }

    void ISystem.Update(in SystemContainer systemContainer, in World world, in TimeSpan delta)
    {
        if (systemContainer.World == world)
        {
            ref uint firstEntityValue = ref world.GetComponentRef<uint>(1);
            firstEntityValue++;
        }
    }

    void ISystem.Finish(in SystemContainer systemContainer, in World world)
    {
        if (systemContainer.World == world)
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
public partial struct ExampleProgram : IProgram
{
    public int value;

    public ExampleProgram()
    {
        value = 100;
    }

    void IProgram.Start(in Simulator simulator, in Allocation allocation, in World world)
    {
        //initialization code
        allocation.Write(new ExampleProgram());
    }

    StatusCode IProgram.Update(in TimeSpan delta)
    {
        if (value > 200)
        {
            return StatusCode.Success(0);
        }

        value++;
        return StatusCode.Continue;
    }

    void IProgram.Finish(in StatusCode statusCode)
    {
        //finalization code
    }
}

public static int Main()
{
    StatusCode statusCode;
    using (World world = new())
    {
        using (Simulator simulator = new(world))
        {
            simulator.AddSystem<ExampleSystem>();
            using (Program program = Program.Create<ExampleProgram>(world))
            {
                while (!program.IsFinished(out statusCode))
                {
                    simulator.Update();
                }
                
                Console.WriteLine(program.Read<ExampleProgram>().value);
            }

            simulator.RemoveSystem<ExampleSystem>();
        }
    }

    return statusCode.IsSuccess ? 0 : 1;
}
```