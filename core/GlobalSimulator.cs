using System;
using System.Collections.Generic;

namespace Simulation
{
    /// <summary>
    /// Dispatches messages for static methods with the <see cref="ListenerAttribute{T}"/>.
    /// </summary>
    public static class GlobalSimulator
    {
        private static readonly List<Action> clearFunctions = new();

        /// <summary>
        /// Registers a listener for messages of type <typeparamref name="T"/>.
        /// </summary>
        public static void Register<T>(Receive<T> receive) where T : unmanaged
        {
            Array.Resize(ref Listeners<T>.list, Listeners<T>.list.Length + 1);
            Listeners<T>.list[^1] = receive;
        }

        /// <summary>
        /// Registers a <paramref name="listener"/>.
        /// </summary>
        public static void Register<T>(IListener<T> listener) where T : unmanaged
        {
            Receive<T> receive = listener.Receive;
            Array.Resize(ref Listeners<T>.list, Listeners<T>.list.Length + 1);
            Listeners<T>.list[^1] = receive;
        }

        /// <summary>
        /// Resets the global simulator to initial state, with no listeners registered.
        /// </summary>
        public static void Reset()
        {
            foreach (Action clearFunction in clearFunctions)
            {
                clearFunction();
            }
        }

        /// <summary>
        /// Broadcasts the given <paramref name="message"/>.
        /// </summary>
        public static void Broadcast<T>(T message) where T : unmanaged
        {
            int length = Listeners<T>.list.Length;
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
            int length = Listeners<T>.list.Length;
            for (int i = 0; i < length; i++)
            {
                Listeners<T>.list[i](ref message);
            }
        }

        private static class Listeners<T> where T : unmanaged
        {
            public static Receive<T>[] list = [];

            static Listeners()
            {
                clearFunctions.Add(Reset);
            }

            public static void Reset()
            {
                Array.Resize(ref list, 0);
            }
        }

        /// <summary>
        /// Delegate for message receivers.
        /// </summary>
        public delegate void Receive<T>(ref T message) where T : unmanaged;
    }
}