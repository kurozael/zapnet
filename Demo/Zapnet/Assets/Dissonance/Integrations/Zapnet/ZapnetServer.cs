using System;
using Dissonance.Networking;
using ZapnetAPI = zapnet.Zapnet;
using Lidgren.Network;

namespace Dissonance.Integrations.Zapnet
{
    public class ZapnetServer : BaseServer<ZapnetServer, ZapnetClient, ZapnetPeer>
    {
        private readonly ZapnetCommsNetwork _net;
		
        public ZapnetServer(ZapnetCommsNetwork net)
        {
            _net = net;
        }

        public override void Connect()
        {
            ZapnetAPI.Network.Subscribe<DissonanceTransmitEvent>(OnReceiveEvent);
            ZapnetAPI.Player.onPlayerRemoved += OnPlayerRemoved;
			
            base.Connect();
        }

        private void OnPlayerRemoved(zapnet.Player player)
        {
            ClientDisconnected(new ZapnetPeer(player));
        }

        public override void Disconnect()
        {
            base.Disconnect();

            ZapnetAPI.Network.Unsubscribe<DissonanceTransmitEvent>(OnReceiveEvent);
            ZapnetAPI.Player.onPlayerRemoved -= OnPlayerRemoved;
        }
		
		private void OnReceiveEvent(DissonanceTransmitEvent data)
		{
			var peer = new ZapnetPeer(data.Sender);
			NetworkReceivedPacket(peer, data.Segment);
		}

        protected override void ReadMessages() {}

        private void Send(ZapnetPeer peer, ArraySegment<byte> packet, int channel, bool reliable)
        {
            var evnt = ZapnetAPI.Network.CreateEvent<DissonanceReceiveEvent>();
            evnt.Data.Segment = packet;
			evnt.SetChannel(channel);
            evnt.AddRecipient(peer.Player);
			evnt.SetDeliveryMethod(reliable ? NetDeliveryMethod.ReliableUnordered : NetDeliveryMethod.Unreliable);
			evnt.Send();
        }

        protected override void SendUnreliable(ZapnetPeer connection, ArraySegment<byte> packet)
        {
            Send(connection, packet, _net.VoiceDataChannelToClient, false);
        }

        protected override void SendReliable(ZapnetPeer connection, ArraySegment<byte> packet)
        {
            Send(connection, packet, _net.SystemMessagesChannelToClient, true);
        }
    }
}
