namespace zapnet
{
    /// <summary>
    /// The interface that all controllable entities must implement.
    /// </summary>
    public interface IControllable
    {
        /// <summary>
        /// The player currently controlling this entity.
        /// </summary>
        SyncPlayer Controller { get; }

        /// <summary>
        /// Assign control of this entity to the provided player.
        /// </summary>
        /// <param name="player"></param>
        void AssignControl(Player player);

        /// <summary>
        /// Get whether this entity has a controller.
        /// </summary>
        /// <returns></returns>
        bool HasController();

        /// <summary>
        /// Get whether the provided player has control of this entity.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        bool HasControl(Player player);

        /// <summary>
        /// Get whether any player has control of this entity.
        /// </summary>
        /// <returns></returns>
        bool HasControl();
    }
}
