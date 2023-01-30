/* 
 * Copyright © Conna Wiles <https://kurozael.com>
 * All Rights Reserved
 * 
 * kurozael <mailto:kurozael@gmail.com>
 */

using Lidgren.Network;

namespace zapnet
{
    /// <summary>
    /// A player synchronized across the network.
    /// </summary>
    public class SyncPlayer : SyncVar<Player>
    {
        /// <summary>
        /// The pending identifier while waiting for the player to be added.
        /// </summary>
        public uint? PendingId;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncPlayer"/> class.
        /// </summary>
        public SyncPlayer() : base(null, SyncTarget.All)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncPlayer"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="target">The target.</param>
        public SyncPlayer(Player value, SyncTarget target = SyncTarget.All) : base(value, target)
        {
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="SyncPlayer"/> to <see cref="System.Boolean"/>.
        /// </summary>
        public static implicit operator bool(SyncPlayer foo)
        {
            return (foo.Value != null);
        }

        /// <summary>
        /// Read and process data from an incoming message.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="changeSilently">Whether or not to avoid invoking change callbacks.</param>
        public override void Read(NetIncomingMessage buffer, bool changeSilently)
        {
            var playerId = buffer.ReadUInt32();

            if (playerId == 0)
            {
                SetValue(null, changeSilently);
                return;
            }

            var player = Zapnet.Player.Find(playerId);

            if (player != null)
            {
                SetValue(player, changeSilently);
                return;
            }

            if (!PendingId.HasValue)
            {
                Zapnet.Player.onPlayerAdded += OnPlayerAdded;
            }

            PendingId = playerId;
        }

        /// <summary>
        /// Write data to an outgoing message.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="dirtyOnly"></param>
        public override void Write(NetOutgoingMessage buffer, bool dirtyOnly)
        {
            buffer.Write(Value != null ? Value.PlayerId : 0);
        }

        private void OnPlayerAdded(Player player)
        {
            if (player.PlayerId == PendingId.Value)
            {
                Zapnet.Player.onPlayerAdded -= OnPlayerAdded;
                PendingId = null;
                Value = player;
            }
        }
    }
}
