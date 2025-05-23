# Simulation

[![Test](https://github.com/simulation-tree/simulation/actions/workflows/test.yml/badge.svg)](https://github.com/simulation-tree/simulation/actions/workflows/test.yml)

Library providing a way to organize and update systems with support for message handling.

### Running simulators

Simulators contain and update systems:
```cs
public static void Main()
{
    using World world = new();
    using Simulator simulator = new(world);

    simulator.Add(new ProgramSystems());
    simulator.Update(world);
    simulator.Remove<ProgramSystems>();
}

public class ProgramSystems : ISystem, IDisposable
{
    public ProgramSystems()
    {
        //initialize
    }

    public void Dispose()
    {
        //clean up
    }

    void ISystem.Update(Simulator simulator, double deltaTime)
    {
        //do work
    }
}
```

### Receiving messages

A system that is partial, and implementing the `IListener<T>` interface will
allow it to receive messages broadcast by the simulator:
```cs
simulator.Broadcast(32f);
simulator.Broadcast(32f);
simulator.Broadcast(32f);

public partial class ListenerSystem : IListener<float>
{
    void IListener<float>.Receive(ref float message)
    {
        //do something with this
    }
}
```

Messages can also be broadcast by reference, allowing systems to modify them,
and use it to communicate between different projects:
```cs
LoadRequest request = new();
simulator.Broadcast(ref request);
Assert.That(request.loaded, Is.True);

public partial class LoadSystem : IListener<LoadRequest>
{
    void IListener<float>.Receive(ref LoadRequest message)
    {
        message.loaded = true;
    }
}

public struct LoadRequest
{
    public bool loaded;
}
```

### Contributing and design

This library is created for composing behaviour of programs using systems, ideally created by
separate isolated projects.

Contributions to this goal are welcome.