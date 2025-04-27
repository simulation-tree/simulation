using System;

namespace Simulation
{
    /// <summary>
    /// Defines the ordering hint for the generated system bank.
    /// </summary>
    public class SystemOrderAttribute : Attribute
    {
        /// <summary>
        /// Hint value.
        /// </summary>
        public readonly int order;

        /// <summary>
        /// Creates an instance of this attribute.
        /// </summary>
        public SystemOrderAttribute(int order)
        {
            this.order = order;
        }
    }
}