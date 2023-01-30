using UnityEngine;

namespace zapnet
{
    /// <summary>
    /// Contains configuration for the synchronization of an entity's transform.
    /// </summary>
    [System.Serializable]
    public class SyncTransformConfig
    {
        /// <summary>
        /// The position axes to be synchronized across the network.
        /// </summary>
        public VectorAxes positionAxes = VectorAxes.X | VectorAxes.Y | VectorAxes.Z;

        /// <summary>
        /// Contains configuration to change the behavior of compression on the position value.
        /// </summary>
        public VectorCompressionConfig positionCompression;

        /// <summary>
        /// The rotation axes to be synchronized across the network.
        /// </summary>
        public VectorAxes rotationAxes = VectorAxes.X | VectorAxes.Y | VectorAxes.Z;

        /// <summary>
        /// Whether or not rotation is compressed when sending across the network.
        /// </summary>
        public bool rotationCompression = true;
    }
}
