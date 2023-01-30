using Dissonance.Networking;
using UnityEngine;
using ZapnetAPI = zapnet.Zapnet;

namespace Dissonance.Integrations.Zapnet
{
    public class ZapnetCommsNetwork : BaseCommsNetwork<ZapnetServer, ZapnetClient, ZapnetPeer, Unit, Unit>
    {
        [SerializeField, UsedImplicitly] private int _voiceDataChannelToServer = 23;
        [SerializeField, UsedImplicitly] private int _systemMessagesChannelToServer = 0;
        [SerializeField, UsedImplicitly] private int _voiceDataChannelToClient = 23;
        [SerializeField, UsedImplicitly] private int _systemMessagesChannelToClient = 0;

        private bool _hasInitialized;

        public int VoiceDataChannelToServer
        {
            get { return _voiceDataChannelToServer; }
        }

        public int SystemMessagesChannelToServer
        {
            get { return _systemMessagesChannelToServer; }
        }

        public int VoiceDataChannelToClient
        {
            get { return _voiceDataChannelToClient; }
        }

        public int SystemMessagesChannelToClient
        {
            get { return _systemMessagesChannelToClient; }
        }

        protected override ZapnetClient CreateClient(Unit connectionParameters)
        {
            return new ZapnetClient(this);
        }

        protected override ZapnetServer CreateServer(Unit connectionParameters)
        {
            return new ZapnetServer(this);
        }

        protected override void Update()
        {
            if (IsInitialized)
            {
                var networkActive = (ZapnetAPI.Network && ZapnetAPI.Network.IsConnected);
				
                if (networkActive)
                {
                    if (!_hasInitialized)
                    {
                        if (ZapnetAPI.Network.IsServer)
                            RunAsDedicatedServer(Unit.None);
                        else
                            RunAsClient(Unit.None);

                        _hasInitialized = true;
                    }
                }
                else if (Mode != NetworkMode.None)
                {
                    _hasInitialized = false;
                    Stop();
                }
            }

            base.Update();
        }
    }
}
