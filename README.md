# Simulation

[![Test](https://github.com/simulation-tree/simulation/actions/workflows/test.yml/badge.svg)](https://github.com/simulation-tree/simulation/actions/workflows/test.yml)

Library providing a way to organize systems, and executing programs.

### Running simulators

Simulators contain and update systems, and iterate over programs found
in the world that the simulator is created with:
```cs
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

### Order of operations

Systems take precedence over programs:

**Starting and Updating**

Systems will always be started before programs. Iterating first with the simulator
world, and then with the world of each program in the order they were created.

**Finishing**

Same as starting/updating but flipped. Systems finish before programs are, and with
program worlds in reverse order, then with simulator world last.

### Systems

Systems are defined by implementing the `ISystem` interface, and are responsible
for initializing, updating, and finalizing the system with all worlds that the
simulator is aware of.

```cs
public readonly partial struct ExampleSystem : ISystem
{
    void ISystem.Start(in SystemContainer systemContainer, in World world)
    {
    }

    void ISystem.Update(in SystemContainer systemContainer, in World world, in TimeSpan delta)
    {
    }

    void ISystem.Finish(in SystemContainer systemContainer, in World world)
    {
    }
}
```

### Initializing systems with data

When systems are added to a simulator world, they are created with `default` state.
In the case that they need to be initialized with data from the beginning, there is a way:
```cs
public readonly partial struct ExampleSystem : ISystem
{
    private readonly int initialData;

    private ExampleSystem(int initialData)
    {
        this.initialData = initialData;
    }

    void ISystem.Start(in SystemContainer systemContainer, in World world)
    {
        if (systemContainer.World == world)
        {
            systemContainer.Write(new ExampleSystem(1337));
            
            //if the system isnt a readonly type
            //ref ExampleSystem exampleSystem = ref systemContainer.Read<ExampleSystem>();
            //exampleSystem.initialData = 1337;
        }
    }

    ...
}
```

The example above shows a check in the `Start` function to see if the world being started with,
is the world of the simulator. Because this only occurs once per world, and systems update with
simulators first, this is a safe way to initialize systems with data. By overwriting the state
with the `Write` function or by modifying the state directly.

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
```

> Unlike systems, their initialization is customized during the creation of the `Program` object.
And so their default constructors *can* be used from there, as shown in the example above.

Normally if a program is finished, the status code in the `Finish` function will be the same as
the one returned by the `Update` function. But in the case that the program is disposed before
it finishes, the status code will be `StatusCode.Termination`.

### Update order

For all operations, systems are iterated upon first, then programs.
