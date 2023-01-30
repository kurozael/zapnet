using System;
using zapnet;

namespace Dissonance.Integrations.Zapnet
{
    public struct ZapnetPeer : IEquatable<ZapnetPeer>
    {
        public readonly Player Player;

        public ZapnetPeer(Player player)
        {
            Player = player;
        }

        public bool Equals(ZapnetPeer other)
        {
            return (Player == other.Player);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            if (obj.GetType() != GetType())
                return false;

            return Equals((ZapnetPeer)obj);
        }

        public override int GetHashCode()
        {
            return Player.GetHashCode();
        }

        public static bool operator ==(ZapnetPeer left, ZapnetPeer right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ZapnetPeer left, ZapnetPeer right)
        {
            return !Equals(left, right);
        }
    }
}
