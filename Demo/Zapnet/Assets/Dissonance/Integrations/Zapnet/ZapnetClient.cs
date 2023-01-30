using System;
using Dissonance.Networking;
using ZapnetAPI = zapnet.Zapnet;
using Lidgren.Network;

namespace Dissonance.Integrations.Zapnet
{
    public class ZapnetClient : BaseClient<ZapnetServer, ZapnetClient, ZapnetPeer>
    {
        private readonly ZapnetCommsNetwork _net;

        public ZapnetClient([NotNull] ZapnetCommsNetwork net) : base(net)
        {
            _net = net;
        }

        public override void Connect()
        {
			ZapnetAPI.Network.Subscribe<DissonanceReceiveEvent>(OnReceiveEvent);
			
            Connected();
        }

        public override void Disconnect()
        {
            ZapnetAPI.Network.Unsubscribe<DissonanceReceiveEvent>(OnReceiveEvent);

            base.Disconnect();
        }

		private void OnReceiveEvent(DissonanceReceiveEvent data)
		{
            NetworkReceivedPacket(data.Segment);
		}

        protected override void ReadMessages() {}

        private void Send(ArraySegment<byte> packet, int channel, bool reliable)
        {
			var evnt = ZapnetAPI.Network.CreateEvent<DissonanceTransmitEvent>();
            evnt.Data.Segment = packet;
			evnt.SetChannel(channel);
			evnt.SetDeliveryMethod(reliable ? NetDeliveryMethod.ReliableUnordered : NetDeliveryMethod.Unreliable);
			evnt.Send();
        }

        protected override void SendUnreliable(ArraySegment<byte> packet)
        {
            Send(packet, _net.SystemMessagesChannelToServer, false);
        }

        protected override void SendReliable(ArraySegment<byte> packet)
        {
            Send(packet, _net.SystemMessagesChannelToServer, true);
        }
    }
}
