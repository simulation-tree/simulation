# Simulation

[![Test](https://github.com/simulation-tree/simulation/actions/workflows/test.yml/badge.svg)](https://github.com/simulation-tree/simulation/actions/workflows/test.yml)

Library providing a way to broadcast messages to added listeners.

### Running simulators

Simulators contain and update systems:
```cs
public static void Main()
{
    using Simulator simulator = new();
    simulator.Add(new ProgramSystems());
    // do work
    simulator.Remove<ProgramSystems>();
}

public class ProgramSystems : IDisposable
{
    public ProgramSystems()
    {
        //before addition
    }

    public void Dispose()
    {
        //after removal
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

### Global simulator

Another way to have listeners and broadcasting setup, is using the included `GlobalSimulator` type.
This approach is slimmer than with the `Simulator`, at the cost of the listeners being global to the entire
runtime.
```cs
public class Program
{
    public static void Main()
    {
        GlobalSimulatorLoader.Load();
        GlobalSimulator.Broadcast(32f);
        GlobalSimulator.Broadcast(32f);
        GlobalSimulator.Broadcast(32f);
    }
}

public static class Systems
{
    [Listener<float>]
    public static void Update(ref float message)
    {
    }
}
```

### Contributing and design

This library is created for composing behaviour of programs using systems, ideally created by
separate isolated projects.

Contributions to this goal are welcome.
