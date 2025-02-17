# Simulation

[![Test](https://github.com/game-simulations/simulation/actions/workflows/test.yml/badge.svg)](https://github.com/game-simulations/simulation/actions/workflows/test.yml)

Library implementing programs executing within a simulator.

### Systems

Systems are defined by implementing the `ISystem` interface, and contain
functions for initializing, iterating, and finalizing the system with all
worlds that the simulator is aware of.

For start and update, it will run with the simulator world first, then with program worlds.
But on finish, it will run with program worlds first, then with the simulator world.
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
> A simulator may iterate over a system with multiple worlds, so the `systemContainer.World == world` check is necessary
for making a branch of code that only runs once on start and finish

Their default constructor is never called, so if the system type contains fields that need initialization,
it should be done in the `Start` function:
```cs
public readonly partial struct AnotherSystem : ISystem
{
    private readonly List<int> values;

    private AnotherSystem(List<int> values)
    {
        this.values = values;
    }

    void ISystem.Start(in SystemContainer systemContainer, in World world)
    {
        if (systemContainer.World == world)
        {
            List<int> values = new();
            systemContainer.Write(new AnotherSystem(values));
        }
    }

    void ISystem.Update(in SystemContainer systemContainer, in World world, in TimeSpan delta)
    {
        foreach (int value in values)
        {
            //do something with these values
        }
    }

    void ISystem.Finish(in SystemContainer systemContainer, in World world)
    {
        if (systemContainer.World == world)
        {
            AnotherSystem anotherSystem = systemContainer.Read<AnotherSystem>();
            anotherSystem.values.Dispose();
        }
}
```

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
> Removing a system isn't necessary, its just shown here for completeness

### Programs

Program entities are defined by implementing the `IProgram` interface, and
expose functions that are called by the simulator to start, update, and finish.

They are operated by a simulator, and have their own world to interact with separate
from the simulator's world:
```cs
public partial struct ExampleProgram : IProgram
{
    public readonly int initialValue;
    public int value;

    public ExampleProgram(int initialValue)
    {
        this.initialValue = initialValue;
        value = initialValue;
    }

    void IProgram.Start(in Simulator simulator, in Allocation allocation, in World world)
    {
        //initialization code
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
            using (Program program = Program.Create(world, new ExampleProgram(100)))
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

> Unlike systems, their initialization is customized during the creation of the `Program` object.
And so their default constructors *can* be used from there, as shown in the example above.

Normally if a program is finished, the status code in the `Finish` function will be the same as
the one returned by the `Update` function. But in the case that the program is disposed before
it finishes, the status code will be `StatusCode.Termination`.

### Update order

For all operations, systems are iterated upon first, then programs.