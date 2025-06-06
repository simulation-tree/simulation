using System;

namespace Simulation
{
    /// <summary>
    /// Attribute to mark static methods as listeners for messages of type <typeparamref name="T"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ListenerAttribute<T> : Attribute where T : unmanaged
    {
    }
}
