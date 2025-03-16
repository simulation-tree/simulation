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
            simulator.AddSystem(new AllSystems());
            using (Program program = Program.Create(world, new ExampleProgram(100)))
            {
                while (!program.IsFinished(out statusCode))
                {
                    simulator.Update();
                }
                
                Console.WriteLine(program.Read<ExampleProgram>().value);
            }

            simulator.RemoveSystem<AllSystems>();
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

**Messages**

When messages are given to a simulator to handle, systems will be called with the 
simulator world, and then program worlds last.

### Systems

Systems are defined by implementing the `ISystem` interface, and are responsible
for initializing, updating, and finalizing the system with all worlds that the
simulator is aware of.

```cs
public readonly partial struct ExampleSystem : ISystem
{
    void IDisposable.Dispose()
    {
    }

    void ISystem.Start(in SystemContext context, in World world)
    {
    }

    void ISystem.Update(in SystemContext context, in World world, in TimeSpan delta)
    {
    }

    void ISystem.Finish(in SystemContext context, in World world)
    {
    }
}
```

### Initializing systems with dependent data

When systems are added to a simulator world, their constructor can be used to customize
the initial data. In the case that they need information from the simulator itself, the
state of the system can be overwritten using the `Write()` function in `Start()`:
```cs
public readonly partial struct ExampleSystem : ISystem
{
    private readonly int initialData;
    private readonly SystemContext context;

    [Obsolete("Default constructor not supported", true)]
    public ExampleSystem() { } //doing this to enforce the public constructor below

    public ExampleSystem(int initialData)
    {
        this.initialData = initialData;
    }

    private ExampleSystem(int initialData, SystemContext context)
    {
        this.initialData = initialData;
        this.context = context;
    }

    void ISystem.Start(in SystemContext context, in World world)
    {
        if (context.World == world)
        {
            context.Write(new ExampleSystem(initialData, context));
        }
    }

    ...
}
```

### Sub systems

When a system intends to have sub systems, and to act as a high level wrapper.
The `Start()` and `Finish()` functions are used to add and remove the sub systems:
```cs
public readonly partial struct AllSystems : ISystem
{
    void IDisposable.Dispose()
    {
    }

    void ISystem.Start(in SystemContext context, in World world)
    {
        if (context.World == world)
        {
            context.AddSystem(new ExampleSystem(100));
        }
    }

    void ISystem.Update(in SystemContext context, in World world, in TimeSpan delta)
    {
    }

    void ISystem.Finish(in SystemContext context, in World world)
    {
        if (systemContainer.World == world)
        {
            context.RemoveSystem<ExampleSystem>();
        }
    }
}
```

Notice that the systems aren't added in the constructor, and aren't removed in dispose.
This is because adding the systems through a `SystemContext` gives information to the
simulator about ordering of dispose calls.

### Programs

Programs are entities created in the simulator's world, and are defined by implementing
the `IProgram` interface. They expose functions that are called by the simulator, and
contain their own world separate from the simulator's:
```cs
public partial struct ExampleProgram : IProgram<ExampleProgram>
{
    public readonly int initialValue;
    public int value;

    public ExampleProgram(int initialValue)
    {
        this.initialValue = initialValue;
        value = initialValue;
    }

    void IProgram<ExampleProgram>.Start(ref ExampleProgram program, in Simulator simulator, in World world)
    {
    }

    StatusCode IProgram<ExampleProgram>.Update(in TimeSpan delta)
    {
        if (value > 200)
        {
            return StatusCode.Success(0);
        }

        value++;
        return StatusCode.Continue;
    }

    void IProgram<ExampleProgram>.Finish(in StatusCode statusCode)
    {
    }
}
```

### Finishing programs

A program is finished when the status code from `Update()` is neither `default` or `StatusCode.Continue`.
This status code will then be available in `Finish()`. If the program is finished because
the simulator is disposed before the program decides to, the status code will be `StatusCode.Termination`.

### Contributing and design

This library is created for composing behaviour of programs using systems, ideally created by
separate isolated projects.

Contributions to this goal are welcome.