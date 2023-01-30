using UnityEngine;
using ZapnetAPI = zapnet.Zapnet;
using zapnet;

namespace Dissonance.Integrations.Zapnet
{
    [RequireComponent(typeof(BaseEntity))]
    public class ZapnetPlayer : MonoBehaviour, IDissonancePlayer
    {
        private BaseEntity _entity;
        private DissonanceComms _comms;

        public string PlayerId
        {
            get
            {
                return _comms.LocalPlayerName;
            }
        }

        public Vector3 Position
        {
            get
            {
                return transform.position;
            }
        }

        public Quaternion Rotation
        {
            get
            {
                return transform.rotation;
            }
        }

        public NetworkPlayerType Type
        {
            get
            {
                if (IsLocalPlayer())
                {
                    return NetworkPlayerType.Local;
                }
                else
                {
                    return NetworkPlayerType.Remote;
                }
            }
        }

        public bool IsTracking
        {
            get
            {
                return true;
            }
        }

        private bool IsLocalPlayer()
        {
            if (_entity && ZapnetAPI.Player.LocalPlayer != null)
            {
                return (_entity == ZapnetAPI.Player.LocalPlayer.Entity);
            }

            return false;
        }

        private void StartTracking()
        {
            _comms.TrackPlayerPosition(this);
        }

        private void StopTracking()
        {
            _comms.StopTracking(this);
        }

        private void OnDestroy()
        {
            StopTracking();
        }

        private void Start()
        {
            _entity = GetComponent<BaseEntity>();
            _comms = FindObjectOfType<DissonanceComms>();

            if (ZapnetAPI.Network.IsClient)
            {
                StartTracking();
            }
        }
    }
}