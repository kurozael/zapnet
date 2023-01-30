using System;

namespace zapnet
{
    /// <summary>
    /// For use in a bit field representing a selection of axes.
    /// </summary>
    [System.Serializable] [Flags]
    public enum VectorAxes
    {
        /// <summary>
        /// No axes are included in the set.
        /// </summary>
        None = 0,

        /// <summary>
        /// Whether the x axis is included in the set.
        /// </summary>
        X = 1,

        /// <summary>
        /// Whether the y axis is included in the set.
        /// </summary>
        Y = 2,

        /// <summary>
        /// Whether the z axis is included in the set.
        /// </summary>
        Z = 4
    }
}
