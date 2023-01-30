namespace zapnet
{
    /// <summary>
    /// Contains configuration for compressing a vector.
    /// </summary>
    [System.Serializable]
    public class VectorCompressionConfig
    {
        /// <summary>
        /// Wether or not compression is enabled.
        /// </summary>
        public bool enabled = true;

        /// <summary>
        /// The minimum value of one value of the vector.
        /// </summary>
        public float minimum = -1024f;

        /// <summary>
        /// The maximum value of one value of the vector.
        /// </summary>
        public float maximum = 1024f;

        /// <summary>
        /// The number of bits to compress with.
        /// </summary>
        public int numberOfBits = 16;
    }
}
