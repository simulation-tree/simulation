using System.Collections;
using System.Collections.Generic;
using Types;

namespace Simulation
{
    /// <summary>
    /// Dispatches messages for static methods with the <see cref="ListenerAttribute{T}"/>.
    /// </summary>
    public static class GlobalSimulator
    {
        private static readonly Dictionary<TypeMetadata, IList> listeners = new();

        /// <summary>
        /// Registers a listener for messages of type <typeparamref name="T"/>.
        /// </summary>
        public static void Register<T>(Receive<T> receive) where T : unmanaged
        {
            TypeMetadata messageType = TypeMetadata.GetOrRegister<T>();
            Listeners<T>.list.Add(receive);
            listeners[messageType] = Listeners<T>.list;
        }

        /// <summary>
        /// Registers a <paramref name="listener"/>.
        /// </summary>
        public static void Register<T>(IListener<T> listener) where T : unmanaged
        {
            TypeMetadata messageType = TypeMetadata.GetOrRegister<T>();
            Receive<T> receive = listener.Receive;
            Listeners<T>.list.Add(receive);
            listeners[messageType] = Listeners<T>.list;
        }

        /// <summary>
        /// Resets the global simulator to initial state, with no listeners registered.
        /// </summary>
        public static void Reset()
        {
            foreach (IList list in listeners.Values)
            {
                list.Clear();
            }

            listeners.Clear();
        }

        /// <summary>
        /// Broadcasts the given <paramref name="message"/>.
        /// </summary>
        public static void Broadcast<T>(T message) where T : unmanaged
        {
            int length = Listeners<T>.list.Count;
            for (int i = 0; i < length; i++)
            {
                Listeners<T>.list[i](ref message);
            }
        }

        /// <summary>
        /// Broadcasts the given <paramref name="message"/>.
        /// </summary>
        public static void Broadcast<T>(ref T message) where T : unmanaged
        {
            int length = Listeners<T>.list.Count;
            for (int i = 0; i < length; i++)
            {
                Listeners<T>.list[i](ref message);
            }
        }

        private static class Listeners<T> where T : unmanaged
        {
            public static readonly List<Receive<T>> list = new();
        }

        /// <summary>
        /// Delegate for message receivers.
        /// </summary>
        public delegate void Receive<T>(ref T message) where T : unmanaged;
    }
}