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
    /// Data representing a remote call.
    /// </summary>
    public struct RemoteCall
    {
        private NetworkEvent<RemoteCallEvent> _evnt;
        private bool _invokeOnSelf;

        /// <summary>
        /// Get the params buffer attached to this remote call.
        /// </summary>
        public NetBuffer Params
        {
            get
            {
                return _evnt.Data.Params;
            }
        }

        /// <summary>
        /// Initialize a new remote call for the provided event.
        /// </summary>
        /// <param name="evnt"></param>
        /// <param name="invokeOnSelf"></param>
        public RemoteCall(NetworkEvent<RemoteCallEvent> evnt, bool invokeOnSelf)
        {
            _invokeOnSelf = invokeOnSelf;
            _evnt = evnt;
        }

        /// <summary>
        /// Send the call event and invoke the corresponding remote method.
        /// </summary>
        public void Call()
        {
            if (_invokeOnSelf)
            {
                _evnt.Invoke();
            }

            _evnt.Send();
        }
    }
}